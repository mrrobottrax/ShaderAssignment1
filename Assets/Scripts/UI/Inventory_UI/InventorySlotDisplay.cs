using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotDisplay : MonoBehaviour, IPointerEnterHandler
{
    [Header("Slot Visuals")]
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Button _slotsButton;

    [Header("Item State Visuals")]
    [SerializeField] private Image _slotImage;
    [SerializeField] private Color32 _defaultColour;
    [SerializeField] private Color32 _selectedColour;// Player is using this slot
    [SerializeField] private Image _highlightOutline;

    [field: Header("Assigned Slot")]
    public InventorySlot AssignedSlot { get; private set; }
    private InventoryDisplay inventoryDisplay;
    private PlayerInventoryComponent playerInventoryComponent;


    #region Unity Callbacks

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

    #endregion

    #region Display Slot Paring Methods

    /// <summary>
    /// This method pairs this display slot to an inventory slot
    /// </summary>
    public void PairSlotToDisplay(int indexVal, Inventory inventory, InventoryDisplay inventoryDisplay)
    {
        // Pair slot
        AssignedSlot = inventory.Slots[indexVal];

        // Cache the inventory display this slot is apart of
        this.inventoryDisplay = inventoryDisplay;

        if (inventoryDisplay.PairedInventoryComponent is PlayerInventoryComponent inventoryComponent)
            playerInventoryComponent = inventoryComponent;

        // Update visuals
        RefreshContents();
    }

    /// <summary>
    /// Removes a display slots pairing to an inventory slot
    /// </summary>
    public void ClearDisplayPairing()
    {
        // Remove slot pairing
        AssignedSlot = null;

        // Update visuals
        RefreshContents();
    }

    #endregion

    #region Helper Methods

    public void OnSlotClick()
    {
        inventoryDisplay?.SlotPressed(this);
    }

    /// <summary>
    /// Toggles the slot display outline based on if a slot is selected or not.
    /// </summary>
    public void SetDisplaySelected(bool isHighlighted)
    {
        _highlightOutline.gameObject.SetActive(isHighlighted);
    }

    /// <summary>
    /// Changes the slot display colour based on if a slot is highlighted or not.
    /// </summary>
    public void SetDisplayHighlighted(bool isSelected)
    {
        _slotImage.color = isSelected ? _selectedColour : _defaultColour;
    }

    /// <summary>
    /// This method refreshes the slots visuals to match its contents
    /// </summary>
    private void RefreshContents()
    {
        // Display if a slot is the highlighted slot
        SetDisplaySelected(playerInventoryComponent?.HeldItemSlot == AssignedSlot);

        // Check for the slots item to match the display data
        if (AssignedSlot?.GetSlotsItem() != null)
        {
            Item_Base slotsItem = AssignedSlot.GetSlotsItem();

            if (_amountText != null)
            {
                if (AssignedSlot.GetSlotsItem().GetAmount() > 1)
                    _amountText.text = AssignedSlot.GetSlotsItem().GetAmount().ToString();
                else _amountText.text = "";
            }

            _itemImage.sprite = slotsItem.GetItemData().ItemSprite;
            _itemImage.gameObject.SetActive(true);
        }
        else
        {
            if (_amountText != null)
                _amountText.text = "";

            _itemImage.gameObject.SetActive(false);
            _itemImage.sprite = null;
        }
    }

    public void SetDisplayInteractable(bool enable)
    {
        _slotsButton.interactable = enable;
    }
    #endregion
}
