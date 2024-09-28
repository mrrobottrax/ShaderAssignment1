using Steamworks;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class Host : MonoBehaviour
{
	protected Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	HSteamListenSocket m_hListenSocket;

	internal NetworkObject m_player;

	bool m_waitingForPeers = false;

	#region Callbacks

	private void Awake()
	{
		m_SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);
		SceneManager.sceneLoaded += OnSceneLoad;

		m_hListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
	}

	private void OnDestroy()
	{
		m_SteamNetConnectionStatusChanged.Dispose();
		SceneManager.sceneLoaded -= OnSceneLoad;

		// Close connections
		if (!SteamManager.Initialized) return;

		SteamNetworkingSockets.CloseListenSocket(m_hListenSocket);
		foreach (var client in NetworkManager.m_peers.Values)
		{
			SteamNetworkingSockets.CloseConnection(client.m_hConn, 0, null, true);
		}
	}

	private void Update()
	{
		// Receive
		foreach (var client in NetworkManager.m_peers.Values)
		{
			ReceiveMessages(client);
			client.FlushQueuedMessages();
		}

		// Check if any clients are still loading
		if (m_waitingForPeers)
		{
			bool noneLoading = true;
			foreach (var peer in NetworkManager.m_peers.Values)
			{
				if (peer.m_loading)
				{
					noneLoading = false;
					break;
				}
			}

			if (noneLoading)
			{
				m_waitingForPeers = false;
				Time.timeScale = 1;
			}
		}
	}

	public void AddPlayer()
	{
		m_player = SpawnPlayer(NetworkManager.m_localIdentity);
	}

	private void OnSceneLoad(Scene scene, LoadSceneMode mode)
	{
		if (mode == LoadSceneMode.Additive)
		{
			Debug.LogWarning("Networking additive scenes not supported");
			return;
		}

		// Probably means we've loaded into the lobby
		if (m_player == null)
		{
			AddPlayer();
		}

		// Tell all clients to change scene
		SendFunctions.SendSceneInfo();

		StartCoroutine(SendSnapshotNextFrame());

		// Wait for confirmation that clients have loaded
		if (NetworkManager.m_peers.Count > 0)
		{
			foreach (var peer in NetworkManager.m_peers.Values)
			{
				peer.m_loading = true;
			}
			Time.timeScale = 0;
			m_waitingForPeers = true;
		}
	}

	IEnumerator SendSnapshotNextFrame()
	{
		yield return null;
		SendFunctions.SendFullSnapshot();
	}

	private void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
	{
		Debug.Log("Connection state changed to " + pCallback.m_info.m_eState);

		if (pCallback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None &&
			pCallback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
		{
			// New connection request, accept it blindly
			Debug.Log("Accepting connection request from " + pCallback.m_info.m_identityRemote.GetSteamID64());
			SteamNetworkingSockets.AcceptConnection(pCallback.m_hConn);
			SteamNetworkingSockets.FlushMessagesOnConnection(pCallback.m_hConn);
		}

		if (pCallback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
		{
			// Check if this player is already in game (dropped connection or something)
			if (!NetworkManager.m_peers.TryGetValue(pCallback.m_info.m_identityRemote, out Peer client))
			{
				// Add a new player
				NetworkObject player = SpawnPlayer(pCallback.m_info.m_identityRemote);
				client = new(pCallback.m_hConn, pCallback.m_info.m_identityRemote, player);
				NetworkManager.m_peers.Add(pCallback.m_info.m_identityRemote, client);
				Debug.LogWarning("Peer added from host");
			}
			else
			{
				client.UpdateConnection(pCallback.m_hConn);
			}

			// Give initial info
			SendFunctions.SendSceneInfo(client);

			// Send DontDestroyOnLoad objects on first connection
			foreach (var networkObject in NetworkObjectManager.GetPersistentNetObjects())
			{
				SendFunctions.SendSpawnPrefab(networkObject.m_netID, networkObject.m_prefabIndex, networkObject.m_ownerIndentity, client);
				SendFunctions.SendObjectSnapshot(networkObject, client);
			}

			SendFunctions.SendFullSnapshot(client);

			// Send the info of the other players
			SendFunctions.SendPeers(client);
		}
	}

	#endregion

	private void ReceiveMessages(Peer client)
	{
		IntPtr[] pMessages = new IntPtr[NetworkData.k_maxMessages];
		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(client.m_hConn, pMessages, pMessages.Length);

		for (int i = 0; i < messageCount; ++i)
		{
			SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(pMessages[i]);

			ProcessMessage(message, client);

			// Free data
			SteamNetworkingMessage_t.Release(pMessages[i]);
		}
	}

	private void ProcessMessage(SteamNetworkingMessage_t message, Peer sender)
	{
		EMessageType type = (EMessageType)Marshal.ReadByte(message.m_pData);

		switch (type)
		{
			case EMessageType.NetworkBehaviourUpdate:
				NetworkBehaviour.ProcessUpdateMessage(message);
				break;

			case EMessageType.VoiceData:
				if (VoiceManager.Instance)
					VoiceManager.Instance.ReceiveVoice(message, sender);
				break;

			case EMessageType.SceneChange:
				SceneChangeMessage sceneChange = Marshal.PtrToStructure<SceneChangeMessage>(message.m_pData + 1);
				if (sceneChange.m_sceneIndex == SceneManager.GetActiveScene().buildIndex)
				{
					sender.m_loading = false;
				}
				else
				{
					Debug.Log($"Client {sender.m_identity} is stupid and on the wrong scene. Correct scene is " +
						$"{SceneManager.GetActiveScene().buildIndex}. Client is on {sceneChange.m_sceneIndex}.");
				}
				break;

			default:
				Debug.LogWarning("Unknown message type " + type);
				break;
		}
	}

	private NetworkObject SpawnPlayer(SteamNetworkingIdentity owner)
	{
		GameObject goPlayer = Instantiate(NetworkData.GetPlayerPrefab());
		DontDestroyOnLoad(goPlayer);

		NetworkObject netObj = goPlayer.GetComponent<NetworkObject>();
		netObj.ForceRegister();

		netObj.m_ownerIndentity = owner;

		// Set IsOwner of NetworkBehaviours
		foreach (var component in netObj.m_networkBehaviours)
		{
			component.IsOwner = owner.Equals(NetworkManager.m_localIdentity);
		}

		return netObj;
	}
}
