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
	internal bool m_ignoreObjectUpdates = false;

	readonly Queue<Tuple<IntPtr, Peer>> m_messageBackup = new();

	internal Peer m_hostPeer;
	internal Dictionary<SteamNetworkingIdentity, Peer> m_peers = new();

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
		foreach (Peer peer in NetworkManager.GetAllPeers())
		{
			SteamNetworkingSockets.CloseConnection(peer.m_hConn, 0, null, true);
		}

	}

	#region External Calls

	internal void Connect(SteamNetworkingIdentity host)
	{
		HSteamNetConnection hConn = SteamNetworkingSockets.ConnectP2P(ref host, 0, 0, null);
		m_hostPeer = new(hConn, host, null);

		m_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
	}

	private void Update()
	{
		foreach (var peer in NetworkManager.GetAllPeers())
		{
			ReceiveMessages(peer);
			SteamNetworkingSockets.FlushMessagesOnConnection(peer.m_hConn);
		}
	}

	void ReceiveMessages(Peer sender)
	{
		IntPtr[] pMessages = new IntPtr[NetworkData.k_maxMessages];
		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(sender.m_hConn, pMessages, pMessages.Length);

		for (int i = 0; i < messageCount; ++i)
		{
			SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(pMessages[i]);

			// MessageID_t id = MessageID_t.Read(message.m_pData);
			// Type type = NetworkManager.m_IdToMessage[id];

			// todo: allow voice to pass
			bool isObjectUpdate = true;

			if (!(isObjectUpdate && m_ignoreObjectUpdates))
			{
				NetworkManager.ProcessMessage(message, sender);

				// Free data
				SteamNetworkingMessage_t.Release(pMessages[i]);
			}
			else
			{
				m_messageBackup.Enqueue(new Tuple<IntPtr, Peer>(pMessages[i], sender));
			}
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
			NetworkManager.ProcessMessage(mess, message.Item2);

			SteamNetworkingMessage_t.Release(message.Item1);
		}

		// Tell the server we've loaded
		// (if we don't have a player yet, the load message is sent when we spawn)
		if (m_player)
		{
			SendFinishedLoading();
		}
	}

	#endregion

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
			m_peers.Add(pCallback.m_info.m_identityRemote, peer);
		}
	}

	void SendFinishedLoading()
	{
		NetworkManager.SendMessage(new SceneChangeMessage(), m_hostPeer);
	}
}