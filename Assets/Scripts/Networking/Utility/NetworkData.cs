﻿using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal class NetworkData : ScriptableObject
{
	#region Singleton
	static NetworkData s_instance;


	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	static void OnRuntimeLoad()
	{
		NetworkData[] data = Resources.LoadAll<NetworkData>("");

		if (data == null)
		{
			Debug.LogError("No NetworkData found");
			return;
		}

		s_instance = data[0];
	}

	[InitializeOnLoadMethod]
	static void OnEditorLoad()
	{
		NetworkData[] data = Resources.LoadAll<NetworkData>("");

		if (data == null)
		{
			Debug.Log("No NetworkData found");
			return;
		}

		s_instance = data[0];
		//Debug.Log("Loaded singleton");

		// Copy lists into dict
		s_instance.m_unsavedDict = new();
		for (int i = 0; i < s_instance.m_savedIDs.Count; i++)
		{
			GlobalObjectId.TryParse(s_instance.m_savedGlobalIDs[i], out GlobalObjectId id);
			s_instance.m_unsavedDict.Add(s_instance.m_savedIDs[i], id);
		}
	}
	#endregion

	[SerializeField] int m_ticksPerSecond = 24;

	internal const int k_playerPrefabIndex = -256;
	internal const int k_maxMessages = 64;

	const int k_minSpawnedObjectIDCount = 10000;

	[SerializeField] GameObject m_playerPrefab;
	[SerializeField] GameObject[] m_networkPrefabs;

	[SerializeField, HideInInspector] List<int> m_savedIDs = new();
	[SerializeField, HideInInspector] List<string> m_savedGlobalIDs = new();

	Dictionary<int, GlobalObjectId> m_unsavedDict = new();

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

	static void SaveDict()
	{
		s_instance.m_savedIDs.Clear();
		s_instance.m_savedGlobalIDs.Clear();
		foreach (var item in s_instance.m_unsavedDict)
		{
			s_instance.m_savedIDs.Add(item.Key);
			s_instance.m_savedGlobalIDs.Add(item.Value.ToString());
		}

		EditorUtility.SetDirty(s_instance);
	}

	public static int AddSceneObject(NetworkObject obj)
	{
		// Find a free ID
		for (int id = int.MaxValue; id > k_minSpawnedObjectIDCount; --id)
		{
			// Check if there is an entry here
			if (s_instance.m_unsavedDict.ContainsKey(id))
			{
				continue;
			}

			s_instance.m_unsavedDict.Add(id, GlobalObjectId.GetGlobalObjectIdSlow(obj));

			SaveDict();

			return id;
		}

		Debug.LogError("Out of IDs for scene objects. This is really fucked up.");
		return 0;
	}

	public static bool HasSceneGameObject(NetworkObject obj)
	{
		if (s_instance == null) return true;

		if (s_instance.m_unsavedDict.TryGetValue(obj.m_netID, out GlobalObjectId globalID))
		{
			// Check if this is really the same object
			GlobalObjectId realID = GlobalObjectId.GetGlobalObjectIdSlow(obj);

			if (globalID.Equals(realID)) return true;
		}

		return false;
	}

	[MenuItem("NetCode/Remove unused IDs")]
	// todo: re-assign ids to be sequential
	public static void CleanupIDs()
	{
		if (s_instance == null) return;

		HashSet<string> scenes = new();
		HashSet<string> startingScenes = new();

		// Add all open scenes to set
		for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
		{
			scenes.Add(EditorSceneManager.GetSceneAt(i).path);
			startingScenes.Add(EditorSceneManager.GetSceneAt(i).path);
		}

		List<int> removedKeys = new();

		// Check if each object exists
		foreach (var kv in s_instance.m_unsavedDict)
		{
			if (kv.Value.identifierType != 2)
			{
				Debug.Log($"{GlobalObjectId.GlobalObjectIdentifierToObjectSlow(kv.Value)} is not a scene object. Removing.");
				s_instance.m_unsavedDict.Remove(kv.Key);
				continue;
			}

			// Load it's scene if it isn't already
			string scenePath = AssetDatabase.GUIDToAssetPath(kv.Value.assetGUID);
			if (!scenes.Contains(scenePath))
			{
				EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
				scenes.Add(scenePath);

				//Debug.Log($"Loading scene {scenePath}");
			}

			object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(kv.Value);

			if (obj == null)
			{
				Debug.Log($"Removing object from scene: {scenePath}");
				removedKeys.Add(kv.Key);
			}
		}

		foreach (var key in removedKeys)
		{
			s_instance.m_unsavedDict.Remove(key);
		}

		// Unload all scenes except original
		foreach (string scenePath in scenes)
		{
			if (!startingScenes.Contains(scenePath))
				EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(scenePath), true);
		}

		SaveDict();

		return;
	}
}