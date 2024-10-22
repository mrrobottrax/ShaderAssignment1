﻿using Steamworks;
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
	internal int m_netID = 0;

	internal SteamNetworkingIdentity m_ownerIndentity;
	internal NetworkBehaviour[] m_networkBehaviours;

	public bool IsOwner => NetworkManager.LocalIdentity.Equals(m_ownerIndentity);
	public bool IsFromScene => m_isFromScene;

	bool m_initialized = false;
	bool m_isFromScene = false;

	void Awake()
	{
		// Scene objects are always server owned
		if (m_netID != 0)
		{
			m_isFromScene = true;

			m_ownerIndentity = NetworkManager.GetServerIdentity();
			Init(); // Init scene objects early
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

		m_networkBehaviours = gameObject.GetComponentsInChildren<NetworkBehaviour>();

		for (int i = 0; i < m_networkBehaviours.Length; ++i)
		{
			NetworkBehaviour net = m_networkBehaviours[i];
			net.m_object = this;
			net.m_index = i;
			net.IsOwner = IsOwner;
		}

		// Get ID. This will be called when the host spawns an object through Instantiate.

		if (m_netID == 0 && NetworkManager.Mode == ENetworkMode.Host)
		{
			// Reserve net ID
			m_netID = NetworkObjectManager.ReserveID(this);

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
		List<MessageBase> snapshotMessages = new();

		foreach (var net in m_networkBehaviours)
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
		List<MessageBase> snapshotMessages = new();

		foreach (var net in m_networkBehaviours)
		{
			net.AddSnapshotMessages(snapshotMessages);
		}

		foreach (var message in snapshotMessages)
		{
			NetworkManager.BroadcastMessage(message);
		}
	}

#if UNITY_EDITOR
	private void OnValidate()
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
			return;
		}

		// Run when in a scene and hasn't run yet
		if (gameObject.scene.name != null && !NetworkData.HasSceneGameObject(this))
		{
			m_netID = NetworkData.AddSceneObject(this);
			Debug.Log($"Generating ID for \"{gameObject.name}\". ID: {m_netID}");
			EditorUtility.SetDirty(gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
		}
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
		m_netID = serializedObject.FindProperty("m_netID");
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