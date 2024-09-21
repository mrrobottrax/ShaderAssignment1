using UnityEngine;

[System.Serializable]
public class InventoryComponent : MonoBehaviour
{
    public Inventory Inventory => inventory;
    protected Inventory inventory;

    [SerializeField] private int _inventorySize;    

    protected virtual void Awake()
    {
        inventory = new Inventory(_inventorySize);
    }
}
