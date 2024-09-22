using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/Aid", order = 4)]
public class Aid_ItemData : ItemData_Base
{
    [field: Header("Consumption effects")]
    [field: SerializeField] public StatusEffectData_Base[] Effects { get; private set; }

    public override Item_Base CreateItemInstance()
    {
        return new Aid_Item(this);
    }
}
