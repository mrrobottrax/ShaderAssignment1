using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class LocalClient : MonoBehaviour
{
	protected Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	private SteamNetworkingIdentity m_server;
	internal static HSteamNetConnection m_hServerConn;

	internal static List<HSteamNetConnection> m_hPeerConns = new();

	internal static NetworkObject m_player;

	#region Initialization

	private void OnEnable()
	{
		if (!SteamManager.Initialized)
			return;

		m_SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);
	}

	private void OnDisable()
	{
		m_SteamNetConnectionStatusChanged.Dispose();
	}

	public void Connect(SteamNetworkingIdentity server)
	{
		m_server = server;
		SteamNetworkingSockets.ConnectP2P(ref m_server, 0, 0, null);

		SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
	}

	#endregion

	private void Update()
	{
		ReceiveMessages();
	}

	private void LateUpdate()
	{
		if (TickManager.ShouldTick())
		{
			SteamNetworkingSockets.FlushMessagesOnConnection(m_hServerConn);

			foreach (var hPeer in m_hPeerConns)
			{
				SteamNetworkingSockets.FlushMessagesOnConnection(hPeer);
			}
		}
	}

	private void ReceiveMessages()
	{
		IntPtr[] pMessages = new IntPtr[NetworkData.k_maxMessages];

		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(m_hServerConn, pMessages, pMessages.Length);

		if (messageCount <= 0)
		{
			return;
		}

		// todo: try processing in reverse so we can quickly discard older messages
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
			// Spawn local player
			case ESnapshotMessageType.ConnectAck:
				ConnectAckMessage connectAck = Marshal.PtrToStructure<ConnectAckMessage>(message.m_pData + 1);
				Debug.Log("Connect ack " + connectAck.m_playerObjectID);

				SpawnPrefabMessage spawn = new()
				{
					m_networkID = connectAck.m_playerObjectID,
					m_ownerID = connectAck.m_playerObjectID,
					m_prefabIndex = NetworkData.k_playerPrefabIndex
				};
				m_player = NetworkObjectManager.SpawnNetworkPrefab(spawn, true);
				break;

			// Load scene
			case ESnapshotMessageType.SceneChange:
				SceneChangeMessage sceneChange = Marshal.PtrToStructure<SceneChangeMessage>(message.m_pData + 1);
				Debug.Log("Scene change " + sceneChange.m_sceneIndex);

				SceneManager.LoadScene(sceneChange.m_sceneIndex);
				break;

			// Spawn a network prefab
			case ESnapshotMessageType.SpawnPrefab:
				SpawnPrefabMessage spawnPrefab = Marshal.PtrToStructure<SpawnPrefabMessage>(message.m_pData + 1);
				Debug.Log("Object spawn " + spawnPrefab.m_networkID + " : " + spawnPrefab.m_prefabIndex);

				NetworkObjectManager.SpawnNetworkPrefab(spawnPrefab, false);
				break;

			// Delete a network object
			case ESnapshotMessageType.RemoveGameObject:
				RemoveObjectMessage removeObject = Marshal.PtrToStructure<RemoveObjectMessage>(message.m_pData + 1);
				Debug.Log("Remove object " + removeObject.m_networkID);

				NetworkObjectManager.RemoveObject(removeObject);
				break;

			// Update a network behaviour
			case ESnapshotMessageType.NetworkBehaviourUpdate:
				NetworkBehaviour.ProcessUpdateMessage(message);
				break;

			// Add a new peer
			case ESnapshotMessageType.NewPeer:
				NewPeerMessage peerMessage = Marshal.PtrToStructure<NewPeerMessage>(message.m_pData + 1);
				Debug.Log("New peer " + peerMessage.m_steamIdentity.GetSteamID64());

				HSteamNetConnection hConn = SteamNetworkingSockets.ConnectP2P(ref peerMessage.m_steamIdentity, 0, 0, null);
				m_hPeerConns.Add(hConn);
				break;

			default:
				Debug.LogWarning("Unknown message type " + type);
				break;
		}
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
			if (m_hServerConn == default)
			{
				// First connection is the server
				m_hServerConn = pCallback.m_hConn;
				m_hPeerConns = new List<HSteamNetConnection>();
			}
			else
			{
				// Next connections are peers
				m_hPeerConns.Add(pCallback.m_hConn);
				Debug.LogWarning("New peer");

				RemoteClient rc = new(pCallback.m_hConn,pCallback.m_info.m_identityRemote, null);
				SendFunctions.SendConnectAck(rc);
			}
		}
	}
}