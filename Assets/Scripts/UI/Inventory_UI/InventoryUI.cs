using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [field: Header("Display Types")]
    [field: SerializeField] public InventoryDisplay InventoryDisplay { get; private set; }

    public void Start()
    {
        InventoryDisplay.SetDisplayActive(false);
    }

    /// <summary>
    /// Opens the corresponding inventory display based on the type of inventory passed in
    /// </summary>
    public void DisplayInventory(InventoryComponent inventory)
    {
		if (inventory != null)
        {
            InventoryDisplay.AssignInventory(inventory);
            PlayerUIManager.SetActiveDisplay(InventoryDisplay);
        }
    }
}
