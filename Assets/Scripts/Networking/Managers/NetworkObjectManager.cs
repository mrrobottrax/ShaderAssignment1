using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class NetworkObjectManager
{
	static int m_lastID = 0;
	static int m_lastPersistentID = 0;

	readonly static Dictionary<int, NetworkObject> m_netObjects = new();
	readonly static Dictionary<int, NetworkObject> m_persistentNetObjects = new(); // DontDestroyOnLoad, includes players

	public static void Init()
	{
		SceneManager.activeSceneChanged += SceneChange;
	}

	static void SceneChange(Scene oldScene, Scene newScene)
	{
		m_lastID = 0;
	}

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
		if (networkObject.m_netID < 0)
		{
			m_persistentNetObjects.Remove(networkObject.m_netID);
		}
		else
		{
			m_netObjects.Remove(networkObject.m_netID);
		}
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

	internal static Dictionary<int, NetworkObject>.ValueCollection GetSceneNetObjects()
	{
		return m_netObjects.Values;
	}

	internal static Dictionary<int, NetworkObject>.ValueCollection GetPersistentNetObjects()
	{
		return m_persistentNetObjects.Values;
	}

	internal static NetworkObject SpawnNetworkPrefab(SpawnPrefabMessage message, Peer sender)
	{
		if (message.m_prefabID == -1)
		{
			Debug.LogError("Only network prefabs can be spawned!");
			return null;
		}

		// Get the prefab
		GameObject prefab;

		bool isPlayer = message.m_prefabID == NetworkData.k_playerPrefabIndex;
		if (isPlayer)
		{
			prefab = NetworkData.GetPlayerPrefab();
		}
		else
		{
			prefab = NetworkData.GetNetworkPrefabs()[message.m_prefabID];
		}

		// Spawn it
		GameObject goPrefab = Object.Instantiate(prefab);
		NetworkObject netObj = goPrefab.GetComponent<NetworkObject>();

		netObj.m_netID = message.m_netID;
		netObj.m_ownerIndentity = message.m_ownerID;

		// Don't destroy on load
		if (message.m_netID < 0)
		{
			Object.DontDestroyOnLoad(goPrefab);
		}

		// Set peer player object
		if (isPlayer && netObj.m_ownerIndentity.Equals(sender.m_identity))
		{
			sender.m_player = netObj;
		}

		// Check if local player
		if (isPlayer && netObj.m_ownerIndentity.Equals(NetworkManager.m_localIdentity))
		{
			NetworkManager.m_localClient.m_player = netObj;
		}

		netObj.Init();

		return netObj;
	}

	// internal static void RemoveObject(RemoveObjectMessage message)
	// {
	// 	Object.Destroy(GetNetworkObject(message.m_networkID).gameObject);
	// }
}
