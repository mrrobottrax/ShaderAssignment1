using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public abstract class InventoryDisplay : MenuDisplayBase
{
    [Header("Slots")]
    [SerializeField] private InventorySlotDisplay[] _displaySlots;

    [Header("UI Elements")]
    [SerializeField] private GameObject _inventoryHolder;
    [SerializeField] private RectTransform _interactionRect;// Used for scroll wheel controls
    protected CanvasGroup interactionCanvasGroup;

    [Header("Components")]
    [SerializeField] private ItemOptionsDisplay _itemOptions_Display;
    [SerializeField] private ItemDescriptionDisplay _itemDiscription_Display;

    [field: Header("System")]
    protected InventoryComponent pairedInventoryComponent;// Inventory and locally organized slots
    public InventoryComponent PairedInventoryComponent => pairedInventoryComponent;

    protected Inventory pairedInventory;

    private List<InventorySlot> filteredSlots = new List<InventorySlot>();// These are the slots used to pair with the display slots

    // Input
    private Vector2 mouseDelta;

    // Slot display
    FilterType currentFilter = FilterType.All;
    protected int topDisplayIndex; // The position in the inventory that the top most slot display represents
    private int highlightedSlotIndex; // The position in the inventory that the player is

    #region Initialization Methods

    private void Awake()
    {
        interactionCanvasGroup = GetComponent<CanvasGroup>();
        topDisplayIndex = 0;
        highlightedSlotIndex = 0;
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
        if(pairedInventory != null)
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
        if (interactionCanvasGroup != null)
            interactionCanvasGroup.interactable = true;

        // Refresh both filtered and display slots
        RefreshSlots();

        // Clear the description before trying to highlight a new slot
        _itemDiscription_Display.ClearDescription();

        // TO Do
        // Actually implement inventory entry logic
        /*
        // Ensure highlighted slot is within bounds
        if (_displaySlots[highlightedSlotIndex].AssignedSlot != null && _displaySlots[highlightedSlotIndex].AssignedSlot.GetSlotsItem() != null)
        {
            EventSystem.current.SetSelectedGameObject(_displaySlots[highlightedSlotIndex].gameObject);
        }
        */
    }

    protected override void OnDisableDisplay()
    {
        // Disable canvas group interactions if one has been found
        if (interactionCanvasGroup != null)
            interactionCanvasGroup.interactable = false;

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

    /// <summary>
    /// This method gets the mouses position when the mouse is moved
    /// </summary>
    private void MousePos(InputAction.CallbackContext context)
    {
        mouseDelta = Mouse.current.position.ReadValue();
    }

    /// <summary>
    /// This method takes the players navigation input and determines if their input should result in an increment of the UI
    /// </summary>
    private void Navigate(InputAction.CallbackContext context)
    {
        Vector2 navAxisRaw = context.ReadValue<Vector2>();

        // Check if mouse is within rect
        if (RectTransformUtility.RectangleContainsScreenPoint(_interactionRect, mouseDelta))
        {
            // Find delta of navigate axis
            if (navAxisRaw.y > 0)
                NavigateUp();
            else if (navAxisRaw.y < 0)
                NavigateDown();
        }
    }

    /// <summary>
    /// This method takes in the players scroll input and checks if the mouse is within the interaction rect of this UI.
    /// If true, take the scroll direction and move the UI up and down accordingly.
    /// </summary>
    private void ScrollMouse(InputAction.CallbackContext context)
    {
        Vector2 scrollAxisRaw = context.ReadValue<Vector2>();

        // Check if mouse is within rect
        if (RectTransformUtility.RectangleContainsScreenPoint(_interactionRect, mouseDelta))
        {
            // Find delta of scroll axis
            if (scrollAxisRaw.y > 0)
                ScrollUp();
            else if (scrollAxisRaw.y < 0)
                ScrollDown();
        }
    }

    public override void Subscribe()
    {
        // Mouse delta
        InputManager.Instance.Player.Look.performed += MousePos;

        // Navigation
        InputManager.Instance.UI.Navigate.performed += Navigate;

        // Mouse scroll
        InputManager.Instance.UI.ScrollWheel.performed += ScrollMouse;
    }

    public override void Unsubscribe()
    {
        // Mouse delta
        InputManager.Instance.Player.Look.performed -= MousePos;

        // Navigation
        InputManager.Instance.UI.Navigate.performed -= Navigate;

        // Mouse scroll
        InputManager.Instance.Player.Movement.performed -= ScrollMouse;
    }
    #endregion

    #region Scroll Methods

    /// <summary>
    /// This method increments the top most slot that is allowed to be displayed backwards by one
    /// </summary>
    private void ScrollUp()
    {
        // Ensure we never cross zero while scrolling
        if (topDisplayIndex > 0)
        {
            topDisplayIndex--;
            PairDisplaySlots(topDisplayIndex);
        }
    }

    /// <summary>
    /// This method increments the top most slot that is allowed to be displayed forwards by one
    /// </summary>
    private void ScrollDown()
    {
        // Ensure we never cross the final slot that has an item in it
        if (_displaySlots[GetLastDisplaySlot() - 1].GetInventoryIndex() < filteredSlots.Count - 1)
        {
            topDisplayIndex++;
            PairDisplaySlots(topDisplayIndex);
        }
    }

    /// <summary>
    /// This method increments the highlighted slot backwards by one
    /// </summary>
    private void NavigateUp()
    {
        if (highlightedSlotIndex > 1)
        {
            highlightedSlotIndex--;
        }
        else if (highlightedSlotIndex == 1)
            ScrollUp();
    }

    /// <summary>
    /// This method increments the highlighted slot forwords by one
    /// </summary>
    private void NavigateDown()
    {
        // Only navigate down if the highlighted slot index is less then the final interactable display slots.
        // This will ensure that the highlighted slot will remain within the display slots range
        if (highlightedSlotIndex < GetLastDisplaySlot())
        {
            highlightedSlotIndex++;
        }
        else if (filteredSlots.Count > _displaySlots.Length)
        {
            ScrollDown();
        }
    }

    /// <summary>
    /// This method loops through the display slots backwards to find the index of first slot with an item in it
    /// </summary>
    private int GetLastDisplaySlot()
    {
        int finalSlot = 0;

        // Loop through the display slots array backwards
        for (int i = _displaySlots.Length; i > 0; i--)
        {
            InventorySlotDisplay slotDisplay = _displaySlots[i - 1];

            // Get the index of the first slot with an item then break
            if (slotDisplay.AssignedSlot != null && slotDisplay.AssignedSlot.GetSlotsItem() != null)
            {
                finalSlot = slotDisplay.GetDisplayIndex();
                break;
            }
        }

        return finalSlot;
    }
    #endregion

    #region Slot Display Methods

    /// <summary>
    /// This method should be called by a slot display when it is selected (Same as highlighting but for non mouse navigation)
    /// </summary>
    public void SlotSelected(InventorySlotDisplay selectedSlot)
    {
        // Update the current index to match the button the player is hovered over
        highlightedSlotIndex = selectedSlot.GetDisplayIndex();

        // Update Description
        UpdateDescriptionComponent(selectedSlot);
    }

    /// <summary>
    /// This method should be called by a slot display when it is pressed
    /// Depending on the inventory display type, buttons on the item options display will be changed before enabling.
    /// </summary>
    public void SlotPressed(InventorySlotDisplay selectedSlotDisplay)
    {
        _itemOptions_Display.SetItem(selectedSlotDisplay, this);
    }

    /// <summary>
    /// Pairs the display slots with an inventories slots based on the start and end indices
    /// </summary>
    public void PairDisplaySlots(int start)
    {
        // Loop through each display slot
        for (int i = 0; i < _displaySlots.Length; i++)
        {
            // Cache the display slot
            InventorySlotDisplay displaySlot = _displaySlots[i];

            // Get the index of the slot to pair with
            int slotPairIndex = start + i;

            // Pair the display slot to its corresponding inventory slot if possible
            if (filteredSlots.Count > slotPairIndex)
                displaySlot.PairSlot(i + 1, slotPairIndex, filteredSlots[slotPairIndex], this);
            else displaySlot.PairSlot(i + 1, slotPairIndex, null, this); // Setting null disables interactions with the display slot
        }
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// This method updates the item description display to match a slot
    /// </summary>
    private void UpdateDescriptionComponent(InventorySlotDisplay slot_Display)
    {
        // Check if the description display is present and this display is interactable
        if (_itemDiscription_Display && interactionCanvasGroup.interactable)
        {
            // If the slot has an item, display it. If not, clear the description display.
            if (slot_Display.AssignedSlot.GetSlotsItem() != null)
                _itemDiscription_Display.UpdateDescription(slot_Display.AssignedSlot.GetSlotsItem().GetItemData());
            else
                _itemDiscription_Display.ClearDescription();
        }
    }

    public enum FilterType
    {
        All,
        Armor,
        Weapon,
        Survival,
        Resource
    }

    /// <summary>
    /// This method updates the list of currently displaying slots to match items of a certain type
    /// </summary>
    public void FilterDisplaySlots(FilterType filterType)
    {
        currentFilter = filterType;

        // Clear the list of shown slots
        filteredSlots.Clear();

        if (pairedInventory.Slots.Count > 0)
            foreach (InventorySlot slot in pairedInventory.Slots)
            {
                // Check if the slot has an item
                if (slot.GetSlotsItem() != null)
                {
                    // Cache the slots item for comparison
                    ItemData_Base item = slot.GetSlotsItem().GetItemData();

                    // Compare slot with filter
                    switch (filterType)
                    {
                        case FilterType.All:
                            filteredSlots.Add(slot);
                            break;
                        case FilterType.Armor:
                            if (item.GetType() == typeof(Armour_ItemData))
                                filteredSlots.Add(slot);
                            break;
                        case FilterType.Weapon:
                            if (item.GetType() == typeof(MeleeWeapon_ItemData) || item.GetType() == typeof(RangedWeapon_ItemData))
                                filteredSlots.Add(slot);
                            break;
                        case FilterType.Survival:
                            if (item.GetType() == typeof(Aid_ItemData))
                                filteredSlots.Add(slot);
                            break;
                        case FilterType.Resource:
                            if (item.GetType() == typeof(Resource_ItemData))
                                filteredSlots.Add(slot);
                            break;
                        default:
                            break;
                    }
                }
            }
    }

    /// <summary>
    /// This method refreshes the list of slots used to pair with the display slots, then refreshes the display
    /// </summary>
    public virtual void RefreshSlots()
    {
        // Sort slots
        FilterDisplaySlots(currentFilter);

        // Refresh display to newly sorted slots
        PairDisplaySlots(topDisplayIndex);
    }
    #endregion

    #region Main Function
    public abstract void TryDisplayMainFunction(InventorySlotDisplay selectedUISlot);
    #endregion
}
