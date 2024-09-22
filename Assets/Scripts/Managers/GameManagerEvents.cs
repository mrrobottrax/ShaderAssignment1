using UnityEngine;

public class GameManagerEvents : ScriptableObject
{
	public void StartHosting()
	{
		GameManager.StartHosting();
	}

	public void StartPropHunt()
	{
		GameManager.GoToTestLevel();
	}
}