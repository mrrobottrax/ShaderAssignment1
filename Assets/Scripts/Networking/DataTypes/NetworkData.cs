using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

internal class NetworkData : ScriptableObject
{
#if UNITY_EDITOR
	class NetworkDataLoader : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			NetworkData.Load();
		}
	}
#endif

	#region Singleton
	static NetworkData s_instance;
	static NetworkData Instance
	{
		get
		{
			if (s_instance == null)
			{
				Load();
			}
			return s_instance;
		}
	}


	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	static void OnRuntimeLoad()
	{
		Load();
	}

	static void Load()
	{
		NetworkData[] data = Resources.LoadAll<NetworkData>("");

		if (data == null || data.Length == 0)
		{
			Debug.LogError("No NetworkData found");
			return;
		}

		//Debug.Log("Loaded NetworkData");

		s_instance = data[0];

#if UNITY_EDITOR
		// Copy lists into dict
		s_instance.m_unsavedDict = new();
		for (int i = 0; i < s_instance.m_savedIDs.Count; i++)
		{
			GlobalObjectId.TryParse(s_instance.m_savedGlobalIDs[i], out GlobalObjectId id);
			s_instance.m_unsavedDict.Add(s_instance.m_savedIDs[i], id);
		}
#endif
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

#if UNITY_EDITOR
	Dictionary<int, GlobalObjectId> m_unsavedDict = new();
#endif

	bool m_disableIdCreation = false;

#if UNITY_EDITOR
	private void OnValidate()
	{
		// Player gets a special index
		if (m_playerPrefab != null)
		{
			m_playerPrefab.GetComponent<NetworkObject>().m_prefabIndex = k_playerPrefabIndex;
			m_playerPrefab.GetComponent<NetworkObject>().m_netID = 0;
			EditorUtility.SetDirty(m_playerPrefab);
		}

		if (m_networkPrefabs != null)
		{
			// Set prefab indices
			for (int i = 0; i < m_networkPrefabs.Length; ++i)
			{
				if (m_networkPrefabs[i] != null)
				{
					if (!m_networkPrefabs[i].TryGetComponent<NetworkObject>(out var obj))
					{
						Debug.LogWarning($"Network prefab {m_networkPrefabs[i].name} doesn't have a NetworkObject");
						continue;
					}

					obj.m_prefabIndex = i;
					obj.m_netID = 0;
					EditorUtility.SetDirty(m_networkPrefabs[i]);
				}
			}
		}
	}
#endif


	public static GameObject GetPlayerPrefab()
	{
		return Instance.m_playerPrefab;
	}

	public static GameObject[] GetNetworkPrefabs()
	{
		return Instance.m_networkPrefabs;
	}

	public static float GetTickDelta()
	{
		return 1.0f / Instance.m_ticksPerSecond;
	}

#if UNITY_EDITOR
	static void SaveDict()
	{
		Instance.m_savedIDs.Clear();
		Instance.m_savedGlobalIDs.Clear();
		foreach (var item in Instance.m_unsavedDict)
		{
			Instance.m_savedIDs.Add(item.Key);
			Instance.m_savedGlobalIDs.Add(item.Value.ToString());
		}

		EditorUtility.SetDirty(Instance);
	}
#endif

#if UNITY_EDITOR
	public static int AddSceneObject(NetworkObject obj)
	{
		// Check if the saved ID is free
		if (obj.m_netID > k_minSpawnedObjectIDCount && !Instance.m_unsavedDict.ContainsKey(obj.m_netID))
		{
			Debug.Log($"Object ({GlobalObjectId.GetGlobalObjectIdSlow(obj)}) saved with an ID ({obj.m_netID}) but the ID is not present in the dictionary. Most likely NetworkData wasn't saved.");
			Instance.m_unsavedDict.Add(obj.m_netID, GlobalObjectId.GetGlobalObjectIdSlow(obj));
			SaveDict();
			return obj.m_netID;
		}

		// Find a free ID
		for (int id = int.MaxValue; id > k_minSpawnedObjectIDCount; --id)
		{
			// Check if there is an entry here
			if (Instance.m_unsavedDict.ContainsKey(id))
			{
				continue;
			}

			Instance.m_unsavedDict.Add(id, GlobalObjectId.GetGlobalObjectIdSlow(obj));

			SaveDict();

			return id;
		}

		Debug.LogError("Out of IDs for scene objects. This is really fucked up.");
		return 0;
	}
#endif

#if UNITY_EDITOR
	public static bool HasSceneGameObject(NetworkObject obj)
	{
		if (s_instance == null) return true;
		if (Instance.m_disableIdCreation) return true;

		//Debug.Log($"Looking for {obj.m_netID}");
		if (Instance.m_unsavedDict.TryGetValue(obj.m_netID, out GlobalObjectId globalID))
		{
			// Check if this is really the same object
			GlobalObjectId realID = GlobalObjectId.GetGlobalObjectIdSlow(obj);

			//Debug.Log($"Checking if {globalID} == {realID}");
			if (globalID.Equals(realID)) return true;
		}

		return false;
	}
#endif

#if UNITY_EDITOR
	[MenuItem("NetCode/Remove unused IDs")]
	// todo: re-assign ids to be sequential
	public static void CleanupIDs()
	{
		if (Instance == null)
		{
			Debug.LogWarning("No instance");
			Load();
		}

		Debug.Log("Cleaning IDs...");

		Instance.m_disableIdCreation = true;

		try
		{
			HashSet<string> scenes = new();
			HashSet<string> startingScenes = new();

			// Add all open scenes to set
			for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
			{
				scenes.Add(EditorSceneManager.GetSceneAt(i).path);
				startingScenes.Add(EditorSceneManager.GetSceneAt(i).path);
			}

			List<int> removedKeys = new();
			List<int> notRemovedKeys = new();

			// Check if each object exists
			foreach (var kv in Instance.m_unsavedDict)
			{
				if (kv.Value.identifierType != 2)
				{
					Debug.LogWarning($"{GlobalObjectId.GlobalObjectIdentifierToObjectSlow(kv.Value)} is not a scene object. Removing.");
					removedKeys.Add(kv.Key);
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
					Debug.LogWarning($"Removing object from scene: {scenePath}. It no longer exists.");
					removedKeys.Add(kv.Key);
					continue;
				}

				// Check for duplicates
				bool duplicate = false;
				foreach (var key2 in notRemovedKeys)
				{
					if (kv.Key != key2)
					{
						if (kv.Value.Equals(Instance.m_unsavedDict[key2]))
						{
							Debug.LogWarning($"Duplicate detected in scene: {scenePath}. Object = {obj}");
							removedKeys.Add(kv.Key);
							duplicate = true;
							break;
						}
					}
				}

				if (duplicate) continue;

				notRemovedKeys.Add(kv.Key);
				Debug.Log($"{kv.Key} : {obj} is okay.");
			}

			foreach (var key in removedKeys)
			{
				Instance.m_unsavedDict.Remove(key);
			}

			// Set all IDs to dict
			foreach (var kv in Instance.m_unsavedDict)
			{
				NetworkObject obj = (NetworkObject)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(kv.Value);
				obj.m_netID = kv.Key;
				Debug.Log($"Setting {obj} ID to {kv.Key}");
				EditorUtility.SetDirty(obj);
			}

			SaveDict();

			AssetDatabase.SaveAssets();
			EditorSceneManager.MarkAllScenesDirty();
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

			// Unload all scenes except original
			foreach (string scenePath in scenes)
			{
				if (!startingScenes.Contains(scenePath))
					EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(scenePath), true);
			}
		}
		finally
		{
			Instance.m_disableIdCreation = false;
		}

		return;
	}
#endif
}