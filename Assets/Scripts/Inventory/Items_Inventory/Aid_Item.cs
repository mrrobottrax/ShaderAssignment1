public class Aid_Item : Item_Base
{
    private Aid_ItemData aidData;

    // Constructor
    public Aid_Item(Aid_ItemData baseData) : base(baseData)
    {
        aidData = baseData;
    }


    /// <summary>
    /// Consumes the item by applying its effects to the specified entity and reducing the item count.
    /// </summary>
    /// <param name="entity_Base">The entity to which the item's effects will be applied.</param>
    public void ConsumeItem(Entity_Base entity_Base)
    {
        entity_Base.StatusEffectComponent.AddStatusEffect(aidData.Effects);
        RemoveAmount(1);
    }

    /// <summary>
    /// Returns the items data as aid item data.
    /// </summary>
    /// <returns>This items aid data</returns>
    public Aid_ItemData GetAidData()
    {
        return aidData;
    }

    #region Implemented Methods

    public override void ItemsSlotCleared(InventorySlot itemsSlot) { /*Nothing special for this item type*/ }
    #endregion
}
