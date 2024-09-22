public class Aid_Item : Item_Base, IFavouritableItem
{
    private Aid_ItemData aidData;

    // Constructor
    public Aid_Item(Aid_ItemData baseData) : base(baseData)
    {
        aidData = baseData;
    }

    public bool IsItemFavourited => isItemFavourited;
    private bool isItemFavourited;

    public int FavouriteSlotPointerID => favouriteSlotPointerID;
    private int favouriteSlotPointerID;

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

    public override void SlotCleared(InventorySlot itemsSlot) { /*Nothing special for this item type*/ }

    public void FavouriteItem(int favouriteSlotID)
    {
        isItemFavourited = true;

        // Store pointer ref
        favouriteSlotPointerID = favouriteSlotID;
    }

    public void UnfavouriteItem(out int favouriteSlotID)
    {
        favouriteSlotID = favouriteSlotPointerID;

        isItemFavourited = false;

        // Reset the pointer ID
        favouriteSlotPointerID = 0;
    }

    public void UseFavouritedItem(PlayerHealth playerHealth)
    {
        ConsumeItem(playerHealth);
    }
    #endregion
}
