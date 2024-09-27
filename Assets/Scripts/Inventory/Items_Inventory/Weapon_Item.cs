
public class Weapon_Item : Item_Base, IEquippableItem
{
    private Weapon_ItemData baseData;

    // Constructor
    public Weapon_Item(Weapon_ItemData baseData) : base(baseData)
    {
        this.baseData = baseData;
    }

    #region Implemented Methods
    public void Equip(PlayerHealth playerHealth)
    {
        // Set the current view model
        playerHealth.GetViewModelManager().SetViewModel(this);
    }

    public void UnEquip(PlayerHealth playerHealth)
    {
        // Clear the current view model
        playerHealth.GetViewModelManager().ClearCurrentViewModel();
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
