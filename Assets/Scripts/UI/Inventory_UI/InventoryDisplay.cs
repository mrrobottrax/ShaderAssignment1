using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryDisplay : MenuDisplayBase
{
    [Header("Slots")]
    [SerializeField] private InventorySlotDisplay[] _displaySlots;

    [Header("UI Elements")]
    protected CanvasGroup canvasGroup;
    [SerializeField] private RectTransform _dropRect;
    [SerializeField] private CanvasGroup _dropDisplayAlpha;

    [Header("Held Item")]
    [SerializeField] private InventoryCursor _inventoryCursor;
    [SerializeField] private Vector2 _cursorOffset;

    [Header("Components")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _dropPoint;

    // System
    public InventoryComponent PairedInventoryComponent { get; private set; }

    private InventorySlotDisplay prevHighlightedSlot;
    private InventorySlotDisplay prevPressedSlot;

    #region Initialization Methods

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        SetDropRectActive(false);
    }

    /// <summary>
    /// This method pairs all of the displays slots with an inventories
    /// </summary>
    public void AssignInventory(InventoryComponent inventoryComp)
    {
        // Store the newly paired inventory
        PairedInventoryComponent = inventoryComp;
        PairedInventoryComponent.Inventory.OnSlotChanged += RefreshSlots;
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
            InventorySlotDisplay displaySlot = _displaySlots[i];
            displaySlot.PairSlotToDisplay(i, PairedInventoryComponent.Inventory, this);
        }
    }

    /// <summary>
    /// This method clears the pairing with an inventory and resets the highlighted slot
    /// </summary>
    public void ClearInventoryPairing()
    {
        prevHighlightedSlot?.SetDisplayHighlighted(false);

        if (PairedInventoryComponent != null)
        {
            PairedInventoryComponent.Inventory.OnSlotChanged -= RefreshSlots;
            PairedInventoryComponent = null;
        }

        // Loop through each display slot
        for (int i = 0; i < _displaySlots.Length; i++)
        {
            // Cache the display slot
            InventorySlotDisplay displaySlot = _displaySlots[i];
            displaySlot.ClearDisplayPairing();
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

        SetDropRectActive(false);
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
        InputManager.Instance.UI.LookAxis.performed += OnMousePos;

        InputManager.Instance.UI.Fire1.performed += OnDropInput;
        InputManager.Instance.UI.Drop.performed += OnDropInput;
    }

    public override void Unsubscribe()
    {
        InputManager.Instance.UI.LookAxis.performed -= OnMousePos;

        InputManager.Instance.UI.Fire1.performed -= OnDropInput;
        InputManager.Instance.UI.Drop.performed -= OnDropInput;
    }

    /// <summary>
    /// Updates the mouse pos
    /// </summary>
    private void OnMousePos(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = context.ReadValue<Vector2>();
        Camera.main.ScreenToWorldPoint(mousePosition);

        // Update cursor pos
        _inventoryCursor.SetCursorPos(mousePosition + _cursorOffset);

        // Update drop rect
        if (_inventoryCursor.HasItem)
            SetDropRectActive(RectTransformUtility.RectangleContainsScreenPoint(_dropRect, mousePosition));
    }

    private void OnDropInput(InputAction.CallbackContext context)
    {
        bool tryDrop = context.ReadValueAsButton();

        if(tryDrop)
        {
            // Check if the mouse is within the rect for Fire1
            if (_inventoryCursor.HasItem &&
                _dropDisplayAlpha.alpha == 1 &&
                context.action == InputManager.Instance.UI.Fire1)
            {
                // Mouse drop
                prevPressedSlot.AssignedSlot.GetSlotsItem().GetItemData().CreateItemObject
                    (
                    PairedInventoryComponent.DropPoint.position, 
                    PairedInventoryComponent.DropPoint.rotation,
                    prevPressedSlot.AssignedSlot.GetSlotsItem().GetAmount()
                    );

                // Clear slot
                prevPressedSlot.AssignedSlot.ClearSlot();
                prevPressedSlot.SetDisplayInteractable(true);

                prevPressedSlot?.SetDisplayHighlighted(false);
                prevPressedSlot = null;

                prevHighlightedSlot?.SetDisplayHighlighted(false);
                prevHighlightedSlot = null;

                RefreshSlots();

                _inventoryCursor.ClearCursor();
            }
            else if (prevHighlightedSlot?.AssignedSlot.GetSlotsItem() != null && 
                context.action == InputManager.Instance.UI.Drop)
            {
                // Quick drop
                prevHighlightedSlot.AssignedSlot.GetSlotsItem().GetItemData().CreateItemObject
                    (
                    PairedInventoryComponent.DropPoint.position,
                    PairedInventoryComponent.DropPoint.rotation,
                    prevHighlightedSlot.AssignedSlot.GetSlotsItem().GetAmount()
                    );

                // Clear slot
                prevHighlightedSlot.AssignedSlot.ClearSlot();
                prevHighlightedSlot.SetDisplayInteractable(true);

                prevPressedSlot?.SetDisplayHighlighted(false);
                prevPressedSlot = null;

                prevHighlightedSlot?.SetDisplayHighlighted(false);
                prevHighlightedSlot = null;

                RefreshSlots();

                _inventoryCursor.ClearCursor();
            }
        }
    }
    #endregion

    #region Slot Display Methods

    /// <summary>
    /// This method should be called by a slot display when it is selected (Same as highlighting but for non mouse navigation)
    /// </summary>
    public void SlotSelected(InventorySlotDisplay selectedSlot)
    {
        prevHighlightedSlot?.SetDisplayHighlighted(false);

        selectedSlot.SetDisplayHighlighted(true);
        prevHighlightedSlot = selectedSlot;
    }

    /// <summary>
    /// This method should be called by a slot display when it is pressed
    /// Depending on the inventory display type, buttons on the item options display will be changed before enabling.
    /// </summary>
    public void SlotPressed(InventorySlotDisplay selectedSlotDisplay)
    {
        // Ignore clicks on empty slots if there is no previously pressed slot
        if (prevPressedSlot == null && selectedSlotDisplay.AssignedSlot?.GetSlotsItem() == null)
            return;

        // Transfer item to mouse
        if(prevPressedSlot == null)
        {
            prevPressedSlot = selectedSlotDisplay;

            // Stop interactions with the 
            selectedSlotDisplay.SetDisplayInteractable(false);
            _inventoryCursor.AssignItem(prevPressedSlot.AssignedSlot.GetSlotsItem());

            return;
        }
        else
        {
            // When a slot is selected and the transfer slot has an item, try to place it.
            if (selectedSlotDisplay.AssignedSlot.GetSlotsItem() == null)
            {
                Item_Base item = prevPressedSlot.AssignedSlot.GetSlotsItem();

                // Assign the item to the newly selected slot
                selectedSlotDisplay.AssignedSlot.AssignItem(item, item.GetAmount());

                // Clear the previously selected slot
                prevPressedSlot.AssignedSlot.ClearSlot();
                prevPressedSlot.SetDisplayInteractable(true);

                // Clear inventory cursor
                _inventoryCursor.ClearCursor();
            }
            else
            {
                // Swap the selected slot with the

                Item_Base prevItem = prevPressedSlot.AssignedSlot.GetSlotsItem();
                Item_Base SelectedItem = selectedSlotDisplay.AssignedSlot.GetSlotsItem();

                // Assign the item from the newly selected slot to the previous slot
                prevPressedSlot.AssignedSlot.AssignItem(SelectedItem, SelectedItem.GetAmount());
                prevPressedSlot.SetDisplayInteractable(true);

                // Assign the item from the previous slot to the nely selected one
                selectedSlotDisplay.AssignedSlot.AssignItem(prevItem, prevItem.GetAmount());

                // Clear inventory cursor
                _inventoryCursor.ClearCursor();

            }

            prevPressedSlot = null;
        }
    }
    #endregion

    #region Drop Rect Methods
    
    private void SetDropRectActive(bool isActive)
    {
        _dropDisplayAlpha.alpha = isActive ? 1 : 0;
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
