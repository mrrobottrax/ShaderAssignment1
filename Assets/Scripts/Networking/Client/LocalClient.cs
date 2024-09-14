using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class LocalClient : MonoBehaviour
{
	Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	HSteamListenSocket m_listenSocket;

	internal Peer m_server;
	internal List<Peer> m_peers = new();

	internal NetworkObject m_player;

	readonly Dictionary<SteamNetworkingIdentity, int> m_peerIDs = new();

	private void Awake()
	{
		m_SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);

		TickManager.OnTick += Tick;
	}

	private void OnDestroy()
	{
		m_SteamNetConnectionStatusChanged.Dispose();

		TickManager.OnTick -= Tick;

		// Close connections
		if (!SteamManager.Initialized) return;

		SteamNetworkingSockets.CloseListenSocket(m_listenSocket);

		SteamNetworkingSockets.CloseConnection(m_server.m_hConn, 0, null, true);
		foreach (Peer peer in m_peers)
		{
			SteamNetworkingSockets.CloseConnection(peer.m_hConn, 0, null, true);
		}

	}

	#region External Calls

	internal void Connect(SteamNetworkingIdentity host)
	{
		m_server.m_identity = host;
		m_server.m_hConn = SteamNetworkingSockets.ConnectP2P(ref host, 0, 0, null);

		m_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
	}

	private void Update()
	{
		ReceiveMessages(m_server);

		foreach (var peer in m_peers)
		{
			ReceiveMessages(peer);
		}
	}

	internal void Tick()
	{
		SteamNetworkingSockets.FlushMessagesOnConnection(m_server.m_hConn);

		foreach (var peer in m_peers)
		{
			SteamNetworkingSockets.FlushMessagesOnConnection(peer.m_hConn);
		}
	}

	#endregion

	void ReceiveMessages(Peer peer)
	{
		IntPtr[] pMessages = new IntPtr[NetworkData.k_maxMessages];

		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(peer.m_hConn, pMessages, pMessages.Length);

		if (messageCount <= 0)
		{
			return;
		}

		// todo: try processing in reverse so we can quickly discard older messages
		for (int i = 0; i < messageCount; ++i)
		{
			SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(pMessages[i]);

			ProcessMessage(message, peer);

			// Free data
			SteamNetworkingMessage_t.Release(pMessages[i]);
		}
	}

	void ProcessMessage(SteamNetworkingMessage_t message, Peer sender)
	{
		EMessageType type = (EMessageType)Marshal.ReadByte(message.m_pData);

		switch (type)
		{
			// Load scene
			case EMessageType.SceneChange:
				SceneChangeMessage sceneChange = Marshal.PtrToStructure<SceneChangeMessage>(message.m_pData + 1);
				Debug.Log("Scene change " + sceneChange.m_sceneIndex);

				SceneManager.LoadScene(sceneChange.m_sceneIndex);
				break;

			// Spawn a network prefab
			case EMessageType.SpawnPrefab:
				SpawnPrefabMessage spawnPrefab = Marshal.PtrToStructure<SpawnPrefabMessage>(message.m_pData + 1);
				Debug.Log("Object spawn " + spawnPrefab.m_networkID + " : " + spawnPrefab.m_prefabIndex);

				NetworkObjectManager.SpawnNetworkPrefab(spawnPrefab);
				break;

			// Delete a network object
			case EMessageType.RemoveGameObject:
				RemoveObjectMessage removeObject = Marshal.PtrToStructure<RemoveObjectMessage>(message.m_pData + 1);
				Debug.Log("Remove object " + removeObject.m_networkID);

				NetworkObjectManager.RemoveObject(removeObject);
				break;

			// Update a network behaviour
			case EMessageType.NetworkBehaviourUpdate:
				NetworkBehaviour.ProcessUpdateMessage(message);
				break;

			// Connect to another peer
			case EMessageType.AddPeer:
				AddPeerMessage connectMessage = Marshal.PtrToStructure<AddPeerMessage>(message.m_pData + 1);
				Debug.LogWarning("Connecting to peer " + connectMessage.m_steamIdentity.GetSteamID64());

				SteamNetworkingSockets.ConnectP2P(ref connectMessage.m_steamIdentity, 0, 0, null);
				break;

			// Receive voice
			case EMessageType.VoiceData:
				if (VoiceManager.Instance != null)
					VoiceManager.Instance.ReceiveVoice(message, sender);
				break;

			default:
				Debug.LogWarning("Unknown message type " + type);
				break;
		}
	}


	void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
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
			if (pCallback.m_hConn != m_server.m_hConn)
			{
				Peer peer = new()
				{
					m_identity = pCallback.m_info.m_identityRemote,
					m_hConn = pCallback.m_hConn,
					m_player = NetworkObjectManager.GetNetworkObject(m_peerIDs[pCallback.m_info.m_identityRemote])
				};
				m_peers.Add(peer);
				Debug.LogWarning("Peer added");
			}
		}
	}
}