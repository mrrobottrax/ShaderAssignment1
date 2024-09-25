using Steamworks;
using System.Collections.Generic;
using UnityEngine;

internal static class NetworkObjectManager
{
	static int m_lastID = 0;
	static int m_lastPersistentID = 0;

	readonly static Dictionary<int, NetworkObject> m_netObjects = new();
	readonly static Dictionary<int, NetworkObject> m_persistentNetObjects = new(); // DontDestroyOnLoad, includes players


	public static NetworkObject GetNetworkObject(int networkID)
	{
		if (networkID < 0)
		{
			return m_persistentNetObjects[networkID];
		}
		else
		{
			return m_netObjects[networkID];
		}
	}

	internal static void AddNetworkObjectToList(NetworkObject networkObject)
	{
		// Find which dict this object goes in

		if (networkObject.gameObject.scene.buildIndex == -1)
		{
			m_persistentNetObjects.Add(networkObject.m_netID, networkObject);
		}
		else
		{
			m_netObjects.Add(networkObject.m_netID, networkObject);
		}
	}

	internal static void RemoveNetworkObjectFromList(NetworkObject networkObject)
	{
		GetNetworkObject(networkObject.m_netID);
	}

	internal static int ReserveID(NetworkObject networkObject)
	{
		if (networkObject.gameObject.scene.buildIndex == -1)
		{
			// Use persistent NetID
			return --m_lastPersistentID;
		}
		else
		{
			// Use scene NetID
			return ++m_lastID;
		}
	}

	internal static void ResetIDs()
	{
		m_lastID = 0;
	}

	internal static void ResetPersistentIDs()
	{
		ResetIDs();
		m_lastPersistentID = 0;
	}

	internal static Dictionary<int, NetworkObject>.ValueCollection GetNetObjects()
	{
		return m_netObjects.Values;
	}

	internal static Dictionary<int, NetworkObject>.ValueCollection GetPersistentNetObjects()
	{
		return m_persistentNetObjects.Values;
	}

	internal static NetworkObject SpawnNetworkPrefab(SpawnPrefabMessage message, Peer sender)
	{
		if (message.m_prefabIndex == -1)
		{
			Debug.LogError("Only network prefabs can be spawned!");
			return null;
		}

		// Get the prefab
		GameObject prefab;

		if (message.m_prefabIndex == NetworkData.k_playerPrefabIndex)
		{
			prefab = NetworkData.GetPlayerPrefab();
		}
		else
		{
			prefab = NetworkData.GetNetworkPrefabs()[message.m_prefabIndex];
		}

		// Spawn it
		GameObject goPrefab = Object.Instantiate(prefab);
		NetworkObject netObj = goPrefab.GetComponent<NetworkObject>();

		netObj.m_netID = message.m_networkID;
		netObj.m_ownerIndentity = message.m_ownerIdentity;

		if (message.m_networkID < 0)
		{
			Object.DontDestroyOnLoad(goPrefab);
		}

		// Players get added to dictionary
		if (netObj.m_ownerIndentity.Equals(sender.m_identity))
		{
			sender.m_player = netObj;
		}

		// Check if local player
		if (netObj.m_ownerIndentity.Equals(NetworkManager.m_localIdentity))
		{
			NetworkManager.m_localClient.m_player = netObj;
		}

		// Add to list
		AddNetworkObjectToList(netObj);

		// Set IsOwner of NetworkBehaviours
		foreach (var component in netObj.m_networkBehaviours)
		{
			component.IsOwner = netObj.m_ownerIndentity.Equals(NetworkManager.m_localIdentity);
		}

		return netObj;
	}

	internal static void RemoveObject(RemoveObjectMessage message)
	{
		Object.Destroy(GetNetworkObject(message.m_networkID).gameObject);
	}
}
