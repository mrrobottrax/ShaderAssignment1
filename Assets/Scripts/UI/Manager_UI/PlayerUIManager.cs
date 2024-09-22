using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{

    [field: SerializeField] public InteractionDisplay InteractionPromptDisplay { get; private set; }
    [field: SerializeField] public PlayerHUDManager HUDManager { get; private set; }
    [field: SerializeField] public InventoryUI InventoryUI { get; private set; }
    [field: SerializeField] public FavouriteWheelDisplay FavouritesWheel { get; private set; }
    [field: SerializeField] public LegibleObjectDisplay LegibleObjectDisplay { get; private set; }
    [field: SerializeField] public DialogueMenuDisplay DialogueMenuDisplay { get; private set; }

    [Header("System")]
    private MenuDisplayBase activeDisplay;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    /// <summary>
    /// This method changes from the active display to a different display
    /// </summary>
    public void SetActiveDisplay(MenuDisplayBase newDisplay)
    {
        // Disable previous Diplay
        activeDisplay?.SetDisplayActive(false);

        // Set new display and execute its activation logic
        activeDisplay = newDisplay;
        activeDisplay.SetDisplayActive(true);

        InputManager.SetControlMode(InputManager.ControlType.UI);

        // Disable camera & player movement
        playerHealth.GetPlayerCamera().EnableFirstPersonCamera(false);
        playerHealth.GetPlayerController().SetControlsSubscription(false);

        // Disable interaction system
        PlayerInteraction interaction = playerHealth.GetComponent<PlayerInteraction>();
        interaction.enabled = false;
        interaction.ClearCurrentInteractable();
    }

    /// <summary>
    /// This method resets, disables, then clears the active UI Display
    /// </summary>
    public void DisableActiveDisplay()
    {
        // Execute the disable logic then clear
        activeDisplay?.SetDisplayActive(false);
        activeDisplay = null;

        InputManager.SetControlMode(InputManager.ControlType.Player);

        // Enable camera & player movement
        playerHealth.GetPlayerCamera().EnableFirstPersonCamera(true);
        playerHealth.GetPlayerController().SetControlsSubscription(true);

        // Enable interaction system
        PlayerInteraction interaction = playerHealth.GetComponent<PlayerInteraction>();
        interaction.enabled = true;
    }

    /// <summary>
    /// This method returns the active display
    /// </summary>
    public MenuDisplayBase GetActiveDisplay()
    {
        return activeDisplay;
    }
}
