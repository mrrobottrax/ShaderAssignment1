public class Armour_Item : Item_Base, IEquippableItem
{
    private Armour_ItemData armourData;

    // Constructor
    public Armour_Item(Armour_ItemData baseData) : base(baseData)
    {
        armourData = baseData;
    }

    public bool IsEquipped => isEquipped;
    private bool isEquipped;

    #region Implemented Methods
    public void Equip()
    {
        isEquipped = true;
    }

    public void UnEquip()
    {
        isEquipped = false;
    }

    public override void SlotCleared(InventorySlot itemsSlot)
    {

    }
    #endregion

    /// <summary>
    /// Returns the items data as armour item data.
    /// </summary>
    /// <returns>This items armour data</returns>
    public Armour_ItemData GetArmourData()
    {
        return armourData;
    }
}
