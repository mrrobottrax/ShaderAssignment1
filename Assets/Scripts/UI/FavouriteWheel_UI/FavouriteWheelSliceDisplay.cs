using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FavouriteWheelSliceDisplay : MonoBehaviour
{
    [Header("Slot Visuals")]
    [SerializeField] private Image _slotImage;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TextMeshProUGUI _amountText;

    [SerializeField] private Color32 _normalColour;
    [SerializeField] private Color32 _selectedColour;

    [field: Header("Assigned Slot")]
    public InventorySlotPointer AssignedSlot { get; private set; }
    private bool isSelected;

    private void Awake()
    {
        _slotImage.color = _normalColour;
    }

    /// <summary>
    /// This method pairs this display slot to an inventory slot
    /// </summary>
    public void PairSlot(InventorySlotPointer slotAssigned)
    {
        // Pair slot
        AssignedSlot = slotAssigned;

        // Update visuals
        RefreshContents();
    }

    /// <summary>
    /// This method refreshes the slots visuals to match its contents
    /// </summary>
    public void RefreshContents()
    {
        // Ensure this display represents a slot
        if (AssignedSlot != null && AssignedSlot.GetPairedSlot() != null)
        {
            // Check for the slots item to match the display data
            if (AssignedSlot.GetPairedSlot().GetSlotsItem() != null)
            {
                // Cache the pointers paired slots item
                Item_Base slotsItem = AssignedSlot.GetPairedSlot().GetSlotsItem();

                // Enable icon
                _itemIcon.gameObject.SetActive(true);
                _itemIcon.sprite = slotsItem.GetItemData().ItemSprite;

                // Set amount
                _amountText.text = slotsItem.GetAmount().ToString();

                return;
            }
        }

        _itemIcon.gameObject.SetActive(false);
        _amountText.text = "";
    }

    public void SetSlotCurrent(bool selected)
    {
        isSelected = selected;

        _slotImage.color = !isSelected ? _normalColour : _selectedColour;
    }
 }
