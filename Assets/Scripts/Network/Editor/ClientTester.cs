using UnityEngine;

// Allow testing local clients

[DisallowMultipleComponent]
public class ClientTester : MonoBehaviour
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void OnRuntimeStart()
	{
		// Autocreate tester when in editor
		if (Application.isEditor)
		{
			new GameObject("ClientTester").AddComponent<ClientTester>();
		}
	}

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
}
