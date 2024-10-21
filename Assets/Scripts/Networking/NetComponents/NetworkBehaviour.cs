using UnityEngine;

public abstract class NetworkBehaviour : MonoBehaviour
{
	internal int m_index;

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
			Debug.LogWarning($"NetworkBehaviour detected on a GameObject without a NetworkObject as it's self or parent, add it!", this);
	}

	public virtual void SendSnapshot(Peer peer) { }
}