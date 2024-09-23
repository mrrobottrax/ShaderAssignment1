using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class ToolbeltDisplay : MenuDisplayBase
{
    [Header("Slots")]
    [SerializeField] private ToolbeltSlotDisplay[] _displaySlots;

    [Header("UI Elements")]
    protected CanvasGroup canvasGroup;

    [field: Header("System")]
    protected InventoryComponent pairedInventoryComponent;// Inventory and locally organized slots
    public InventoryComponent PairedInventoryComponent => pairedInventoryComponent;
    protected Inventory pairedInventory;

    #region Initialization Methods

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// This method pairs all of the displays slots with an inventories
    /// </summary>
    public void AssignInventory(InventoryComponent inventoryComp)
    {
        // Store the newly paired inventory
        pairedInventoryComponent = inventoryComp;
        pairedInventory = inventoryComp.Inventory;

        pairedInventory.OnSlotChanged += RefreshSlots;
    }

    /// <summary>
    /// This method clears the pairing with an inventory and resets the highlighted slot
    /// </summary>
    public void ClearInventoryPairing()
    {
        if (pairedInventory != null)
        {
            pairedInventory.OnSlotChanged -= RefreshSlots;
            pairedInventoryComponent = null;
            pairedInventory = null;
        }
    }
    #endregion

    #region Implemented Methods
    protected override void OnEnableDisplay()
    {
        base.OnEnableDisplay();

        // Enable canvas group interactions if one has been found
        if (canvasGroup != null)
            canvasGroup.interactable = true;

        // Refresh both filtered and display slots
        RefreshSlots();
    }

    protected override void OnDisableDisplay()
    {
        // Disable canvas group interactions if one has been found
        if (canvasGroup != null)
            canvasGroup.interactable = false;

        base.OnDisableDisplay();

        ClearInventoryPairing();
    }
    #endregion

    #region Input Methods

    /// <summary>
    /// This method either subscribes or unsubscribes the UI controls
    /// </summary>
    public override void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }


    public override void Subscribe()
    {
        
    }

    public override void Unsubscribe()
    {
        
    }
    #endregion

    #region Scroll Methods

    #endregion

    #region Slot Display Methods

    /// <summary>
    /// This method should be called by a slot display when it is selected (Same as highlighting but for non mouse navigation)
    /// </summary>
    public void SlotSelected(ToolbeltSlotDisplay selectedSlot)
    {
        
    }

    /// <summary>
    /// This method should be called by a slot display when it is pressed
    /// Depending on the inventory display type, buttons on the item options display will be changed before enabling.
    /// </summary>
    public void SlotPressed(ToolbeltSlotDisplay selectedSlotDisplay)
    {
        // Transfer item to mouse
    }

    /// <summary>
    /// Pairs the display slots with an inventories slots based on the start and end indices
    /// </summary>
    public void PairDisplaySlots()
    {
        // Loop through each display slot
        for (int i = 0; i < _displaySlots.Length; i++)
        {
            // Cache the display slot
            ToolbeltSlotDisplay displaySlot = _displaySlots[i];

            displaySlot.PairSlot(pairedInventory.Slots[i], this);
        }
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// This method refreshes the list of slots used to pair with the display slots, then refreshes the display
    /// </summary>
    public virtual void RefreshSlots()
    {
        // Refresh display to newly sorted slots
        PairDisplaySlots();
    }
    #endregion    
}
