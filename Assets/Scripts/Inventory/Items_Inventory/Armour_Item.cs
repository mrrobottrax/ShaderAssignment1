public class Armour_Item : Item_Base, IEquippableItem
{
    private Armour_ItemData armourData;

    // Constructor
    public Armour_Item(Armour_ItemData baseData) : base(baseData)
    {
        armourData = baseData;
    }

    public PlayerHealth Owner => owner;
    private PlayerHealth owner;

    public bool IsEquipped => isEquipped;
    private bool isEquipped;

    #region Implemented Methods
    public void Equip(PlayerHealth playerHealth)
    {
        owner = playerHealth;
        isEquipped = true;

        OnItemChanged?.Invoke();
    }

    public void UnEquip()
    {
        isEquipped = false;

        OnItemChanged?.Invoke();

        owner = null;
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
