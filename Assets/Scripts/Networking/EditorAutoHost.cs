// Start hosting when loading into a scene in the editor
using UnityEngine.SceneManagement;

internal static class EditorAutoHost
{
	public static void Init()
	{
#if UNITY_EDITOR
		if (SceneManager.GetActiveScene().buildIndex != 0)
			NetworkManager.StartHosting();
#endif
	}
}
