using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
	[SerializeField, HideInInspector]
	internal int m_prefabIndex = -1; // Only used by spawned prefabs

	internal int m_netID = 0;
	internal int m_ownerID = 0; // 0 = server owned

	internal NetworkBehaviour[] m_networkBehaviours;

	private void Awake()
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
			int index = 0;
			foreach (var field in net.m_netVarFields)
			{
				if (field.FieldType.IsByRef)
				{
					throw new InvalidOperationException($"NetVars cannot be reference types");
				}

				// Get size of all NetVars
				size += Marshal.SizeOf(field.FieldType);

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

				++index;
			}

			// Apply to array
			net.m_netVarBuffer = new byte[size];
			net.m_netVarBuffer = net.GetNetVarBytes();
		}
	}

	private void Start()
	{
		ForceRegister();
	}

	// Get NetID and add to lists and all that
	public void ForceRegister()
	{
		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			if (m_netID == 0)
			{
				// Reserve net ID
				m_netID = NetworkObjectManager.ReserveID(this);

				// Notify clients of object creation
				foreach (var client in Host.GetClients())
				{
					client.SendSpawnPrefab(m_netID, m_prefabIndex, m_ownerID);
					SendAllNetworkBehaviourData(client);
				}

				// Add to list
				NetworkObjectManager.AddNetworkObjectToList(this);
			}
		}
	}

	internal void SendAllNetworkBehaviourData(RemoteClient client)
	{
		foreach (var net in m_networkBehaviours)
		{
			client.SendNetworkBehaviourUpdate(m_netID, net.m_index, net.GetNetVarBytes());
		}
	}

	private void OnDestroy()
	{
		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			// Notify clients of object destruction
			foreach (var client in Host.GetClients())
			{
				client.SendDestroyGameObject(m_netID);
			}
		}

		// Remove from list
		NetworkObjectManager.RemoveNetworkObjectFromList(this);
	}

	public int GetNetID()
	{
		return m_netID;
	}

	private void Update()
	{
		if (TickManager.ShouldTick())
			Tick();
	}

	void Tick()
	{
		if (NetworkManager.GetPlayerNetID() == m_ownerID)
		{
			// Scan NetworkBehaviours for changes
			foreach (var net in m_networkBehaviours)
			{
				byte[] newBytes = net.GetNetVarBytes();

				if (!Enumerable.SequenceEqual(net.m_netVarBuffer, newBytes))
				{
					// Change detected
					net.m_netVarBuffer = newBytes;

					// Broadcast new NetVars
					if (NetworkManager.Mode == ENetworkMode.Host)
					{
						// Send to all clients
						foreach (var client in Host.GetClients())
						{
							client.SendNetworkBehaviourUpdate(m_netID, net.m_index, newBytes);
						}
					}
					else
					{
						// Send to host
						byte[] buffer = NetworkBehaviour.CreateMessageBuffer(m_netID, net.m_index, newBytes);

						// Pin the buffer in memory
						GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
						try
						{
							// Get the pointer to the buffer
							IntPtr pMessage = handle.AddrOfPinnedObject();

							// Send the message
							NetworkManager.SendMessage(
								ESnapshotMessageType.NetworkBehaviourUpdate,
								pMessage, buffer.Length,
								ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable,
								LocalClient.m_hConn
							); ;
						}
						finally
						{
							// Free the pinned object
							handle.Free();
						}
					}
				}
			}
		}
	}
}