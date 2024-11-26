using UnityEngine;

public class HostOnly : MonoBehaviour
{
	private void Awake()
	{
		if (NetworkManager.Mode == ENetworkMode.Client)
		{
			gameObject.SetActive(false);
		}
	}
}
