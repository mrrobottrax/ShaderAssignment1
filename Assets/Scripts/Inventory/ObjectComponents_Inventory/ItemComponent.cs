using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof (Interactable))]
public class ItemComponent : MonoBehaviour, IInteraction
{
    [Header("Interaction Parameters")]
    private string interactionPrompt;
    public string InteractionPrompt => interactionPrompt;

    [SerializeField] private Sprite interactSprite;
    public Sprite InteractSprite => interactSprite;

    [SerializeField] private bool interactionEnabled = true;
    public bool IsInteractionEnabled => interactionEnabled;

    [Header("Item")]
    [SerializeField] private ItemData_Base _item;
    [SerializeField] private int _amount;

    public void Awake()
    {
        interactionPrompt = $"Pickup {_item.ItemName}";
    }

    public void Interact(Transform interactor)
    {
        // Try to get the iventory of the interactor
        interactor.TryGetComponent(out InventoryComponent inventory);

        // Try to add the item to the inventory
        bool slotFound = inventory.Inventory.AddItem(_item, _amount);

        // If the item was added, destroy the GameObject.
        if (slotFound)
            Destroy(gameObject);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }

    /// <summary>
    /// Sets the stack size for a physical item to add when picked up
    /// </summary>
    /// <param name="amount"></param>
    public void SetItemAmount(int amount)
    {
        _amount = amount;
    }
}
