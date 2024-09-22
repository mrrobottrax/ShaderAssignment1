// Start hosting when loading into a scene in the editor
internal static class EditorAutoHost
{
	public static void Init()
	{
#if UNITY_EDITOR
		NetworkManager.StartHosting();
#endif
	}
}
