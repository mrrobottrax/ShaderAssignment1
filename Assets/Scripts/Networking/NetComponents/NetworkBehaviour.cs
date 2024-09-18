using Steamworks;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public abstract class NetworkBehaviour : MonoBehaviour
{
	internal int m_index;

	internal FieldInfo[] m_netVarFields;
	internal MethodInfo[] m_netVarCallbacks;

	internal byte[] m_netVarBuffer;

	public bool IsOwner { get; internal set; } = true;


	internal byte[] GetNetVarBytes()
	{
		// Allocate buffer
		byte[] buffer = new byte[m_netVarBuffer.Length];

		// Store all NetVar data in a buffer
		int offset = 0;
		foreach (var field in m_netVarFields)
		{
			object value = field.GetValue(this);

			if (!field.FieldType.IsArray)
			{
				var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
				try
				{
					Marshal.Copy(handle.AddrOfPinnedObject(), buffer, offset, Marshal.SizeOf(field.FieldType));
				}
				finally
				{
					handle.Free();
				}

				offset += Marshal.SizeOf(field.FieldType);
			}
			else
			{
				Array arr = (Array)value;

				Array.Copy(arr, 0, buffer, offset, arr.Length);
				offset += arr.Length;
			}
		}

		return buffer;
	}

	internal static void ProcessUpdateMessage(SteamNetworkingMessage_t message)
	{
		// Read message from buffer
		NetworkBehaviourUpdateMessage behaviourUpdate = Marshal.PtrToStructure<NetworkBehaviourUpdateMessage>(message.m_pData + 1);

		// Copy new data into fields
		NetworkObject obj = NetworkObjectManager.GetNetworkObject(behaviourUpdate.m_networkID);
		NetworkBehaviour comp = obj.m_networkBehaviours[behaviourUpdate.m_componentIndex];

		if (!obj)
		{
			Debug.LogWarning($"Network object {behaviourUpdate.m_networkID} doesn't exist yet");
			return;
		}

		// Read new data from buffer
		int cbStart = 1 + Marshal.SizeOf<NetworkBehaviourUpdateMessage>();
		int cbBuffer = message.m_cbSize - cbStart;

		byte[] buffer = new byte[cbBuffer];
		Marshal.Copy(message.m_pData + cbStart, buffer, 0, cbBuffer);

		comp.m_netVarBuffer = buffer;

		var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			IntPtr pBuffer = handle.AddrOfPinnedObject();

			// Copy into each field
			int offset = 0;
			int index = 0;
			foreach (var field in comp.m_netVarFields)
			{
				IntPtr pFieldData = pBuffer + offset;

				if (!field.FieldType.IsArray)
				{
					object value = Marshal.PtrToStructure(pFieldData, field.FieldType);

					if (!field.GetValue(comp).Equals(value))
					{
						field.SetValue(comp, value);

						// Run value change function if set
						comp.m_netVarCallbacks[index]?.Invoke(comp, null);
					}

					offset += Marshal.SizeOf(field.FieldType);
				}
				else
				{
					byte[] arr = (byte[])field.GetValue(comp);
					Marshal.Copy(pFieldData, arr, 0, arr.Length);

					// Run value change function if set
					comp.m_netVarCallbacks[index]?.Invoke(comp, null);

					offset += arr.Length * Marshal.SizeOf(field.FieldType.GetElementType());
				}

				++index;
			}
		}
		finally
		{
			handle.Free();
		}
	}

	internal static byte[] CreateMessageBuffer(int networkID, int componentIndex, byte[] data)
	{
		NetworkBehaviourUpdateMessage message = new()
		{
			m_networkID = networkID,
			m_componentIndex = componentIndex,
		};

		byte[] buffer = new byte[Marshal.SizeOf<NetworkBehaviourUpdateMessage>() + data.Length];

		// Copy message
		var hMessage = GCHandle.Alloc(message, GCHandleType.Pinned);
		try
		{
			var pMessage = hMessage.AddrOfPinnedObject();

			Marshal.Copy(pMessage, buffer, 0, Marshal.SizeOf<NetworkBehaviourUpdateMessage>());
		}
		finally
		{
			hMessage.Free();
		}

		// Copy NetVar buffer
		Array.Copy(data, 0, buffer, Marshal.SizeOf<NetworkBehaviourUpdateMessage>(), data.Length);

		return buffer;
	}

	private void OnValidate()
	{
		// Check parents for another NetworkBehaviour
		Transform parent = transform;
		while (parent != null)
		{
			if (parent.TryGetComponent<NetworkObject>(out _))
			{
				break;
			}

			parent = parent.parent;
		}

		if (parent == null)
			Debug.LogWarning("NetworkBehaviour detected on a GameObject without a NetworkObject as it's self or parent, tread lightly!");
	}
}