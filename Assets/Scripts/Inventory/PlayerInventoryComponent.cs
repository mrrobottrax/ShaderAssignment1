using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventoryComponent : InventoryComponent, IInputHandler
{
    [field: Header("Equipment Slot Pointers")]
    public InventorySlotPointer _heldItemSlot { get; private set; } = new();

	private InventorySlotPointer _headSlot = new();
    private InventorySlotPointer _chestSlot = new();
    private InventorySlotPointer _legsSlot = new();
    private InventorySlotPointer _feetSlot = new();

    [Header("Favourited Items")]
    [SerializeField] private int _favouriteSlotsSize = 8;
    private List<InventorySlotPointer> favouriteSlots;

    [Header("System")]
    private PlayerUIManager playerUIManager;
    private FavouriteWheelDisplay favouriteWheelDisplay;

    #region Initialization Methods

    protected override void Awake()
    {
        base.Awake();

        // Make the new slots list
        favouriteSlots = new List<InventorySlotPointer>(_favouriteSlotsSize);

        // Fill the list
        for (int i = 0; i < _favouriteSlotsSize; i++)
        {
            InventorySlotPointer slot = new();
            favouriteSlots.Add(slot);
        }

        playerUIManager = GetComponentInChildren<PlayerUIManager>();
    }
    private void Start()
    {
        SetControlsSubscription(true);

        // Cache ref to favourites wheel
        favouriteWheelDisplay = playerUIManager.FavouritesWheel;

        // Assign the created favourite slots to the favourite wheel display
        favouriteWheelDisplay.AssignSlots(favouriteSlots.ToArray());
    }
    #endregion

    #region Input Methods

    public void Subscribe()
    {
        // Inventory
        InputManager.Instance.controls.Permanents.Inventory.performed += InventoryInput;

        // Favourites wheel
        InputManager.Instance.controls.Permanents.FavouritesWheel.performed += FavouriteWheelInput;
        InputManager.Instance.controls.Permanents.FavouritesWheel.canceled += FavouriteWheelInput;

        // Hotbar
        InputManager.Instance.controls.Player._1.performed += HotBarInput;
        InputManager.Instance.controls.Player._2.performed += HotBarInput;
        InputManager.Instance.controls.Player._3.performed += HotBarInput;
        InputManager.Instance.controls.Player._4.performed += HotBarInput;
        InputManager.Instance.controls.Player._5.performed += HotBarInput;
        InputManager.Instance.controls.Player._6.performed += HotBarInput;
        InputManager.Instance.controls.Player._7.performed += HotBarInput;
        InputManager.Instance.controls.Player._8.performed += HotBarInput;
    }

    public void Unsubscribe()
    {
        InputManager.Instance.controls.Permanents.Inventory.performed -= InventoryInput;

        // Favourites wheel
        InputManager.Instance.controls.Permanents.FavouritesWheel.performed -= FavouriteWheelInput;
        InputManager.Instance.controls.Permanents.FavouritesWheel.canceled -= FavouriteWheelInput;

        // Hotbar
        InputManager.Instance.controls.Player._1.performed -= HotBarInput;
        InputManager.Instance.controls.Player._2.performed -= HotBarInput;
        InputManager.Instance.controls.Player._4.performed -= HotBarInput;
        InputManager.Instance.controls.Player._5.performed -= HotBarInput;
        InputManager.Instance.controls.Player._6.performed -= HotBarInput;
        InputManager.Instance.controls.Player._7.performed -= HotBarInput;
        InputManager.Instance.controls.Player._8.performed -= HotBarInput;
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
        if (!inventoryUI.GetPlayerInventoryDisplay().GetDisplayActive())
            inventoryUI.DisplayInventory(this);
        else playerUIManager.DisableActiveDisplay(); // Disable the inventory display
    }

    /// <summary>
    /// This method gathers the input necesary to determine if the players favourite wheel display should be toggled on or off. 
    /// </summary>
    private void FavouriteWheelInput(InputAction.CallbackContext context)
    {
        // Stop the input if another menu is already open
        if (playerUIManager.GetActiveDisplay() != null && playerUIManager.GetActiveDisplay() != playerUIManager.FavouritesWheel)
            return;

        // Display the favourites wheel if it is not active
        if (!playerUIManager.FavouritesWheel.GetDisplayActive())
            playerUIManager.SetActiveDisplay(favouriteWheelDisplay);
        else playerUIManager.DisableActiveDisplay(); // Disable the favourite wheel display
    }

    /// <summary>
    /// This method allows PC users to use a standard hotbar by selecting the slot corresponding to the number value of the key pressed
    /// </summary>
    private void HotBarInput(InputAction.CallbackContext context)
    {
        int key = (int)context.ReadValue<float>();

        // Use the items favourite function
        if(favouriteSlots[key].GetPairedSlot()?.GetSlotsItem() is IFavouritableItem favouritableItem)
            favouritableItem.UseFavouritedItem();
    }

    #endregion

    #region Equipment Methods

    /// <summary>
    /// This method will attempt to pair a selected slot with the correct pointer slot, based on the items type.
    /// </summary>
    public void EquipItem(InventorySlot itemsSlot)
    {
        // Check if the selected slots item is equippable
        if (itemsSlot != null && 
            itemsSlot.GetSlotsItem() != null && 
            itemsSlot.GetSlotsItem() is IEquippableItem equippableItem)
        {
            InventorySlotPointer slotToPairTo = null;

            // Find the slots items type to establish a pairing with a pointer for future references
            if (equippableItem is Weapon_Item)
            {
                slotToPairTo = _heldItemSlot;
            }
            else if (itemsSlot.GetSlotsItem() is Armour_Item armourItem)
            {
                // Get the armour data of the item
                Armour_ItemData itemData = armourItem.GetArmourData();

                // Find the equipment slot based on the armour type
                switch (itemData.ArmorsType)
                {
                    case Armour_ItemData.ArmorType.HeadPiece:
                        slotToPairTo = _headSlot;
                        break;

                    case Armour_ItemData.ArmorType.ChestPiece:
                        slotToPairTo = _chestSlot;
                        break;

                    case Armour_ItemData.ArmorType.Legs:
                        slotToPairTo = _legsSlot;
                        break;

                    case Armour_ItemData.ArmorType.Feet:
                        slotToPairTo = _feetSlot;
                        break;
                }
            }

            // Check if a slot was chosen to pair with
            if (slotToPairTo != null)
            {
                // Try to unequip an item if the selected slot has one
                if (slotToPairTo.GetPairedSlot()?.GetSlotsItem() != null)
                    UnequipItem(slotToPairTo.GetPairedSlot());

                // Establish the new slot pairing and equip the item
                slotToPairTo.SetPairedSlot(itemsSlot);
                equippableItem.Equip();
            }
        }
        else if (_heldItemSlot.GetPairedSlot() != null) // If an item that does not exist was passed in, unequip the current item.
            UnequipItem(_heldItemSlot.GetPairedSlot());
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

            InventorySlotPointer slotToClear = null;

            // Find the slots items type
            if (selectedSlot.GetSlotsItem() is Weapon_Item)
            {
                slotToClear = _heldItemSlot;
            }
            else if (selectedSlot.GetSlotsItem() is Armour_Item armourItem)
            {
                Armour_ItemData itemData = armourItem.GetArmourData();

                switch (itemData.ArmorsType)
                {
                    case Armour_ItemData.ArmorType.HeadPiece:
                        slotToClear = _headSlot;
                        break;

                    case Armour_ItemData.ArmorType.ChestPiece:
                        slotToClear = _chestSlot;
                        break;

                    case Armour_ItemData.ArmorType.Legs:
                        slotToClear = _legsSlot;
                        break;

                    case Armour_ItemData.ArmorType.Feet:
                        slotToClear = _feetSlot;
                        break;
                }
            }

            // Unequip the item
            equippableItem.UnEquip();
            slotToClear.ClearPairedSlot();
        }
    }
    #endregion

    #region Favourited Items Methods

    /// <summary>
    /// This method searches for an empty spot to place a favourited item, returns the slot, and its index.
    /// </summary>
    public InventorySlotPointer GetEmptyFavouriteSlot(out int slotIndex)
    {
        slotIndex = favouriteSlots.FindIndex(i => i.GetPairedSlot() == null);
        return slotIndex != -1 ? favouriteSlots[slotIndex] : null;
    }

    /// <summary>
    /// This method pairs a favourite slot with a specified ID to a InventorySlot.
    /// </summary>
    public void AssignFavouritePointer(int index, InventorySlot slot)
    {
        favouriteSlots[index].SetPairedSlot(slot);
    }

    /// <summary>
    ///  This method clears a favourite slots pairing with a specified ID.
    /// </summary>
    public void ClearFavouritePointer(int index)
    {
        favouriteSlots[index].ClearPairedSlot();
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// This method returns the favourite pointer at a specified index value
    /// </summary>
    public InventorySlotPointer GetFavouritePointer(int index)
    {
        return favouriteSlots[index];
    }

    /// <summary>
    /// This method returns the head slot pointer.
    /// </summary>
    public InventorySlotPointer GetHeadSlotPointer()
    {
        return _headSlot;
    }

    /// <summary>
    /// This method returns the chest slot pointer.
    /// </summary>
    public InventorySlotPointer GetChestSlotPointer()
    {
        return _chestSlot;
    }

    /// <summary>
    /// This method returns the legs slot pointer.
    /// </summary>
    public InventorySlotPointer GetLegsSlotPointer()
    {
        return _legsSlot;
    }

    /// <summary>
    /// This method returns the feet slot pointer.
    /// </summary>
    public InventorySlotPointer GetFeetSlotPointer()
    {
        return _feetSlot;
    }

    #endregion
}
