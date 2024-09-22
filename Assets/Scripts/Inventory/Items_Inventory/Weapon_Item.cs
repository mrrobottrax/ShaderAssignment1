using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon_Item : Item_Base, IEquippableItem
{
    private Weapon_ItemData baseData;

    // Constructor
    public Weapon_Item(Weapon_ItemData baseData) : base(baseData)
    {
        this.baseData = baseData;
    }

    public PlayerHealth Owner => owner;
    private PlayerHealth owner;

    public bool IsEquipped => isEquipped;
    private bool isEquipped;

    public bool IsItemFavourited => isItemFavourited;
    private bool isItemFavourited;

    public int FavouriteSlotPointerID => favouriteSlotPointerID;
    private int favouriteSlotPointerID;

    #region Implemented Methods
    public void Equip(PlayerHealth playerHealth)
    {
        owner = playerHealth;
        isEquipped = true;
        OnItemChanged?.Invoke();

        // Set the current view model
        playerHealth.GetViewModelManager().SetViewModel(this);
    }

    public void UnEquip()
    {
        isEquipped = false;
        OnItemChanged?.Invoke();

        // Clear the current view model
        owner.GetViewModelManager().ClearCurrentViewModel();
        owner = null;
    }

    public override void SlotCleared(InventorySlot itemsSlot)
    {
        // Unequip this item if it is equipped
        if (isEquipped)
        {
            PlayerInventoryComponent inventoryComponent = owner.GetPlayerInventory();
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
