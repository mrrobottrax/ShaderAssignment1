
public class RangedWeapon_Item : Weapon_Item
{
    public RangedWeapon_Item(Weapon_ItemData baseData) : base(baseData)
    {

    }

    private int ammoInMag;

    #region Magazine Methods
    /// <summary>
    /// This method sets the amount of ammo present in the magazine;
    /// </summary>
    public void SetMagCount(int value)
    {
        ammoInMag = value;

        // Notify the slot that this item has been updated
        OnItemChanged();
    }

    public int GetMagCount()
    {
        return ammoInMag;
    }
    #endregion

    /// <summary>
    /// This method removes the weapons ammo type by a specified amount and updates the HUD UI if this item is equipped
    /// </summary>
    /// <param name="amount">Ammo consumed</param>
    public void ProjectileCreated(PlayerHealth castor, int amount)
    {
        RangedWeapon_ItemData rangedItemData = GetItemData() as RangedWeapon_ItemData;

        // Consume the item required to create the projectile
        switch (rangedItemData.AmmoType)
        {
            case RangedWeapon_ItemData.EConsumeOnFire.Ammo:
                SetMagCount(ammoInMag - amount);
                break;

            case RangedWeapon_ItemData.EConsumeOnFire.Self:

                // Remove an item from this stack
                RemoveAmount(amount);

                // Update the ammo display
                //if (IsEquipped)
                   // castor.PlayerUIManager.HUDManager.AmmoDisplay.SetDisplay(false, 0, 0, GetAmount());// Show how much is remaining in the stack

                break;
        }
    }
}
