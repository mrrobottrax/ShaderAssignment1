using UnityEngine.SceneManagement;

// Start hosting when loading into a scene in the editor
// Called automatically by Initializer.cs
internal static class EditorAutoHost
{
	public static void Init()
	{
//#if UNITY_EDITOR
//		if (SceneManager.GetActiveScene().buildIndex != 0)
		{
			NetworkManager.StartHosting(true);
			NetworkManager.SetGameJoinable(true);
		}
//#endif
	}
}
