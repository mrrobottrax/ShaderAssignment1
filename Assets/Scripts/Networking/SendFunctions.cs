using System.Runtime.InteropServices;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

internal static class SendFunctions
{
	public static void SendConnectAck(RemoteClient recepient)
	{
		ConnectAckMessage message = new()
		{
			m_playerObjectID = recepient.m_player.m_netID
		};

		NetworkManager.SendMessage(
			ESnapshotMessageType.ConnectAck,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
			recepient.m_hConn
		);
	}

	// Send NetVars of a NetworkBehaviour
	public static void SendNetworkBehaviourUpdate(int networkID, int componentIndex, byte[] data, RemoteClient recepient = null)
	{
		byte[] buffer = NetworkBehaviour.CreateMessageBuffer(networkID, componentIndex, data);

		// Pin the buffer in memory
		GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			// Get the pointer to the buffer
			IntPtr pMessage = handle.AddrOfPinnedObject();

			// Send the message
			if (recepient == null)
			{
				NetworkManager.SendMessageAll(
					ESnapshotMessageType.NetworkBehaviourUpdate,
					pMessage, buffer.Length,
					ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
				);
			}
			else
			{
				NetworkManager.SendMessage(
					ESnapshotMessageType.NetworkBehaviourUpdate,
					pMessage, buffer.Length,
					ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
					recepient.m_hConn
				);
			}
		}
		finally
		{
			// Free the pinned object
			handle.Free();
		}
	}

	// Send a full snapshot of an object
	public static void SendObjectSnapshot(NetworkObject obj, RemoteClient recepient = null)
	{
		foreach (var net in obj.m_networkBehaviours)
		{
			SendNetworkBehaviourUpdate(obj.m_netID, net.m_index, net.GetNetVarBytes(), recepient);
		}
	}

	// Send this client the scene index
	public static void SendSceneInfo(RemoteClient client = null)
	{
		SceneChangeMessage message = new()
		{
			m_sceneIndex = SceneManager.GetActiveScene().buildIndex,
		};

		if (client != null)
		{
			NetworkManager.SendMessage(
				ESnapshotMessageType.SceneChange,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
				client.m_hConn
			);
		}
		else
		{
			NetworkManager.SendMessageAll(
				ESnapshotMessageType.SceneChange,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
			);
		}

		// Send new objects
		foreach (var networkObject in NetworkObjectManager.GetNetObjects())
		{
			SendSpawnPrefab(networkObject.m_netID, networkObject.m_prefabIndex, networkObject.m_ownerID, client);
			SendObjectSnapshot(networkObject, client);
		}
	}

	public static void SendPeers(RemoteClient recepient)
	{
		foreach (var client in Host.m_clients.Values)
		{
			if (client != recepient)
			{
				NewPeerMessage message = new()
				{
					m_steamIdentity = client.m_identity,
				};

				NetworkManager.SendMessage(
					ESnapshotMessageType.NewPeer,
					message,
					ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
					recepient.m_hConn
				);
			}
		}
	}

	public static void SendSpawnPrefab(int networkID, int prefabIndex, int ownerID, RemoteClient recepient = null)
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

		if (recepient != null)
		{
			NetworkManager.SendMessage(
				ESnapshotMessageType.SpawnPrefab,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
				recepient.m_hConn
			);
		}
		else
		{
			NetworkManager.SendMessageAll(
				ESnapshotMessageType.SpawnPrefab,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
			);
		}
	}

	public static void SendDestroyGameObject(int networkID)
	{
		RemoveObjectMessage message = new()
		{
			m_networkID = networkID
		};

		NetworkManager.SendMessageAll(
			ESnapshotMessageType.RemoveGameObject,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
		);
	}
}
