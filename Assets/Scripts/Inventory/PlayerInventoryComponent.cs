using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventoryComponent : InventoryComponent, IInputHandler
{
    [field: Header("Components")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerActions _playerActions;

    [field: Header("Equipment Slot Pointers")]
    public InventorySlotPointer _heldItemSlot { get; private set; } = new();

    [Header("System")]
    private PlayerUIManager playerUIManager;

    #region Initialization Methods

    protected override void Awake()
    {
        base.Awake();

        playerUIManager = GetComponentInChildren<PlayerUIManager>();
    }
    private void Start()
    {
        SetControlsSubscription(true);

        _heldItemSlot.OnPointerChanged += HeldItemSwitch;
    }
    #endregion

    #region Input Methods

    public void Subscribe()
    {
        // Inventory
        InputManager.Instance.Permanents.Inventory.performed += InventoryInput;

        // Hotbar
        InputManager.Instance.Player._1.performed += HotBarInput;
        InputManager.Instance.Player._2.performed += HotBarInput;
        InputManager.Instance.Player._3.performed += HotBarInput;
    }

    public void Unsubscribe()
    {
        InputManager.Instance.Permanents.Inventory.performed -= InventoryInput;

        // Hotbar
        InputManager.Instance.Player._1.performed -= HotBarInput;
        InputManager.Instance.Player._2.performed -= HotBarInput;
        InputManager.Instance.Player._3.performed -= HotBarInput;
    }

    public void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }

    /// <summary>
    /// This method gathers the input necesary to determine if the players inventory display should be toggled on or off. 
    /// </summary>
    private void InventoryInput(InputAction.CallbackContext context)
    {
        InventoryUI inventoryUI = playerUIManager.InventoryUI;

        // Display the players inventory if it is not already active
        if (!inventoryUI.ToolBeltDisplay.GetDisplayActive())
            inventoryUI.DisplayInventory(this);
        else playerUIManager.DisableActiveDisplay(); // Disable the inventory display
    }

    /// <summary>
    /// This method allows PC users to use a standard hotbar by selecting the slot corresponding to the number value of the key pressed
    /// </summary>
    private void HotBarInput(InputAction.CallbackContext context)
    {
        int key = (int)context.ReadValue<float>();

        if (Inventory.Slots[key].GetSlotsItem() != null)
        {
            TryEquipItem(Inventory.Slots[key]);
            _playerActions.SetPlayerReady(true);

            return;
        }
        else if (_heldItemSlot.GetPairedSlot() != null)
        {
            UnequipItem(_heldItemSlot.GetPairedSlot());
            return;
        }

        // Pull out arms since an empty slot was pressed
        _playerActions.SetPlayerReady(true);

    }

    #endregion

    #region Equipment Methods

    /// <summary>
    /// This method will attempt to pair a selected slot with the correct pointer slot, based on the items type.
    /// </summary>
    public void TryEquipItem(InventorySlot itemsSlot)
    {
        // Check if the selected slots item is equippable
        if (itemsSlot != null && 
            itemsSlot != _heldItemSlot.GetPairedSlot() &&
            itemsSlot.GetSlotsItem() != null && 
            itemsSlot.GetSlotsItem() is IEquippableItem equippableItem)
        {

            // Check if a slot was chosen to pair with
            if (_heldItemSlot != null)
            {
                // Try to unequip an item if the selected slot has one
                if (_heldItemSlot.GetPairedSlot()?.GetSlotsItem() != null)
                    UnequipItem(_heldItemSlot.GetPairedSlot());

                // Establish the new slot pairing and equip the item
                _heldItemSlot.SetPairedSlot(itemsSlot);
                equippableItem.Equip(_playerHealth);
            }
        }
    }

    /// <summary>
    /// Unequips the item from the specified inventory slot and clears any established pairings.
    /// </summary>
    /// <param name="selectedSlot">The slot containing the item to be unequipped.</param>
    public void UnequipItem(InventorySlot selectedSlot)
    {
        // Check if the selected slots item is equippable
        if (selectedSlot.GetSlotsItem() is IEquippableItem equippableItem)
        {
            Debug.Log("Try Unequip");

            // Unequip the item
            equippableItem.UnEquip();
            _heldItemSlot.ClearPairedSlot();
        }
    }

    /// <summary>
    /// Handles the switching of held items
    /// </summary>
    private void HeldItemSwitch(InventorySlotPointer pointer)
    {
        if (pointer.GetPairedSlot() != null)
        {
            if (pointer.GetPairedSlot().GetSlotsItem() != null)
            {
                // Unequip the prev item, then equip the new one.
                UnequipItem(pointer.GetPairedSlot());
                TryEquipItem(pointer.GetPairedSlot());
            }
            else UnequipItem(pointer.GetPairedSlot());
        }
    }
    #endregion
}
