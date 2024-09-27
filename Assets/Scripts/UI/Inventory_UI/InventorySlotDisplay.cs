using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotDisplay : MonoBehaviour
{
    [Header("Slot Visuals")]
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Button _slotsButton;

    [Header("Item State Visuals")]
    [SerializeField] private Image _slotImage;
    [SerializeField] private Color32 _defaultColour;
    [SerializeField] private Color32 _highlightedColour;
    [SerializeField] private Color32 _selectedColour;

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

    public void OnSlotClick()
    {
        inventoryDisplay?.SlotPressed(this);
    }

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

    #region Helper Methods

    /// <summary>
    /// This method refreshes the slots visuals to match its contents
    /// </summary>
    private void RefreshContents()
    {
        // Display if a slot is the highlighted slot
        if (playerInventoryComponent?.HeldItemSlot != null)
            _slotImage.color = (playerInventoryComponent.HeldItemSlot == AssignedSlot) ? _selectedColour : _defaultColour;

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
