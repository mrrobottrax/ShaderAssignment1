using UnityEngine;

public static class InputManager
{
	public static Controls Controls { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Init()
	{
		Controls = new Controls();
		Controls.Enable();
	}

	public static void SetControlMode(ControlType controlType)
	{
		Controls.UI.Disable();
		Controls.Player.Disable();

		switch (controlType)
		{
			case ControlType.Player:
				Controls.Player.Enable();
				break;


			case ControlType.UI:
				Controls.UI.Enable();
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
