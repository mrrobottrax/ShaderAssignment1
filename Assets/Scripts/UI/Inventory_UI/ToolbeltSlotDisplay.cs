using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToolbeltSlotDisplay : MonoBehaviour
{
    [Header("Slot Visuals")]
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Button _slotsButton;

    [Header("Item State Visuals")]
    [SerializeField] private Image _slotImage;
    [SerializeField] private Color32 _defaultColour;
    [SerializeField] private Color32 _highlightedColour;

    [Header("Assigned Slot")]
    public InventorySlot AssignedSlot;
    private ToolbeltDisplay toolbeltDisplay;

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
            toolbeltDisplay?.SlotSelected(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Ensure the button can be interacted with before updating anything
        if (_slotsButton.interactable)
            toolbeltDisplay?.SlotSelected(this);
    }

    public void OnSlotClick()
    {
        toolbeltDisplay?.SlotPressed(this);
    }
    #endregion

    /// <summary>
    /// This method pairs this display slot to an inventory slot
    /// </summary>
    public void PairSlot(InventorySlot slotAssigned, ToolbeltDisplay toolbeltDisplay)
    {
        // Pair slot
        AssignedSlot = slotAssigned;

        // Cache the inventory display this slot is apart of
       this.toolbeltDisplay = toolbeltDisplay;

        // Update visuals
        RefreshContents();
    }

    /// <summary>
    /// This method refreshes the slots visuals to match its contents
    /// </summary>
    private void RefreshContents()
    {
        // Ensure this display represents a slot
        if (AssignedSlot != null)
        {
            // Check for the slots item to match the display data
            if (AssignedSlot.GetSlotsItem() != null)
            {
                Item_Base slotsItem = AssignedSlot.GetSlotsItem();

                if(_amountText != null)
                {
                    if (AssignedSlot.GetSlotsItem().GetAmount() > 1)
                        _amountText.text = AssignedSlot.GetSlotsItem().GetAmount().ToString();
                    else _amountText.text = "";
                }

                _itemImage.sprite = slotsItem.GetItemData().ItemSprite;
                _itemImage.gameObject.SetActive(true);

                _slotImage.color = (slotsItem is IEquippableItem equipableItem && equipableItem.IsEquipped) ? _highlightedColour : _defaultColour;

                // Allow interactions as the slot is filled
                SetDisplayInteractable(true);
                return;
            }
        }

        SetDisplayInteractable(false);

        if (_amountText != null)
            _amountText.text = "";

        _itemImage.gameObject.SetActive(false);
        _itemImage.sprite = null;
    }

    #region Helper Methods

    public void SetDisplayInteractable(bool enable)
    {
        _slotsButton.interactable = enable;
    }
    #endregion
}
