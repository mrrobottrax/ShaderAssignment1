using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkRigidbody : NetworkBehaviour
{
	Rigidbody m_rigidbody;

	private void Awake()
	{
		m_rigidbody = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		if (IsOwner)
		{
			m_rigidbody.isKinematic = false;
		}
		else
		{
			m_rigidbody.isKinematic = true;
		}
	}
}
