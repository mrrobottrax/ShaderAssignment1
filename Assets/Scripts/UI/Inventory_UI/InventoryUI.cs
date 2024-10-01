using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
	[SerializeField] InventorySlotDisplay _slotPrefab;
	[SerializeField] RectTransform _slotHolder;

	private PlayerInventory inventory; // This can change when spectating players
	private List<InventorySlotDisplay> slots;

	private void Awake()
	{
		SetInventory(NetworkManager.GetLocalPlayer().GetComponent<PlayerInventory>());
	}

	private void OnDestroy()
	{
		if (inventory != null)
		{
			inventory.OnActiveSlotChange -= OnSlotChange;
		}
	}

	public void SetInventory(PlayerInventory inventory)
	{
		if (this.inventory != null)
		{
			inventory.OnActiveSlotChange -= OnSlotChange;
		}

		this.inventory = inventory;

		inventory.OnActiveSlotChange += OnSlotChange;

		// Clear all slots
		if (slots != null)
		{
			foreach (var slot in slots)
			{
				Destroy(slot.gameObject);
			}
		}
		slots = new List<InventorySlotDisplay>();

		// Instantiate slots
		int i = 0;
		while (slots.Count < inventory.Slots.Length)
		{
			InventorySlotDisplay slot = Instantiate(_slotPrefab, _slotHolder);
			slot.SetSlot(inventory.Slots[i]);
			slots.Add(slot);

			++i;
		}
	}

	void OnSlotChange(InventorySlot prev, InventorySlot active)
	{
		foreach(var slot in slots)
		{
			if (slot.GetSlot() == active)
			{
				slot.SetHighlight(true);
			}
			else
			{
				slot.SetHighlight(false);
			}
		}
	}
}
