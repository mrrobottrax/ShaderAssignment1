using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/Armor", order = 0)]
public class Armour_ItemData : ItemData_Base
{
    [field: Header("Armour Type")]
    [field: SerializeField] public ArmorType ArmorsType { get; private set; }

    [System.Serializable]
    public enum ArmorType
    {
        HeadPiece,
        ChestPiece,
        Legs,
        Feet
    }

    [field: Header("Armour Stats")]
    [field: SerializeField] public int Defence { get; private set; }

    public override Item_Base CreateItemInstance()
    {
        return new Armour_Item(this);
    }
}
