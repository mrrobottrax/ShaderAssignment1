using UnityEngine;

public static class InputManager
{
	public static Controls Instance { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Init()
	{
		Instance = new Controls();
		Instance.Enable();
	}

	public static void SetControlMode(ControlType controlType)
	{
		Instance.UI.Disable();
		Instance.Player.Disable();

		switch (controlType)
		{
			case ControlType.Player:
				Instance.Player.Enable();
				break;


			case ControlType.UI:
				Instance.UI.Enable();
				break;


			case ControlType.Disabled:
				break;
		}
	}

	public enum ControlType
	{
		Player,
		UI,
		Disabled
	}
}
