using Steamworks;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class Player : MonoBehaviour
{
	[SerializeField] float moveSpeed = 2;

	[SerializeField] GameObject[] firstPersonObjects;
	[SerializeField] GameObject[] thirdPersonObjects;

	NetworkObject m_netObj;

	bool m_isOwner;

	private void Awake()
	{
		m_netObj = GetComponent<NetworkObject>();
	}

	private void Start()
	{
		m_isOwner = NetworkManager.GetPlayerNetID() == m_netObj.GetNetID();

		if (m_isOwner)
		{
			SetLocalOnlyStuffEnabled(true);
		}
		else
		{
			SetLocalOnlyStuffEnabled(false);
		}
	}

	private void SetLocalOnlyStuffEnabled(bool enabled)
	{
		foreach (var obj in firstPersonObjects)
		{
			obj.SetActive(enabled);
		}

		foreach (var obj in thirdPersonObjects)
		{
			obj.SetActive(!enabled);
		}
	}

	private void Update()
	{
		if (!m_isOwner)
		{
			return;
		}

		Vector3 movement = new()
		{
			x = Input.GetAxisRaw("Horizontal"),
			z = Input.GetAxisRaw("Vertical")
		};

		transform.position += moveSpeed * Time.deltaTime * movement.normalized;
	}
}