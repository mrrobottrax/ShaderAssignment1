using UnityEngine;

[DisallowMultipleComponent]
public class Client : MonoBehaviour
{
	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
}
