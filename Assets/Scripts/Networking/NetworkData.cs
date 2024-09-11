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
	#endregion

	[SerializeField] int m_ticksPerSecond = 24;

	public const int k_playerPrefabIndex = -256;

	[SerializeField] GameObject m_playerPrefab;
	[SerializeField] GameObject[] m_networkPrefabs;


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
}