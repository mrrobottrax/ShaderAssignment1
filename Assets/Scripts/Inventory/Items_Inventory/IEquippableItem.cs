public interface IEquippableItem
{
    public bool IsEquipped { get; }
    /// <summary>
    /// Implemented by the child class. This method should determine the logic executed when trying to equip an item of a type.
    /// </summary>
    public abstract void Equip();

    /// <summary>
    /// Implemented by the child class. This method should determine the logic executed when trying to unequip an item of a type.
    /// </summary>
    public abstract void UnEquip();
}
