using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotDisplay : MonoBehaviour
{
	[Header("Slot Visuals")]
	[SerializeField] private TextMeshProUGUI _amountText;
	[SerializeField] private Image _itemImage;

	[Header("Item State Visuals")]
	[SerializeField] private Image _slotImage;
	[SerializeField] private Color32 _defaultColour;
	[SerializeField] private Color32 _selectedColour; // Player is using this slot
	[SerializeField] private Image _highlightOutline;

	InventorySlot assignedSlot;

	protected void Awake()
	{
		SetHighlight(false);
		ItemUpdate();
	}

	private void OnDestroy()
	{
		if (assignedSlot != null)
			assignedSlot.itemUpdate -= ItemUpdate;
	}

	public void SetSlot(InventorySlot slot)
	{
		if (assignedSlot != null)
			assignedSlot.itemUpdate -= ItemUpdate;

		assignedSlot = slot;
		assignedSlot.itemUpdate += ItemUpdate;
	}

	public InventorySlot GetSlot()
	{
		return assignedSlot;
	}

	void ItemUpdate()
	{
		if (assignedSlot == null || !assignedSlot.items.TryPeek(out Item item))
		{
			_amountText.text = null;
			_itemImage.sprite = null;

			_itemImage.gameObject.SetActive(false);
			return;
		}

		if (item.stackSize == 1)
		{
			_amountText.text = null;
		}
		else
		{
			_amountText.text = assignedSlot.items.Count.ToString();
		}

		_itemImage.sprite = item.itemSprite;
		_itemImage.gameObject.SetActive(true);
	}

	public void SetHighlight(bool hightlight)
	{
		_highlightOutline.enabled = hightlight;

		_slotImage.color = hightlight ? _selectedColour : _defaultColour;
	}
}
