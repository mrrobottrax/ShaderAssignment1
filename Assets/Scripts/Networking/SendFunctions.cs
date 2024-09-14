﻿using System.Runtime.InteropServices;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Steamworks;

internal static class SendFunctions
{
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

		// Send new objects not included in scene file
		// todo: this is just all of them
		foreach (var networkObject in NetworkObjectManager.GetNetObjects())
		{
			SendSpawnPrefab(networkObject.m_netID, networkObject.m_prefabIndex, networkObject.m_ownerIndentity, client);
			SendObjectSnapshot(networkObject, client);
		}
	}

	public static void SendPeers(RemoteClient recepient)
	{
		foreach (var client in NetworkManager.m_host.m_clients.Values)
		{
			if (client != recepient)
			{
				AddPeerMessage message = new()
				{
					m_steamIdentity = client.m_identity,
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

	public static void SendSpawnPrefab(int networkID, int prefabIndex, SteamNetworkingIdentity ownerIdentity, RemoteClient recepient = null)
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
