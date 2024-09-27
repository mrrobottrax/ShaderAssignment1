using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerUIManager _playerUIManager;

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
        if(inventory is InventoryComponent)
        {
            InventoryDisplay.AssignInventory(inventory);
            _playerUIManager.SetActiveDisplay(InventoryDisplay);
        }
    }
}
