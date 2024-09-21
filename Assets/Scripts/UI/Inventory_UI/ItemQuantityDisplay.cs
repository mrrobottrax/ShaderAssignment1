using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemQuantityDisplay : MenuDisplayBase
{
    [Header("Components")]
    [SerializeField] private PlayerUIManager uIManager;
    private PlayerInventoryComponent playerInventoryComponent;

    [Header("UI Elements")]
    [SerializeField] private Slider _amountSlider;
    [SerializeField] private TMP_InputField _amountFeild;

    [Header("Primary Button")]
    [SerializeField] private Button _primaryButton;
    [SerializeField] private TextMeshProUGUI _primaryText;

    private string trashItemText = "Trash";
    private string moveItemText = "Move";
    private string sellItemText = "Sell";

    [Header("System")]
    private EQuantityInteractionType currentDisplayType;
    public enum EQuantityInteractionType
    {
        Trash,
        Move,
        Transaction
    }

    private InventorySlotDisplay currentSlotDisplay;
    private InventoryDisplay currentInventoryDisplay;

    private int currentAmountSelected;

    #region Input Methods
    public override void SetControlsSubscription(bool isSubscribing)
    {

    }

    public override void Subscribe()
    {

    }

    public override void Unsubscribe()
    {

    }
    #endregion

    #region Menu Methods

    protected override void OnEnableDisplay()
    {
        base.OnEnableDisplay();
    }

    protected override void OnDisableDisplay()
    {
        base.OnDisableDisplay();
    }
    #endregion

    /// <summary>
    /// This method should be called when an item quantity needs to be selected from a slot by the player
    /// This method will determine which buttons will be used for the interaction.
    /// </summary>
    public void SetItem(InventorySlotDisplay slotDisplay, InventoryDisplay inventoryDisplay, EQuantityInteractionType quantityPanelType)
    {
        SetDisplayActive(true);

        // Cache a pointer to the players inventory component
        if (playerInventoryComponent == null)
            playerInventoryComponent = GameManager.Instance.GetPlayer().GetPlayerInventory();

        // Cache display type and slot
        currentSlotDisplay = slotDisplay;
        currentInventoryDisplay = inventoryDisplay;
        currentDisplayType = quantityPanelType;

        // Stop the PlayerInventoryComponent's and InventoryDisplay's controls
        playerInventoryComponent.SetControlsSubscription(false);
        currentInventoryDisplay.SetControlsSubscription(false);

        // Reset & clamp the sliders range
        currentAmountSelected = 1;
        _amountSlider.value = 1;
        _amountSlider.maxValue = slotDisplay.AssignedSlot.GetSlotsItem().GetAmount();

        // Change the primary function button's text to match the InventoryDisplay type
        switch (quantityPanelType)
        {
            case EQuantityInteractionType.Trash:
                _primaryText.text = trashItemText;
                break;

            case EQuantityInteractionType.Move:
                _primaryText.text = moveItemText;
                break;

            case EQuantityInteractionType.Transaction:
                _primaryText.text = sellItemText;
                break;
        }
    }

    #region Button & Slider Methods

    /// <summary>
    /// This method should be called each time the slider is updated
    /// </summary>
    public void ProcessSlider()
    {
        // Update the current amount selected to match the sliders value
        currentAmountSelected = (int)_amountSlider.value;

        // Refresh UI
        RefreshSelectionMethods();
    }

    /// <summary>
    /// This method should be called each time the input field is updated
    /// </summary>
    public void ProcessInputField()
    {
        string text = _amountFeild.text;

        if (text != "")
        {
            // Update the current amount selected to match the input fields value
            currentAmountSelected = Mathf.Clamp(int.Parse(text), 0, currentSlotDisplay.AssignedSlot.GetSlotsItem().GetAmount());

            // Refresh UI
            RefreshSelectionMethods();
        }
    }

    /// <summary>
    /// This method resets all of the UI back to a default state and closes it
    /// This method should also be on the cancle button
    /// </summary>
    public void ClearPanel()
    {
        // Hide this display
        SetDisplayActive(false);

        // Enable the PlayerInventoryComponent's and InventoryDisplay's controls
        playerInventoryComponent.SetControlsSubscription(true);
        currentInventoryDisplay.SetControlsSubscription(true);

        // Reset vars to a null state
        currentSlotDisplay = null;
        currentInventoryDisplay = null;
        currentAmountSelected = 0;
    }

    /// <summary>
    /// This method takes the amount that the player had selected when the button is pushed and uses the item based on the inventoryDisplay type
    /// </summary>
    public void PrimaryButton()
    {
        // Determine the purpose for the items the player took
        switch (currentDisplayType)
        {
            case EQuantityInteractionType.Trash:
                // Trash the items selected
                currentSlotDisplay.AssignedSlot.GetSlotsItem().RemoveAmount(currentAmountSelected);
                break;

            case EQuantityInteractionType.Move:
                break;

            case EQuantityInteractionType.Transaction:
                break;
        }

        // Close Panel
        ClearPanel();
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// This method updates the quantity selection methods (Slider and Input Field) to match the current quanityt selected
    /// </summary>
    private void RefreshSelectionMethods()
    {
        _amountFeild.text = currentAmountSelected.ToString();
        _amountSlider.value = currentAmountSelected;
    }
    #endregion
}
