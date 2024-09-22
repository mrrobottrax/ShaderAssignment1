using UnityEngine;

// Allows calling these events from buttons and such without
// needing to add a script to the world
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