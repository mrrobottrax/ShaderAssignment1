using UnityEngine;

public class GameManagerEvents : ScriptableObject
{
	public void StartHosting()
	{
		PropHuntGameManager.StartHosting();
	}

	public void StartPropHunt()
	{
		PropHuntGameManager.StartGame();
	}
}