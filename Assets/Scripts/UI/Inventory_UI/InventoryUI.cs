using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerUIManager _playerUIManager;

    [Header("Display Types")]
    [SerializeField] private ContainerInventoryDisplay _containerInventoryDisplay;
    [SerializeField] private PlayerInventoryDisplay _playerInventoryDisplay;

    public void Start()
    {
        _playerInventoryDisplay.SetDisplayActive(false);
    }

    /// <summary>
    /// Opens the corresponding inventory display based on the type of inventory passed in
    /// </summary>
    public void DisplayInventory(InventoryComponent inventory)
    {
        if(inventory is PlayerInventoryComponent)
        {
            _playerInventoryDisplay.AssignInventory(inventory);
            _playerUIManager.SetActiveDisplay(_playerInventoryDisplay);
        }
        else
        {
            //_containerInventoryDisplay.SetInventoryDisplayActive(true);
        }
    }

    public PlayerInventoryDisplay GetPlayerInventoryDisplay()
    {
        return _playerInventoryDisplay;
    }
}
