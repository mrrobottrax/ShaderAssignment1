using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryDisplay : InventoryDisplay
{
    [Header("Components")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _filledSlotsText;
    [SerializeField] private Slider _healthBar;
    [SerializeField] private TextMeshProUGUI _goldCounter;

    #region Implemented Methods
    protected override void OnEnableDisplay()
    {
        base.OnEnableDisplay();

        // Update player stats
        _healthBar.value = _playerHealth.Health;
    }

    protected override void OnDisableDisplay()
    {
        base.OnDisableDisplay();
    }

    public override void RefreshSlots()
    {
        base.RefreshSlots();

        // Update filled slots text
        _filledSlotsText.text = $"{pairedInventory.GetFilledSlots()} / {pairedInventory.Slots.Count}";
    }

    public override void TryDisplayMainFunction(InventorySlotDisplay selectedUISlot)
    {
        if (selectedUISlot.AssignedSlot.GetSlotsItem() != null)
        {
            var item = selectedUISlot.AssignedSlot.GetSlotsItem();

            // Alter selection behaviour based on item type
            switch (item)
            {
                case Armour_Item:
                    Debug.Log("Try Equip Armor");
                    TryEquipItem(selectedUISlot.AssignedSlot);
                    break;

                case Aid_Item aid_Item:
                    Debug.Log("Consume Item");
                    ConsumeAidItem(aid_Item);
                    break;

                case Weapon_Item:
                    Debug.Log("Try Equip Weapon");
                    TryEquipItem(selectedUISlot.AssignedSlot);
                    break;
            }
        }

        // Re-Pair all inventory slots to account for array size changes
        PairDisplaySlots(topDisplayIndex);
    }
    #endregion

    /// <summary>
    /// This method will try to equip or unequip an equipable item based on its current state.
    /// </summary>
    private void TryEquipItem(InventorySlot slot)
    {
        if (pairedInventoryComponent is PlayerInventoryComponent playerInventory)
        {
            // Check if the item is equippable
            if (slot.GetSlotsItem() is IEquippableItem equippableItem)
            {
                // Check if the item is equippable and is not equipped
                if (!equippableItem.IsEquipped)
                    playerInventory.EquipItem(slot); // Equip the item if it is not
                else playerInventory.UnequipItem(slot); // Unequip an already equipped item
            }
        }
    }

    /// <summary>
    /// This method will consume a selected item
    /// </summary>
    private void ConsumeAidItem(Aid_Item aid_Item)
    {
        aid_Item.ConsumeItem(_playerHealth);
    }

    /// <summary>
    /// This method will attempt to favourite an item in the players inventory if possible.
    /// </summary>
    public void TryFavouriteItem(InventorySlotDisplay selectedUISlot)
    {
        if (pairedInventoryComponent is PlayerInventoryComponent playerInventory)
            if (selectedUISlot.AssignedSlot.GetSlotsItem() is IFavouritableItem item)
            {
                // Favourite the item
                if (!item.IsItemFavourited)
                {
                    InventorySlotPointer emptySlotPointer = playerInventory.GetEmptyFavouriteSlot(out int slotIndex);

                    // Ensure the slot returned was not null
                    if(emptySlotPointer != null)
                    {
                        // Set the favourite slot pointer to the selected slot
                        playerInventory.AssignFavouritePointer(slotIndex, selectedUISlot.AssignedSlot);
                        item.FavouriteItem(slotIndex);
                    }
                }
                else // Unfavourite the item
                {
                    item.UnfavouriteItem(out int favouriteSlotID);
                    playerInventory.ClearFavouritePointer(favouriteSlotID);
                }

                // Refresh display
                RefreshSlots();
            }
    }
}
