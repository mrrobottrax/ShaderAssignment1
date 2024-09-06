using UnityEngine;

public class Weapon_Item : Item_Base, IEquippableItem, IFavouritableItem
{
    private Weapon_ItemData baseData;

    // Constructor
    public Weapon_Item(Weapon_ItemData baseData) : base(baseData)
    {
        this.baseData = baseData;
    }

    public bool IsEquipped => isEquipped;
    private bool isEquipped;

    public bool IsItemFavourited => isItemFavourited;
    private bool isItemFavourited;

    public int FavouriteSlotPointerID => favouriteSlotPointerID;
    private int favouriteSlotPointerID;

    #region Implemented Methods
    public void Equip()
    {
        isEquipped = true;

        // Set the current view model
        Player.Instance.GetViewModelManager().SetViewModel(this);
    }

    public void UnEquip()
    {
        isEquipped = false;

        // Clear the current view model
        Player.Instance.GetViewModelManager().ClearCurrentViewModel();
    }

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

    public void UseFavouritedItem()
    {
        PlayerInventoryComponent inventoryComponent = Player.Instance.GetPlayerInventory();
        inventoryComponent.EquipItem(inventoryComponent.GetFavouritePointer(favouriteSlotPointerID).GetPairedSlot());
    }

    public override void SlotCleared(InventorySlot itemsSlot)
    {
        // Unequip this item if it is equipped
        if (isEquipped)
        {
            PlayerInventoryComponent inventoryComponent = Player.Instance.GetPlayerInventory();
            inventoryComponent.UnequipItem(itemsSlot);
        }
    }
    #endregion

    /// <summary>
    /// Returns the items data as weapon item data.
    /// </summary>
    /// <returns>This items weapon data</returns>
    public Weapon_ItemData GetWeaponData()
    {
        return baseData;
    }
}
