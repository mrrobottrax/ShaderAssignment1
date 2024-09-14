using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Host : MonoBehaviour
{
	protected Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	internal static NetworkObject m_player;
	internal static readonly Dictionary<SteamNetworkingIdentity, RemoteClient> m_clients = new();

	#region Initialization
	private void Awake()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnEnable()
	{
		if (!SteamManager.Initialized)
			return;

		m_SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);
	}

	private void OnDisable()
	{
		if (!SteamManager.Initialized)
			return;

		m_SteamNetConnectionStatusChanged.Dispose();
	}

	private void Start()
	{
		SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);

		m_player = SpawnPlayer(true);
	}
	#endregion

	private void Update()
	{
		foreach (var client in m_clients.Values)
		{
			ReceiveMessages(client);
		}
	}

	private void LateUpdate()
	{
		if (TickManager.ShouldTick())
		{
			// Send updates to all clients
			foreach (var client in m_clients)
			{
				client.Value.FlushQueuedMessages();
			}
		}
	}

	private void ReceiveMessages(RemoteClient client)
	{
		IntPtr[] pMessages = new IntPtr[NetworkData.k_maxMessages];

		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(client.m_hConn, pMessages, pMessages.Length);

		if (messageCount <= 0)
		{
			return;
		}

		for (int i = 0; i < messageCount; ++i)
		{
			SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(pMessages[i]);

			ProcessMessage(message);

			// Free data
			SteamNetworkingMessage_t.Release(pMessages[i]);
		}
	}

	private void ProcessMessage(SteamNetworkingMessage_t message)
	{
		ESnapshotMessageType type = (ESnapshotMessageType)Marshal.ReadByte(message.m_pData);

		switch (type)
		{
			case ESnapshotMessageType.NetworkBehaviourUpdate:
				NetworkBehaviour.ProcessUpdateMessage(message);
				break;

			default:
				Debug.LogWarning("Unknown message type " + type);
				break;
		}
	}

	private void OnSceneChange(Scene oldScene, Scene newScene)
	{
		// Tell all clients to change scene
		SendFunctions.SendSceneInfo();
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
			// Check if this player is already in game
			if (!m_clients.TryGetValue(pCallback.m_info.m_identityRemote, out RemoteClient client))
			{
				// Add a new player
				NetworkObject player = SpawnPlayer();
				client = new(pCallback.m_hConn, pCallback.m_info.m_identityRemote, player);
				m_clients.Add(pCallback.m_info.m_identityRemote, client);
			}
			else
			{
				client.UpdateConnection(pCallback.m_hConn);
			}

			// Give initial info
			SendFunctions.SendConnectAck(client);
			SendFunctions.SendSceneInfo(client);
			SendFunctions.SendPeers(client);

			// Send DontDestroyOnLoad objects on first connection
			foreach (var networkObject in NetworkObjectManager.GetPersistentNetObjects())
			{
				// Don't send their own player
				if (networkObject.m_netID != client.m_player.m_netID)
				{
					Debug.Log("Sending spawn: " + networkObject.m_netID + " : " + networkObject.m_prefabIndex);
					SendFunctions.SendSpawnPrefab(networkObject.m_netID, networkObject.m_prefabIndex, networkObject.m_ownerID, client);
					SendFunctions.SendObjectSnapshot(networkObject, client);
				}
			}
		}
	}

	private NetworkObject SpawnPlayer(bool forHost = false)
	{
		GameObject goPlayer = Instantiate(NetworkData.GetPlayerPrefab());
		DontDestroyOnLoad(goPlayer);

		NetworkObject netObj = goPlayer.GetComponent<NetworkObject>();
		netObj.ForceRegister();

		netObj.m_ownerID = netObj.m_netID;

		// Set IsOwner of NetworkBehaviours
		foreach (var component in netObj.m_networkBehaviours)
		{
			component.IsOwner = forHost || netObj.m_ownerID == m_player.m_ownerID;
		}

		return netObj;
	}
}
