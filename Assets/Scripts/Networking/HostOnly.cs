using UnityEngine;

public class HostOnly : MonoBehaviour
{
	private void Awake()
	{
		if (NetworkManager.Mode != ENetworkMode.Host)
		{
			gameObject.SetActive(false);
		}
	}
}
