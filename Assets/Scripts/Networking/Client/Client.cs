using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class Client : MonoBehaviour
{
	Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	HSteamListenSocket m_listenSocket;
	bool m_ignoreObjectUpdates = false;

	Queue<Tuple<IntPtr, Peer>> m_messageBackup = new();

	internal SteamNetworkingIdentity m_serverID;

	internal NetworkObject m_player;

	private void Awake()
	{
		m_SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);

		SceneManager.sceneLoaded += OnSceneLoad;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoad;

		// Close connections
		if (!SteamManager.Initialized) return;
		m_SteamNetConnectionStatusChanged.Dispose();

		SteamNetworkingSockets.CloseListenSocket(m_listenSocket);
		foreach (Peer peer in NetworkManager.m_peers.Values)
		{
			SteamNetworkingSockets.CloseConnection(peer.m_hConn, 0, null, true);
		}

	}

	#region External Calls

	internal void Connect(SteamNetworkingIdentity host)
	{
		m_serverID = host;
		SteamNetworkingSockets.ConnectP2P(ref host, 0, 0, null);

		m_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
	}

	private void Update()
	{
		foreach (var peer in NetworkManager.m_peers.Values)
		{
			ReceiveMessages(peer);
			SteamNetworkingSockets.FlushMessagesOnConnection(peer.m_hConn);
		}
	}

	private void OnSceneLoad(Scene scene, LoadSceneMode mode)
	{
		StartCoroutine(FinishLoadingNextFrame());
	}

	// Has to wait for Awake and Start
	IEnumerator FinishLoadingNextFrame()
	{
		yield return null;

		m_ignoreObjectUpdates = false;

		// Run through queue
		while (m_messageBackup.TryDequeue(out var message))
		{
			SteamNetworkingMessage_t mess = Marshal.PtrToStructure<SteamNetworkingMessage_t>(message.Item1);
			ProcessMessage(mess, message.Item2);

			SteamNetworkingMessage_t.Release(message.Item1);
		}

		// Tell the server we've loaded
		// (if we don't have a player yet, the load message is sent when we spawn)
		if (!m_serverID.Equals(default) && m_player)
		{
			SendFinishedLoading();
		}
	}

	#endregion

	void ReceiveMessages(Peer sender)
	{
		IntPtr[] pMessages = new IntPtr[NetworkData.k_maxMessages];
		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(sender.m_hConn, pMessages, pMessages.Length);

		for (int i = 0; i < messageCount; ++i)
		{
			SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(pMessages[i]);

			if (ProcessMessage(message, sender))
			{
				// Free data
				SteamNetworkingMessage_t.Release(pMessages[i]);
			}
			else
			{
				m_messageBackup.Enqueue(new Tuple<IntPtr, Peer>(pMessages[i], sender));
			}
		}
	}

	bool ProcessMessage(SteamNetworkingMessage_t message, Peer sender)
	{
		EMessageType type = (EMessageType)Marshal.ReadByte(message.m_pData);

		switch (type)
		{
			// Load scene
			case EMessageType.SceneChange:
				SceneChangeMessage sceneChange = Marshal.PtrToStructure<SceneChangeMessage>(message.m_pData + 1);
				SceneManager.LoadSceneAsync(sceneChange.m_sceneIndex);
				m_ignoreObjectUpdates = true;
				break;

			// Spawn a network prefab
			case EMessageType.SpawnPrefab:
				if (m_ignoreObjectUpdates) return false;

				SpawnPrefabMessage spawnPrefab = Marshal.PtrToStructure<SpawnPrefabMessage>(message.m_pData + 1);
				Debug.Log("Object spawn " + spawnPrefab.m_networkID + " : " + spawnPrefab.m_prefabIndex);

				if (spawnPrefab.m_ownerIdentity.Equals(NetworkManager.LocalIdentity))
				{
					// Tell the server we've loaded
					if (!m_serverID.Equals(default) && m_player)
					{
						SendFinishedLoading();
					}
				}

				NetworkObjectManager.SpawnNetworkPrefab(spawnPrefab, sender);
				break;

			// Delete a network object
			case EMessageType.RemoveGameObject:
				if (m_ignoreObjectUpdates) return false;

				RemoveObjectMessage removeObject = Marshal.PtrToStructure<RemoveObjectMessage>(message.m_pData + 1);
				Debug.Log("Remove object " + removeObject.m_networkID);

				NetworkObjectManager.RemoveObject(removeObject);
				break;

			// Update a network behaviour
			case EMessageType.NetworkBehaviourUpdate:
				if (m_ignoreObjectUpdates) return false;

				NetworkBehaviour.ProcessUpdateMessage(message);
				break;

			// Connect to another peer
			case EMessageType.AddPeer:
				AddPeerMessage connectMessage = Marshal.PtrToStructure<AddPeerMessage>(message.m_pData + 1);
				Debug.Log("Connecting to peer " + connectMessage.m_steamIdentity.GetSteamID64());

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

		return true;
	}

	void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
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
			Peer peer = new(pCallback.m_hConn, pCallback.m_info.m_identityRemote, null);
			NetworkManager.m_peers.Add(pCallback.m_info.m_identityRemote, peer);
		}
	}

	void SendFinishedLoading()
	{
		SceneChangeMessage message = new()
		{
			m_sceneIndex = SceneManager.GetActiveScene().buildIndex,
		};

		NetworkManager.SendMessage(
			EMessageType.SceneChange,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
			NetworkManager.m_peers[m_serverID].m_hConn
		);
	}
}