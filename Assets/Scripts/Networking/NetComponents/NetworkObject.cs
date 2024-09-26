﻿using Steamworks;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
	[SerializeField, HideInInspector]
	internal int m_prefabIndex = -1; // Only used by spawned prefabs

	[SerializeField, HideInInspector]
	internal int m_netID = 0;

	internal SteamNetworkingIdentity m_ownerIndentity;

	internal NetworkBehaviour[] m_networkBehaviours;

	private void Awake()
	{
		TickManager.OnLateTick += LateTick;

		// Non-prefabs are always owned by server
		if (m_prefabIndex == -1)
		{
			m_ownerIndentity = NetworkManager.GetServerIdentity();
		}

		InitNetworkBehaviours();
	}

	private void Start()
	{
		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			ForceRegister();
		}
		else if (m_netID == 0)
		{
			// Reserve net ID
			m_netID = NetworkObjectManager.ReserveID(this);

			// Add to list
			NetworkObjectManager.AddNetworkObjectToList(this);

			Debug.Log(m_netID);
		}
	}

	private void OnDestroy()
	{
		TickManager.OnLateTick -= LateTick;

		// Notify clients of destruction
		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			SendFunctions.SendDestroyGameObject(m_netID);
		}

		// Remove from list
		NetworkObjectManager.RemoveNetworkObjectFromList(this);
	}

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

		if (gameObject.scene.name != null && !NetworkData.HasSceneGameObject(this))
		{
			m_netID = NetworkData.AddSceneObject(this);
			Debug.Log($"Generating ID for \"{gameObject.name}\". ID: {m_netID}");
			EditorUtility.SetDirty(gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
		}
	}

	void LateTick()
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

	void InitNetworkBehaviours()
	{
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