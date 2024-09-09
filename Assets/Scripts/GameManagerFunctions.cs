using UnityEngine;

public class GameManagerFunctions : ScriptableObject
{
	public void StartHosting()
	{
		GameManager.StartHosting();
	}

	public void GoToTestLevel()
	{
		GameManager.GoToTestLevel();
	}
}