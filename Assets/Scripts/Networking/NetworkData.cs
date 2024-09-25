using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class NetworkData : ScriptableObject
{
	#region Singleton
	static NetworkData s_instance;


	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	static void OnRuntimeLoad()
	{
		s_instance = Resources.LoadAll<NetworkData>("")[0];
	}

	[InitializeOnLoadMethod]
	static void OnEditorLoad()
	{
		s_instance = Resources.LoadAll<NetworkData>("")[0];
		Debug.Log("Loaded singleton");
	}
	#endregion

	[SerializeField] int m_ticksPerSecond = 24;

	internal const int k_playerPrefabIndex = -256;
	internal const int k_maxMessages = 64;

	const int k_minSpawnedObjectIDCount = 2000;

	[SerializeField] GameObject m_playerPrefab;
	[SerializeField] GameObject[] m_networkPrefabs;


	[SerializeField] Dictionary<int, NetworkObject> m_reservedNetIDs = new();

	private void OnValidate()
	{
#if UNITY_EDITOR
		// Player gets a special index
		if (m_playerPrefab != null)
		{
			m_playerPrefab.GetComponent<NetworkObject>().m_prefabIndex = k_playerPrefabIndex;
			EditorUtility.SetDirty(m_playerPrefab);
		}

		if (m_networkPrefabs != null)
		{
			// Set prefab indices
			for (int i = 0; i < m_networkPrefabs.Length; ++i)
			{
				if (m_networkPrefabs[i] != null)
				{
					m_networkPrefabs[i].GetComponent<NetworkObject>().m_prefabIndex = i;
					EditorUtility.SetDirty(m_networkPrefabs[i]);
				}
			}
		}
#endif
	}


	public static GameObject GetPlayerPrefab()
	{
		return s_instance.m_playerPrefab;
	}

	public static GameObject[] GetNetworkPrefabs()
	{
		return s_instance.m_networkPrefabs;
	}

	public static float GetTickDelta()
	{
		return 1.0f / s_instance.m_ticksPerSecond;
	}


	public static int AddSceneObject(NetworkObject obj)
	{
		// Try IDs from int.max
		for (int i = int.MaxValue; i > k_minSpawnedObjectIDCount; i--)
		{
			if (s_instance.m_reservedNetIDs.TryGetValue(i, out NetworkObject obj2))
			{
				if (obj2 == null)
				{
					s_instance.m_reservedNetIDs[i] = obj;
					return i;
				}

				continue;
			}

			s_instance.m_reservedNetIDs.Add(i, obj);
			return i;
		}

		Debug.LogError("Out of IDs for scene objects. This is really fucked up.");
		return 0;
	}

	public static bool HasSceneGameObject(NetworkObject obj)
	{
		if (s_instance.m_reservedNetIDs.TryGetValue(obj.m_netID, out NetworkObject obj2))
		{
			if (obj2.Equals(obj))
			{
				return true;
			}

			return false;
		}

		return false;
	}
}