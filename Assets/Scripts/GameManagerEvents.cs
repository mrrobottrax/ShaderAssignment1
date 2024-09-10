using UnityEngine;

public class GameManagerEvents : ScriptableObject
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