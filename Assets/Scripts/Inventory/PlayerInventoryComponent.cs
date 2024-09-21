using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventoryComponent : InventoryComponent
{
    [Header("Equipment Slot Pointers")]
	private readonly InventorySlotPointer _heldItemSlot = new();

	private readonly InventorySlotPointer _headSlot = new();
    private readonly InventorySlotPointer _chestSlot = new();
    private readonly InventorySlotPointer _legsSlot = new();
    private readonly InventorySlotPointer _feetSlot = new();

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

    /// <summary>
    /// This method either subscribes or unsubscribes the players controls
    /// </summary>
    public void SetControlsSubscription(bool isSubscribing)
    {
        if (isSubscribing)
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
        else
        {
			// Ensure that the input manager exists
			if (InputManager.Instance == null)
				return;

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
        else playerUIManager.DisableActiveDisplay(); // Disable the faourite wheel display
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
    public void EquipItem(InventorySlot selectedSlot)
    {
        // Check if the selected slots item is equippable
        if (selectedSlot != null && 
            selectedSlot.GetSlotsItem() != null && 
            selectedSlot.GetSlotsItem() is IEquippableItem equippableItem)
        {
            InventorySlotPointer slotToPairTo = null;

            // Find the slots items type to establish a pairing with a pointer for future references
            if (equippableItem is Weapon_Item)
            {
                slotToPairTo = _heldItemSlot;
            }
            else if (selectedSlot.GetSlotsItem() is Armour_Item armourItem)
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
                // Try to unequip the previous item if one is present
                if (slotToPairTo.GetPairedSlot()?.GetSlotsItem() != null)
                    UnequipItem(selectedSlot);

                // Establish the new slot pairing and equip the item
                slotToPairTo.SetPairedSlot(selectedSlot);
                equippableItem.Equip();
            }
        }
        else // If an item that does not exist was passed in, unequip the current item.
        {
            if (_heldItemSlot.GetPairedSlot() != null)
                UnequipItem(_heldItemSlot.GetPairedSlot());
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
            // Find the slots items type
            if (selectedSlot.GetSlotsItem() is Weapon_Item)
            {
                // Clear the established pairing
                _heldItemSlot.ClearPairedSlot();
            }
            else if (selectedSlot.GetSlotsItem() is Armour_Item armourItem)
            {
                Armour_ItemData itemData = armourItem.GetArmourData();

                switch (itemData.ArmorsType)
                {
                    case Armour_ItemData.ArmorType.HeadPiece:
                        _headSlot.ClearPairedSlot();
                        break;

                    case Armour_ItemData.ArmorType.ChestPiece:
                        _chestSlot.ClearPairedSlot();
                        break;

                    case Armour_ItemData.ArmorType.Legs:
                        _legsSlot.ClearPairedSlot();
                        break;

                    case Armour_ItemData.ArmorType.Feet:
                        _feetSlot.ClearPairedSlot();
                        break;
                }
            }

            // Unequip the item
            equippableItem.UnEquip();
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
