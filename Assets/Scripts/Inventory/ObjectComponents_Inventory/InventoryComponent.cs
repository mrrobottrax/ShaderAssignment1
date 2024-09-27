using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InventoryComponent : MonoBehaviour
{
    public Inventory Inventory => inventory;
    protected Inventory inventory;

    [SerializeField] private int _inventorySize;    

    [field: Header("Components")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerActions _playerActions;

    [field: Header("Equipment Slot Pointers")]
    public InventorySlot HeldItemSlot { get; private set; }

    [Header("System")]
    private PlayerUIManager playerUIManager;

    #region Initialization Methods

    private void Awake()
    {
        playerUIManager = GetComponentInChildren<PlayerUIManager>();

        inventory = new Inventory(_inventorySize);
    }

    private void Start()
    {
        SetControlsSubscription(true);
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
        if (!inventoryUI.InventoryDisplay.GetDisplayActive())
            inventoryUI.DisplayInventory(this);
        else playerUIManager.DisableActiveDisplay(); // Disable the inventory display
    }

    /// <summary>
    /// This method allows PC users to use a standard hotbar by selecting the slot corresponding to the number value of the key pressed
    /// </summary>
    private void HotBarInput(InputAction.CallbackContext context)
    {
        int key = (int)context.ReadValue<float>();

        InventorySlot newSlot = Inventory.Slots[key];

        if (HeldItemSlot != newSlot)
        {
            // Try to unsubsribe to the previous helditem slot's updates if it changed
            if (HeldItemSlot != null)
                HeldItemSlot.OnSlotChanged -= OnHighlightedSlotUpdate;

            // Equip an item if a new slot was selected
            SetEquipSlot(Inventory.Slots[key]);

            // Subsribe to the new helditem slot's updates
            HeldItemSlot.OnSlotChanged += OnHighlightedSlotUpdate;

            _playerActions.SetPlayerReady(true);
        }
    }

    #endregion

    #region Equipment Methods

    /// <summary>
    /// This method will attempt to pair a selected slot with the correct pointer slot, based on the items type.
    /// </summary>
    public void SetEquipSlot(InventorySlot itemsSlot)
    {
        // Try to unequip an item if the selected slot has one
        TryUnequipItem();

        HeldItemSlot = itemsSlot;

        // Check if the selected slots item is equippable
        if (itemsSlot != null && itemsSlot?.GetSlotsItem() is IEquippableItem equippableItem)
            equippableItem.Equip(_playerHealth);
    }

    /// <summary>
    /// Unequips the item from the specified inventory slot and clears any established pairings.
    /// </summary>
    /// <param name="selectedSlot">The slot containing the item to be unequipped.</param>
    public void TryUnequipItem()
    {
        // Check if the selected slots item is equippable
        if (HeldItemSlot != null && HeldItemSlot.GetSlotsItem() is IEquippableItem equippableItem)
        {
            // Unequip the item
            equippableItem.UnEquip(_playerHealth);
        }
        else // Slot is emppty, clear view model.
            _playerHealth.GetViewModelManager().ClearCurrentViewModel();
    }
    #endregion

    /// <summary>
    /// Listens for changes in the held items slots state.
    /// </summary>
    private void OnHighlightedSlotUpdate(InventorySlot updatedSlot)
    {
        if (updatedSlot == HeldItemSlot && HeldItemSlot.GetSlotsItem() != null)
        {
            SetEquipSlot(updatedSlot);
            return;
        }

        TryUnequipItem();
    }
}
