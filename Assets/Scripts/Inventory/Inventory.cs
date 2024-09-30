using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Inventory
{
    [Header("Slots")]
    [SerializeField] private List<InventorySlot> _slots;
    public List<InventorySlot> Slots => _slots;

    [field: Header("Events")]
    public event Action OnSlotChanged;

    #region Constructors

    /// <summary>
    /// This constructor defines the size of an inventory by creating all of its slots
    /// </summary>
    public Inventory(int size)
    {
        // Make the new slots list
        _slots = new List<InventorySlot>(size);

        // Fill the list
        for (int i = 0; i < size; i++)
        {
            InventorySlot slot = new InventorySlot();

            _slots.Add(slot);
            slot.OnSlotChanged += SlotChangedAction;
        }
    }

    /// <summary>
    /// Calls an event when a slot has been changed.
    /// This method is passed to each new slot that is created.
    /// </summary>
    private void SlotChangedAction(InventorySlot slotUpdated)
    {
        OnSlotChanged?.Invoke();
    }
    #endregion

    #region Inventory searching methods

    /// <summary>
    /// This method returns a list of all slots with an item type in them.
    /// </summary>
    public bool ContainsItem(int itemID, out List<InventorySlot> slot)
    {
        throw new System.NotImplementedException();
        //slot = _slots.Where(i => i.GetSlotsItem()?.GetItemData().ItemID == itemID).ToList();
        //return _slots == null ? false : true;
    }

    /// <summary>
    /// This overload returns the first slot with an item of the desired type.
    /// </summary>
    public bool ContainsItem(ItemData_Base item, out InventorySlot slot)
    {
        slot = null;
        foreach (InventorySlot i in Slots)
        {
            if (i.GetSlotsItem().GetItemData() == item)
            {
                slot = i;
            }
        }
        return slot == null ? false : true;
    }

    /// <summary>
    /// This method returns the total count of a type of item within an inventory.
    /// </summary>
    public int GetTotalOfItem(int itemID)
    {
        //int count = 0;
        //foreach (InventorySlot i in Slots)
        //{
        //    if (i.GetSlotsItem().GetItemData().ItemID != itemID)
        //        continue;
        //    count += i.GetSlotsItem().GetAmount();
        //}

        //return count;
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This method searches for an empty slot
    /// </summary>
    public bool GetEmptySlot(out InventorySlot emptySlot)
    {
        emptySlot = _slots.FirstOrDefault(i => !i.GetSlotsItem()?.GetItemData());
        return emptySlot == null ? false : true;
    }
    #endregion

    #region Slot management methods

    /// <summary>
    /// This method looks for the optimal slot to add an item to.
    /// </summary>
    public bool AddItem(ItemData_Base addedItem, int amount)
    {
        throw new System.NotImplementedException();

        //// First, check if any slots contain an item of the same type
        //if (ContainsItem(addedItem.ItemID, out List<InventorySlot> slotsWithItems))
        //{
        //    foreach (var slot in slotsWithItems)
        //    {
        //        bool fitsInSlot = slot.GetSlotsItem().CheckForRoom(amount, out int roomRemaining);

        //        // For caseses where the amount can fit within the stack
        //        if (fitsInSlot)
        //        {
        //            slot.GetSlotsItem().AddAmount(amount);
        //            return true;
        //        }
        //        else if (!fitsInSlot && roomRemaining > 0)// Attempt to fit as much into the stack as possible
        //        {
        //            // Fill the stack
        //            slot.GetSlotsItem().AddAmount(roomRemaining);

        //            // Find how much will be put in the other slot
        //            int leftover = amount - roomRemaining;

        //            // Try to find space for the remaning items
        //            AddItem(addedItem, leftover);
        //            return true;
        //        }
        //    }
        //}

        //// Check for the first empty slot
        //if (GetEmptySlot(out InventorySlot emptySlot))
        //{
        //    // Add the item then sort the inventory
        //    emptySlot.AssignItem(addedItem.CreateItemInstance(), amount);
        //    return true;
        //}

        //return false;
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// This method returns the amount of slots with items present in them
    /// </summary>
    public int GetFilledSlots()
    {
        int amount = 0;

        // Count each slot with an item
        foreach (InventorySlot i in _slots)
        {
            if (i.GetSlotsItem() != null)
                amount++;
        }

        return amount;
    }

    #endregion
}