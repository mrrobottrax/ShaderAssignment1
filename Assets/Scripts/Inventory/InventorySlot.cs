using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    [Header("System")]
    private Item_Base slotsItem;

    public Action<InventorySlot> OnSlotChanged; // When this slot changes it will update the inventory

    #region Methods

    /// <summary>
    /// This method assigns an item with an amount
    /// </summary>
    public void AssignItem(Item_Base item, int amount)
    {
        // Set both the item and the amount assigned
        slotsItem = item;
        slotsItem.SetAmount(amount);

        // Subscribe inventory to item changes
        slotsItem.OnItemChanged += SlotChanged;
    }

    /// <summary>
    /// This method clears the slots data
    /// </summary>
    public void ClearSlot()
    {
        // Call the items cleared logic
        slotsItem.SlotCleared(this);

        // Unsub inventory from item changes
        slotsItem.OnItemChanged -= SlotChanged;

        slotsItem = null;
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// Returns the item in the slot
    /// </summary>
    public Item_Base GetSlotsItem()
    {
        return slotsItem;
    }

    /// <summary>
    /// This method should be called when a change has been made to a slot
    /// </summary>
    public void SlotChanged()
    {
        // Check if the slot needs to be cleared
        if (slotsItem != null)
        {
            int total = slotsItem.GetAmount();
            if (total <= 0)
            {
                ClearSlot();
            }
        }

        OnSlotChanged?.Invoke(this);
    }

    #endregion
}