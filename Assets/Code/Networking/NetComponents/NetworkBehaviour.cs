using System.Collections.Generic;
using UnityEngine;

public abstract class NetworkBehaviour : MonoBehaviour
{
	internal int m_index;
	public NetworkObject NetObj {get; internal set;}

	public bool IsOwner { get; internal set; } = true;

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
			Debug.LogWarning($"NetworkBehaviour detected on a GameObject {this.gameObject.name} without a NetworkObject as it's self or parent, tread lightly!", this);
	}

	public void SendUpdate<T>(T message, Peer target) where T : Message
	{
		NetworkManager.SendMessage(message, target);
	}

	public void BroadcastUpdate<T>(T message) where T : Message
	{
		NetworkManager.BroadcastMessage(message);
	}

	public virtual void AddSnapshotMessages(List<Message> messages) { }
}