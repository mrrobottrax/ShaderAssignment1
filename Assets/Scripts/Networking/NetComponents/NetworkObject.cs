using Steamworks;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
	[SerializeField]
	internal int m_prefabIndex = -1; // Only used by spawned prefabs

	[SerializeField]
	internal int m_netID = 0;

	internal SteamNetworkingIdentity m_ownerIndentity;

	internal NetworkBehaviour[] m_networkBehaviours;

	bool m_initialized = false;

	private void Awake()
	{
		TickManager.OnSend += Send;

		// Non-prefabs are always owned by server
		if (m_prefabIndex == -1)
		{
			m_ownerIndentity = NetworkManager.GetServerIdentity();
		}

		// Add to list if a scene object
		if (m_netID != 0)
		{
			NetworkObjectManager.AddNetworkObjectToList(this);
		}
	}

	private void Start()
	{
		InitNetworkBehaviours();

		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			ForceRegister();
		}
	}

	private void OnDestroy()
	{
		TickManager.OnSend -= Send;

		// Notify clients of destruction
		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			SendFunctions.SendDestroyGameObject(m_netID);
		}

		// Remove from list
		NetworkObjectManager.RemoveNetworkObjectFromList(this);
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

	void Send()
	{
		if (NetworkManager.LocalIdentity.Equals(m_ownerIndentity))
		{
			// Scan NetworkBehaviours for changes
			foreach (var net in m_networkBehaviours)
			{
				byte[] newBytes = net.GetNetVarBytes();

				if (!Enumerable.SequenceEqual(net.m_netVarBuffer, newBytes))
				{
					// Change detected
					net.m_netVarBuffer = newBytes;

					SendFunctions.SendNetworkBehaviourUpdate(m_netID, net.m_index, newBytes);
				}
			}
		}
	}

	// Get NetID and add to lists and all that
	public void ForceRegister()
	{
		InitNetworkBehaviours();

		if (m_netID == 0)
		{
			// Reserve net ID
			m_netID = NetworkObjectManager.ReserveID(this);

			// Add to list
			NetworkObjectManager.AddNetworkObjectToList(this);

			// Notify peers of object creation
			SendFunctions.SendSpawnPrefab(m_netID, m_prefabIndex, NetworkManager.m_localIdentity);
			SendFunctions.SendObjectSnapshot(this);
		}
	}

	internal void InitNetworkBehaviours()
	{
		if (m_initialized) return;
		m_initialized = true;

		m_networkBehaviours = gameObject.GetComponentsInChildren<NetworkBehaviour>();

		for (int i = 0; i < m_networkBehaviours.Length; ++i)
		{
			NetworkBehaviour net = m_networkBehaviours[i];

			net.m_index = i;

			// Get all NetVars
			Type type = net.GetType();

			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

			net.m_netVarFields = type.GetFields(flags)
				.Where(field => field.GetCustomAttribute(typeof(NetVarAttribute)) != null).ToArray();

			net.m_netVarCallbacks = new MethodInfo[net.m_netVarFields.Length];

			// Iterate NetVars
			int size = 0;
			for (int index = 0; index < net.m_netVarFields.Length; ++index)
			{
				FieldInfo field = net.m_netVarFields[index];

				if (field.FieldType.IsByRef)
				{
					throw new InvalidOperationException($"NetVars cannot be reference types");
				}

				if (field.FieldType.IsArray)
				{
					// Get size of array
					Array array = (Array)field.GetValue(net);
					Type elementType = field.FieldType.GetElementType();

					size += array.Length * Marshal.SizeOf(elementType);
				}
				else if (field.FieldType.IsEnum)
				{
					Type underlyingType = Enum.GetUnderlyingType(field.FieldType);
					size += Marshal.SizeOf(underlyingType);
				}
				else
				{
					// Get size of NetVar
					size += Marshal.SizeOf(field.FieldType);
				}

				// Set callbacks
				string callbackName = field.GetCustomAttribute<NetVarAttribute>().m_callback;
				if (callbackName != null)
				{
					MethodInfo info = type.GetMethod(callbackName, flags);
					net.m_netVarCallbacks[index] = info;
				}
				else
				{
					net.m_netVarCallbacks[index] = null;
				}
			}

			// Apply to array
			net.m_netVarBuffer = new byte[size];
			net.m_netVarBuffer = net.GetNetVarBytes();
		}
	}
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