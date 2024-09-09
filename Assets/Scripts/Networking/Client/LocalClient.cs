using Steamworks;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class LocalClient : MonoBehaviour
{
	protected Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChanged;

	private SteamNetworkingIdentity m_server;
	internal static HSteamNetConnection m_hConn;

	internal static int m_playerObjectID;

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

	public void Init(SteamNetworkingIdentity server)
	{
		m_server = server;
	}

	private void Start()
	{
		SteamNetworkingSockets.ConnectP2P(ref m_server, 0, 0, null);
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
			SteamNetworkingSockets.FlushMessagesOnConnection(m_hConn);
		}
	}

	private void ReceiveMessages()
	{
		IntPtr[] pMessages = new IntPtr[64];

		int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(m_hConn, pMessages, pMessages.Length);

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
			case ESnapshotMessageType.SceneChange:
				SceneChangeMessage sceneChange = Marshal.PtrToStructure<SceneChangeMessage>(message.m_pData + 1);

				m_playerObjectID = sceneChange.m_playerObjectID;
				SceneManager.LoadScene(sceneChange.m_sceneIndex);
				Debug.Log("Scene change " + sceneChange.m_playerObjectID);
				break;

			case ESnapshotMessageType.SpawnPrefab:
				SpawnPrefabMessage spawnPrefab = Marshal.PtrToStructure<SpawnPrefabMessage>(message.m_pData + 1);

				NetworkObjectManager.SpawnNetworkPrefab(spawnPrefab);
				Debug.Log("Object spawn");
				break;

			case ESnapshotMessageType.RemoveGameObject:
				RemoveObjectMessage removeObject = Marshal.PtrToStructure<RemoveObjectMessage>(message.m_pData + 1);

				NetworkObjectManager.RemoveObject(removeObject);
				Debug.Log("Remove object");
				break;

			case ESnapshotMessageType.NetworkBehaviourUpdate:
				NetworkBehaviour.ProcessUpdateMessage(message);
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

			m_hConn = pCallback.m_hConn;
		}
	}
}