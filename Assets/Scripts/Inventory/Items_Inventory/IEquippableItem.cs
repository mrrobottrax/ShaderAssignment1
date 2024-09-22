public interface IEquippableItem
{
    public PlayerHealth Owner { get; }
    public bool IsEquipped { get; }
    /// <summary>
    /// Implemented by the child class. This method should determine the logic executed when trying to equip an item of a type.
    /// </summary>
    public abstract void Equip(PlayerHealth playerHealth);

    /// <summary>
    /// Implemented by the child class. This method should determine the logic executed when trying to unequip an item of a type.
    /// </summary>
    public abstract void UnEquip();
}
