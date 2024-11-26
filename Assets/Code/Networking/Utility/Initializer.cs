// Init stuff in a specific order

using UnityEngine;

internal static class Initializer
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Init()
	{
		SteamManager.Init();
		NetworkManager.Init();

		EditorAutoHost.Init();
		// The rest is unimportant and can start itself up
	}
}
