public class InventorySlotPointer
{
    private InventorySlot pairedSlot;

    /// <summary>
    /// Gets the paired inventory slot.
    /// </summary>
    /// <returns>The currently paired InventorySlot.</returns>
    public InventorySlot GetPairedSlot()
    {
        return pairedSlot;
    }

    /// <summary>
    /// Sets the paired inventory slot and subscribes to its change events.
    /// </summary>
    /// <param name="slot">The InventorySlot to be paired.</param>
    public virtual void SetPairedSlot(InventorySlot slot)
    {
        pairedSlot = slot;

        // Subscribe to changes made by the paired slot
        pairedSlot.OnSlotChanged += PairedSlotChanged;
    }

    /// <summary>
    /// Clears the paired inventory slot and unsubscribes from its change events.
    /// </summary>
    public virtual void ClearPairedSlot()
    {
        // Stop listening for changes made to the paired slot
        pairedSlot.OnSlotChanged -= PairedSlotChanged;

        pairedSlot = null;
    }

    /// <summary>
    /// Handles changes to the paired inventory slot.
    /// </summary>
    /// <param name="slot">The InventorySlot that has changed.</param>
    protected virtual void PairedSlotChanged(InventorySlot slot)
    {
        // Ensure the changes made were to the same slot as the paired slot
        if (pairedSlot == slot && pairedSlot.GetSlotsItem() == null)
        {
            ClearPairedSlot();
        }
    }
}
