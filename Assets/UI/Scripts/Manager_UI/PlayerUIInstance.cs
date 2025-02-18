using UnityEngine;

public class PlayerUIInstance : MonoBehaviour
{
	[SerializeField] InteractionUIManager interactionPromptDisplay;
	[SerializeField] PlayerHUD hudManager;
	[SerializeField] InventoryUI inventoryUI;

	public static InteractionUIManager InteractionPromptDisplay { get { return instance.interactionPromptDisplay; } }
	public static PlayerHUD HUDManager { get { return instance.hudManager; } }
	public static InventoryUI InventoryUI { get { return instance.inventoryUI; } }

	// System
	private MenuDisplayBase activeDisplay;
	private PlayerStats playerHealth;
	private static PlayerUIInstance instance;

	private void Awake()
	{
		playerHealth = GetComponentInParent<PlayerStats>();

		if (!instance)
		{
			instance = this;
		}
		else
		{
			Debug.LogWarning("Multiple UI managers in scene");
		}

	}

	/// <summary>
	/// This method changes from the active display to a different display
	/// </summary>
	public static void SetActiveDisplay(MenuDisplayBase newDisplay)
	{
		// Disable previous Diplay
		if (instance.activeDisplay) instance.activeDisplay.SetDisplayActive(false);

		// Set new display and execute its activation logic
		instance.activeDisplay = newDisplay;
		instance.activeDisplay.SetDisplayActive(true);

		InputManager.SetControlMode(InputManager.ControlType.UI);
	}

	/// <summary>
	/// This method resets, disables, then clears the active UI Display
	/// </summary>
	public static void DisableActiveDisplay()
	{
		// Execute the disable logic then clear
		if (instance.activeDisplay) instance.activeDisplay.SetDisplayActive(false);
		instance.activeDisplay = null;

		InputManager.SetControlMode(InputManager.ControlType.Player);

		// Enable camera & player movement
		instance.playerHealth.FirstPersonCamera.EnableFirstPersonCamera(true);
		instance.playerHealth.PlayerController.SetControlsSubscription(true);

		// Enable interaction system
		PlayerInteraction interaction = instance.playerHealth.GetComponent<PlayerInteraction>();
		interaction.enabled = true;
	}

	/// <summary>
	/// This method returns the active display
	/// </summary>
	public static MenuDisplayBase GetActiveDisplay()
	{
		return instance.activeDisplay;
	}
}
