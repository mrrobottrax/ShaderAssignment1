using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class InventorySlotDisplay : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Header("Slot Visuals")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private Button _slotsButton;

    [Header("Item State Visuals")]
    [SerializeField] private GameObject _itemEquippedElement;

    [Header("Assigned Slot")]
    public InventorySlot AssignedSlot;
    private InventoryDisplay inventoryDisplay;

    [Header("System")]
    private int displayIndex;// The index of this display slot in the display slot list
    private int inventoryIndex;// The index of the inventory slot this display represents

    #region Button Listeners
    public void OnEnable()
    {
        _slotsButton?.onClick.AddListener(OnSlotClick);
    }

    public void OnDisable()
    {
        _slotsButton?.onClick.RemoveAllListeners();
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        // Ensure the button can be interacted with before updating anything
        if (_slotsButton.interactable)
            inventoryDisplay?.SlotSelected(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Ensure the button can be interacted with before updating anything
        if (_slotsButton.interactable)
            inventoryDisplay?.SlotSelected(this);
    }

    public void OnSlotClick()
    {
        inventoryDisplay?.SlotPressed(this);
    }
    #endregion

    /// <summary>
    /// This method pairs this display slot to an inventory slot
    /// </summary>
    public void PairSlot(int displayIndex, int inventoryIndex, InventorySlot slotAssigned, InventoryDisplay InventoryDisplay)
    {
        // Assign index values
        this.displayIndex = displayIndex;
        this.inventoryIndex = inventoryIndex;

        // Pair slot
        AssignedSlot = slotAssigned;

        // Cache the inventory display this slot is apart of
        inventoryDisplay = InventoryDisplay;

        // Update visuals
        RefreshContents();
    }

    /// <summary>
    /// This method refreshes the slots visuals to match its contents
    /// </summary>
    private void RefreshContents()
    {
        // Ensure this display represents a slot
        if(AssignedSlot != null)
        {
            // Check for the slots item to match the display data
            if (AssignedSlot.GetSlotsItem() != null)
            {
                Item_Base slotsItem = AssignedSlot.GetSlotsItem();

                // Update slot text
                if (_nameText != null)
                    _nameText.text = slotsItem.GetItemData().ItemName;

                if (_amountText != null)
                    _amountText.text = AssignedSlot.GetSlotsItem().GetAmount().ToString();

                if (_valueText != null)
                    _valueText.text = slotsItem.GetItemData().ItemBaseValue.ToString();

                // Check if the item is either favourited or equipped
                _itemEquippedElement?.SetActive(slotsItem is IEquippableItem equipableItem && equipableItem.IsEquipped);

                // Allow interactions as the slot is filled
                SetDisplayInteractable(true);
                return;
            }
        }

        SetDisplayInteractable(false);

        if (_nameText != null)
            _nameText.text = "";

        if (_amountText != null)
            _amountText.text = "";

        if (_valueText != null)
            _valueText.text = "";

        _itemEquippedElement?.SetActive(false);
    }

    #region Helper Methods

    public void SetDisplayInteractable(bool enable)
    {
        _slotsButton.interactable = enable;
    }

    /// <summary>
    /// Returns which display slot this slot display represents
    /// </summary>
    public int GetDisplayIndex()
    {
       return displayIndex;
    }

    /// <summary>
    /// Returns which inventory slot this slot display represents
    /// </summary>
    public int GetInventoryIndex()
    {
        return inventoryIndex;
    }
    #endregion
}
