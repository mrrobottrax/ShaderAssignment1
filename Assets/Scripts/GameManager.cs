using System;
using UnityEngine.SceneManagement;

public enum EGameState
{
	Menu = 0,
	InLobby, // In the lobby, selling stuff, etc, players can join
	InGame, // Players can join but will be put in spectator mode
}

public static class GameManager
{
	private static EGameState m_gameState;
	public static EGameState GameState { get { return m_gameState; } }


	public static Action onGameStateChange;

	private static void ChangeGameState(EGameState state)
	{
		m_gameState = state;
		onGameStateChange?.Invoke();

		if (state == EGameState.InLobby)
		{
			NetworkManager.SetGameJoinable(true);
		}
		else
		{
			NetworkManager.SetGameJoinable(false);
		}
	}

	public static void StartHosting()
	{
		NetworkManager.StartHosting();
		GoToLobby();
	}

	public static void GoToLobby()
	{
		SceneManager.LoadScene(1);
		ChangeGameState(EGameState.InLobby);
	}

	public static void GoToTestLevel()
	{
		ChangeGameState(EGameState.InGame);
		SceneManager.LoadScene("Test Level");
	}
}