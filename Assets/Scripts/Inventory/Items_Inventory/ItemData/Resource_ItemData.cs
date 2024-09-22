using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/Resource", order = 1)]
public class Resource_ItemData : ItemData_Base
{
    public override Item_Base CreateItemInstance()
    {
        return new Misc_Item(this);
    }
}
