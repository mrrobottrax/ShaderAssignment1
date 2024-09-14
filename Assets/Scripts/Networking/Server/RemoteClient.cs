using Steamworks;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ESteamNetworkingSend : int
{
	k_nSteamNetworkingSend_Unreliable = 0,
	k_nSteamNetworkingSend_NoNagle = 1,

	k_nSteamNetworkingSend_UnreliableNoNagle = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoNagle,

	k_nSteamNetworkingSend_NoDelay = 4,

	k_nSteamNetworkingSend_UnreliableNoDelay = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoDelay | k_nSteamNetworkingSend_NoNagle,

	k_nSteamNetworkingSend_Reliable = 8,

	k_nSteamNetworkingSend_ReliableNoNagle = k_nSteamNetworkingSend_Reliable | k_nSteamNetworkingSend_NoNagle
}

public class RemoteClient
{
	internal HSteamNetConnection m_hConn;
	SteamNetworkingIdentity m_identity;
	internal readonly NetworkObject m_player;

	public RemoteClient(HSteamNetConnection hConn, SteamNetworkingIdentity identity, NetworkObject player)
	{
		m_hConn = hConn;
		m_identity = identity;
		m_player = player;
	}

	public void UpdateConnection(HSteamNetConnection hConn)
	{
		m_hConn = hConn;
	}

	// Send all queued messages
	public void FlushQueuedMessages()
	{
		SteamNetworkingSockets.FlushMessagesOnConnection(m_hConn);
	}

	// Send this client the scene index
	public void SendSceneInfo()
	{
		SceneChangeMessage message = new()
		{
			m_sceneIndex = SceneManager.GetActiveScene().buildIndex,
			m_playerObjectID = m_player.m_netID
		};

		NetworkManager.SendMessage(
			ESnapshotMessageType.SceneChange,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
			m_hConn
		);

		// Send new objects
		foreach (var networkObject in NetworkObjectManager.GetNetObjects())
		{
			SendSpawnPrefab(networkObject.m_netID, networkObject.m_prefabIndex, networkObject.m_ownerID);
			networkObject.SendFullSnapshotToClient(this);
		}
	}

	public void SendPeers()
	{
		foreach (var client in Host.m_clients.Values)
		{
			if (client != this)
			{
				NewPeerMessage message = new()
				{
					m_steamIdentity = client.m_identity,
				};

				NetworkManager.SendMessage(
					ESnapshotMessageType.SpawnPrefab,
					message,
					ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
					m_hConn
				);
			}
		}
	}

	public void SendSpawnPrefab(int networkID, int prefabIndex, int ownerID)
	{
		if (prefabIndex == -1)
		{
			Debug.LogError("Trying to spawn NetworkObject not added as a prefab!");
			return;
		}

		SpawnPrefabMessage message = new()
		{
			m_networkID = networkID,
			m_prefabIndex = prefabIndex,
			m_ownerID = ownerID
		};

		NetworkManager.SendMessage(
			ESnapshotMessageType.SpawnPrefab,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
			m_hConn
		);
	}

	public void SendDestroyGameObject(int networkID)
	{
		RemoveObjectMessage message = new()
		{
			m_networkID = networkID
		};

		NetworkManager.SendMessage(
			ESnapshotMessageType.RemoveGameObject,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
			m_hConn
		);
	}

	public void SendNetworkBehaviourUpdate(int networkID, int componentIndex, byte[] data)
	{
		byte[] buffer = NetworkBehaviour.CreateMessageBuffer(networkID, componentIndex, data);

		// Pin the buffer in memory
		GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			// Get the pointer to the buffer
			IntPtr pMessage = handle.AddrOfPinnedObject();

			// Send the message
			NetworkManager.SendMessage(
				ESnapshotMessageType.NetworkBehaviourUpdate,
				pMessage, buffer.Length,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
				m_hConn
			);
		}
		finally
		{
			// Free the pinned object
			handle.Free();
		}
	}
}