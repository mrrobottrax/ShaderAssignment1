using System.Runtime.InteropServices;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Steamworks;
using UnityEditor.PackageManager;

internal static class SendFunctions
{
	// Send NetVars of a NetworkBehaviour
	public static void SendNetworkBehaviourUpdate(int networkID, int componentIndex, byte[] data, Peer recepient = null)
	{
		byte[] buffer = NetworkBehaviour.CreateMessage(networkID, componentIndex, data);

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
					EMessageType.NetworkBehaviourUpdate,
					pMessage, buffer.Length,
					ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
				);
			}
			else
			{
				NetworkManager.SendMessage(
					EMessageType.NetworkBehaviourUpdate,
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
	public static void SendObjectSnapshot(NetworkObject obj, Peer recepient = null)
	{
		foreach (var net in obj.m_networkBehaviours)
		{
			SendNetworkBehaviourUpdate(obj.m_netID, net.m_index, net.GetNetVarBytes(), recepient);
		}
	}

	// Send this client the scene index
	public static void SendSceneInfo(Peer client = null)
	{
		SceneChangeMessage message = new()
		{
			m_sceneIndex = SceneManager.GetActiveScene().buildIndex,
		};

		if (client != null)
		{
			NetworkManager.SendMessage(
				EMessageType.SceneChange,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
				client.m_hConn
			);
		}
		else
		{
			NetworkManager.SendMessageAll(
				EMessageType.SceneChange,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
			);
		}
	}

	public static void SendFullSnapshot(Peer client = null)
	{
		foreach (var obj in NetworkObjectManager.GetSceneNetObjects())
		{
			// Check if this object is part of the scene
			if (obj.m_prefabIndex == -1)
			{
				SendObjectSnapshot(obj, client);
			}
			else
			{
				SendSpawnPrefab(obj.m_netID, obj.m_prefabIndex, obj.m_ownerIndentity, client);
				SendObjectSnapshot(obj, client);
			}
		}
	}

	public static void SendPeers(Peer recepient)
	{
		foreach (var client in NetworkManager.m_peers.Values)
		{
			if (client != recepient)
			{
				AddPeerMessage message = new()
				{
					m_steamIdentity = client.m_identity,
					m_networkID = client.m_player.m_netID
				};

				NetworkManager.SendMessage(
					EMessageType.AddPeer,
					message,
					ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
					recepient.m_hConn
				);
			}
		}
	}

	public static void SendSpawnPrefab(int networkID, int prefabIndex, SteamNetworkingIdentity ownerIdentity, Peer recepient = null)
	{
		if (prefabIndex == -1)
		{
			Debug.LogError($"Trying to spawn NetworkObject not added as a prefab! {prefabIndex} : {networkID}");
			return;
		}

		SpawnPrefabMessage message = new()
		{
			m_networkID = networkID,
			m_prefabIndex = prefabIndex,
			m_ownerIdentity = ownerIdentity,
		};

		if (recepient != null)
		{
			NetworkManager.SendMessage(
				EMessageType.SpawnPrefab,
				message,
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
				recepient.m_hConn
			);
		}
		else
		{
			NetworkManager.SendMessageAll(
				EMessageType.SpawnPrefab,
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
			EMessageType.RemoveGameObject,
			message,
			ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable
		);
	}
}
