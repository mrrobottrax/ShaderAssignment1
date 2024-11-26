using Steamworks;
using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class NetworkObject : MonoBehaviour
{
	[SerializeField]
	internal int m_prefabIndex = -1; // Only used by spawned prefabs

	[SerializeField]
	public int NetID = 0;

	internal SteamNetworkingIdentity m_ownerIndentity;
	public NetworkBehaviour[] NetworkBehaviours {get; private set;}

	public bool IsOwner => NetworkManager.LocalIdentity.Equals(m_ownerIndentity);
	public bool IsFromScene => m_isFromScene;

	bool m_initialized = false;
	bool m_isFromScene = false;

	void Awake()
	{
		// Scene objects are always server owned
		if (NetID != 0)
		{
			m_isFromScene = true;

			m_ownerIndentity = NetworkManager.GetServerIdentity();
			Init(); // Init scene objects early
		}
		// Non-player prefabs are also server owned
		else if (m_prefabIndex >= 0)
		{
			m_ownerIndentity = NetworkManager.GetServerIdentity();
			Init();
		}
	}

	void Start()
	{
		Init();
	}

	internal void Init()
	{
		if (m_initialized) return;
		m_initialized = true;

		// Setup networkbehaviours

		NetworkBehaviours = gameObject.GetComponentsInChildren<NetworkBehaviour>();

		for (int i = 0; i < NetworkBehaviours.Length; ++i)
		{
			NetworkBehaviour net = NetworkBehaviours[i];
			net.NetObj = this;
			net.m_index = i;
			net.IsOwner = IsOwner;
		}

		// Get ID. This will be called when the host spawns an object through Instantiate.

		if (NetID == 0 && NetworkManager.Mode == ENetworkMode.Host)
		{
			// Reserve net ID
			NetID = NetworkObjectManager.ReserveID(this);

			// Notify peers of object creation
			NetworkManager.BroadcastMessage(new SpawnPrefabMessage(this));
			BroadcastSnapshot();
		}

		NetworkObjectManager.AddNetworkObjectToList(this);
	}

	private void OnDestroy()
	{
		// Notify clients of destruction
		if (IsOwner)
		{
			NetworkManager.BroadcastMessage(new DestroyObjectMessage(this));
		}

		// Remove from list
		NetworkObjectManager.RemoveNetworkObjectFromList(this);
	}

	public void SendSnapshot(Peer peer)
	{
		List<Message> snapshotMessages = new();

		foreach (var net in NetworkBehaviours)
		{
			net.AddSnapshotMessages(snapshotMessages);
		}

		foreach (var message in snapshotMessages)
		{
			NetworkManager.SendMessage(message, peer);
		}
	}

	public void BroadcastSnapshot()
	{
		List<Message> snapshotMessages = new();

		foreach (var net in NetworkBehaviours)
		{
			net.AddSnapshotMessages(snapshotMessages);
		}

		foreach (var message in snapshotMessages)
		{
			NetworkManager.BroadcastMessage(message);
		}
	}

#if UNITY_EDITOR
	private void OnValidate_Internal()
	{
		// Don't run in play mode
		if (EditorApplication.isPlaying)
		{
			return;
		}

		// Don't run when switching to play mode
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
			return;
		}

		// Don't run when editing a prefab
		if (PrefabStageUtility.GetCurrentPrefabStage() != null)
		{
			NetID = 0;
			return;
		}

		if (this == null) return;

		// Don't override network ID
		if (PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.Regular)
		{
			var prefabObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);

			var objs = prefabObject.GetComponentsInChildren<NetworkObject>();

			foreach (var obj in objs)
			{
				obj.NetID = 0;
			}
		}

		// Run when in a scene and hasn't run yet
		if (gameObject.scene.name != null && !NetworkData.HasSceneGameObject(this))
		{
			NetID = NetworkData.AddSceneObject(this);
			Debug.Log($"Generating ID for \"{gameObject.name}\". ID: {NetID}");
			EditorUtility.SetDirty(gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
		}
	}

	private void OnValidate()
	{
		EditorApplication.delayCall += OnValidate_Internal;
	}
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(NetworkObject))]
internal class NetworkObjectEditor : Editor
{
	SerializedProperty m_prefabIndex;
	SerializedProperty m_netID;

	private void OnEnable()
	{
		m_prefabIndex = serializedObject.FindProperty("m_prefabIndex");
		m_netID = serializedObject.FindProperty("NetID");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUI.BeginDisabledGroup(true);
		EditorGUILayout.IntField("Prefab Index", m_prefabIndex.intValue);
		EditorGUILayout.IntField("Net ID", m_netID.intValue);
		EditorGUI.EndDisabledGroup();

		//serializedObject.ApplyModifiedProperties();
	}
}
#endif