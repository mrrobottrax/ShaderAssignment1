using System;
using UnityEngine;

[Serializable]
public abstract class Item_Base
{
    protected ItemData_Base itemData;

    [Header("System")]
    private int amountInStack;

    [Header("Events")]
    public Action OnItemChanged;

    /// <summary>
    /// This concrete constructor creates a new isntance of an item and sets its base data.
    /// Classes derived from item should implement their own version of this method that defines the type of item created.
    /// </summary>
    public Item_Base(ItemData_Base baseData)
    {
        itemData = baseData;
        amountInStack = 1;
    }

    /// <summary>
    /// Sets the amount of items present in the stack
    /// </summary>
    public void SetAmount(int value)
    {
        amountInStack = value;

        OnItemChanged?.Invoke();
    }

    /// <summary>
    /// This method should be called from the slot when it is cleared
    /// </summary>
    /// <param name="itemsSlot">The slot that is being cleared</param>
    public abstract void ItemsSlotCleared(InventorySlot itemsSlot);

    /// <summary>
    /// Adds a specified amount to the stack
    /// </summary>
    public void AddAmount(int amount)
    {
        SetAmount(amountInStack + amount);
    }

    /// <summary>
    /// Removes a specified amount to the stack
    /// </summary>
    public void RemoveAmount(int amount)
    {
        SetAmount(amountInStack - amount);
    }

    /// <summary>
    /// This method checks the stack for room left
    /// </summary>
    public bool CheckForRoom(int amount)
    {
        if (amountInStack + amount <= itemData.MaxAmount) return true;
        else return false;
    }

    /// <summary>
    /// This overload checks the stack for room left. Also, it returns the room in the slot remaining.
    /// </summary>
    public bool CheckForRoom(int amount, out int roomRemaining)
    {
        roomRemaining = itemData.MaxAmount - amountInStack;
        return CheckForRoom(amount);
    }

    #region Helper Methods


    /// <summary>
    /// Gets the item data associated with the current item.
    /// </summary>
    /// <returns>The ItemData_Base instance containing the item's data.</returns>
    public ItemData_Base GetItemData()
    {
        return itemData;
    }

    /// <summary>
    /// Returns the size of the stack
    /// </summary>
    public int GetAmount()
    {
        return amountInStack;
    }
    #endregion
}
