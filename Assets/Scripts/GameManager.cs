using System;
using UnityEngine;
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
		SceneManager.LoadSceneAsync(1);
		ChangeGameState(EGameState.InLobby);
	}

	public static void GoToTestLevel()
	{
		ChangeGameState(EGameState.InGame);
		SceneManager.LoadSceneAsync("Test Level");
		Debug.Log("SCENE CHANGE");
	}
}