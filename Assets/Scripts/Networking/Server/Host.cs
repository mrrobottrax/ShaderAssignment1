using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class Host : MonoBehaviour
{
	protected Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	HSteamListenSocket m_hListenSocket;

	internal NetworkObject m_player;
	internal Dictionary<SteamNetworkingIdentity, Peer> m_clients = new();

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
		foreach (var client in NetworkManager.GetAllPeers())
		{
			SteamNetworkingSockets.CloseConnection(client.m_hConn, 0, null, true);
		}
	}

	private void Update()
	{
		// Receive
		foreach (var client in NetworkManager.GetAllPeers())
		{
			ReceiveMessages(client);
			client.FlushQueuedMessages();
		}

		// Check if any clients are still loading
		if (m_waitingForPeers)
		{
			bool noneLoading = true;
			foreach (var peer in NetworkManager.GetAllPeers())
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
		// todo:
		//SendFunctions.SendSceneInfo();

		StartCoroutine(SendSnapshotNextFrame());

		// Wait for confirmation that clients have loaded
		if (NetworkManager.GetAllPeers().Count() > 0)
		{
			foreach (var peer in NetworkManager.GetAllPeers())
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
		// todo:
		//SendFunctions.SendFullSnapshot();
	}

	private void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
	{
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
			if (!m_clients.TryGetValue(pCallback.m_info.m_identityRemote, out Peer client))
			{
				// Add a new player
				NetworkObject player = SpawnPlayer(pCallback.m_info.m_identityRemote);
				client = new(pCallback.m_hConn, pCallback.m_info.m_identityRemote, player);
				m_clients.Add(pCallback.m_info.m_identityRemote, client);
			}
			else
			{
				client.UpdateConnection(pCallback.m_hConn);
			}

			// todo:
			// Give initial info
			// SendFunctions.SendSceneInfo(client);

			// // Send DontDestroyOnLoad objects on first connection
			// foreach (var networkObject in NetworkObjectManager.GetPersistentNetObjects())
			// {
			// 	SendFunctions.SendSpawnPrefab(networkObject.m_netID, networkObject.m_prefabIndex, networkObject.m_ownerIndentity, client);
			// 	SendFunctions.SendObjectSnapshot(networkObject, client);
			// }

			// SendFunctions.SendFullSnapshot(client);

			// // Send the info of the other players
			// SendFunctions.SendPeers(client);
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

			NetworkManager.ProcessMessage(message, client);

			// Free data
			SteamNetworkingMessage_t.Release(pMessages[i]);
		}
	}
	private NetworkObject SpawnPlayer(SteamNetworkingIdentity owner)
	{
		GameObject goPlayer = Instantiate(NetworkData.GetPlayerPrefab());
		DontDestroyOnLoad(goPlayer);

		NetworkObject netObj = goPlayer.GetComponent<NetworkObject>();
		netObj.m_ownerIndentity = owner;
		netObj.Init();

		return netObj;
	}
}
