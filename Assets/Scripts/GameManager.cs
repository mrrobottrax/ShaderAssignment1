﻿using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EGameState
{
	Menu = 0,
	OnBreak, // In the lobby, selling stuff, etc, players can join
	InGame, // Players can join but will be put in spectator mode
}

public class GameManager : MonoBehaviour
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void OnRuntimeStart()
	{
		new GameObject("Game Manager").AddComponent<GameManager>();
	}

	private static EGameState m_gameState;
	public static EGameState GameState { get { return m_gameState; } }

	public static Action onGameStateChange;

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	private static void ChangeGameState(EGameState state)
	{
		m_gameState = state;
		onGameStateChange?.Invoke();

		if (state == EGameState.OnBreak)
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
		SceneManager.LoadScene(1);
		ChangeGameState(EGameState.OnBreak);
	}

	public static void GoToTestLevel()
	{
		ChangeGameState(EGameState.InGame);
		SceneManager.LoadScene(2);
	}
}