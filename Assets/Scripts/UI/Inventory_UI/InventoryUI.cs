using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class InventoryUI : MonoBehaviour
{
	[SerializeField] InventorySlotDisplay _hotbarSlotPrefab;
	[SerializeField] RectTransform _hotbarHolder;

	[Space]

	[SerializeField] ToolbeltSlotDisplay _toolbeltSlotPrefab;
	[SerializeField] RectTransform _toolbeltHolder;

	private PlayerInventory inventory; // This can change when spectating players
	private List<InventorySlotDisplay> hotbarSlots;
	private List<ToolbeltSlotDisplay> toolbeltSlots;
	private List<Vector2> toolBeltSlotPositions;

	private void Awake()
	{
		SetInventory(NetworkManager.GetLocalPlayer().GetComponent<PlayerInventory>());
		OnToolbeltToggle(false);
	}

	private void OnDestroy()
	{
		UnsubscribeCallbacks();
	}

	// Set the inventory to show
	public void SetInventory(PlayerInventory inventory)
	{
		UnsubscribeCallbacks();

		this.inventory = inventory;

		inventory.OnActiveSlotChange += OnSlotChange;
		inventory.OnToolbeltToggle += OnToolbeltToggle;

		// Hotbar
		{
			// Clear all slots
			if (hotbarSlots != null)
			{
				foreach (var slot in hotbarSlots)
				{
					Destroy(slot.gameObject);
				}
			}
			hotbarSlots = new List<InventorySlotDisplay>();

			// Instantiate hotbar slots
			int i = 0;
			while (hotbarSlots.Count < inventory.Slots.Length)
			{
				InventorySlotDisplay slot = Instantiate(_hotbarSlotPrefab, _hotbarHolder);
				slot.SetSlot(inventory.Slots[i]);
				hotbarSlots.Add(slot);

				++i;
			}
		}

		// Toolbelt
		{
			// Clear all slots
			if (toolbeltSlots != null)
			{
				foreach (var slot in hotbarSlots)
				{
					Destroy(slot.gameObject);
				}
			}
			toolbeltSlots = new List<ToolbeltSlotDisplay>();

			// Instantiate slots
			int i = 0;
			while (toolbeltSlots.Count < inventory.Slots.Length)
			{
				ToolbeltSlotDisplay slot = Instantiate(_toolbeltSlotPrefab, _toolbeltHolder);

				slot.OnDragAction += OnToolbeltSlotDrag;
				slot.disableLerp = true;
				slot.SetSlot(inventory.Slots[i]);

				toolbeltSlots.Add(slot);
				++i;
			}
		}
	}

	void UnsubscribeCallbacks()
	{
		if (inventory != null)
		{
			inventory.OnActiveSlotChange -= OnSlotChange;
			inventory.OnToolbeltToggle -= OnToolbeltToggle;
		}
	}

	void OnSlotChange(InventorySlot prev, InventorySlot active)
	{
		foreach (var slot in hotbarSlots)
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

	void OnToolbeltToggle(bool isOpen)
	{
		if (isOpen)
		{
			_hotbarHolder.gameObject.SetActive(false);
			_toolbeltHolder.gameObject.SetActive(true);

			StartCoroutine(CacheToolbeltPositionsNextFrame());
		}
		else
		{
			_hotbarHolder.gameObject.SetActive(true);
			_toolbeltHolder.gameObject.SetActive(false);

			foreach (var slot in toolbeltSlots)
			{
				PointerEventData eventData = new(EventSystem.current);
				slot.OnPointerExit(eventData);
			}
		}
	}

	IEnumerator CacheToolbeltPositionsNextFrame()
	{
		yield return null;

		// Cache positions
		toolBeltSlotPositions = new List<Vector2>();
		foreach (var slot in toolbeltSlots)
		{
			toolBeltSlotPositions.Add(slot.transform.position);
			slot.goalPosition = slot.transform.position;
			slot.disableLerp = false;
		}
	}

	void OnToolbeltSlotDrag()
	{
		// Check that each pair is in the correct order
		int sourceIndex = -1;
		int destIndex = -1;
		for (int i = 1; i < toolbeltSlots.Count; i++)
		{
			int j = i - 1;

			if (toolbeltSlots[i].transform.position.x < toolbeltSlots[j].transform.position.x)
			{
				sourceIndex = i;
				destIndex = j;
				break;
			}
		}

		if (sourceIndex == -1 || destIndex == -1) return;

		// Swap the items in the slots
		(toolbeltSlots[sourceIndex].GetSlot().items, toolbeltSlots[destIndex].GetSlot().items)
			= (toolbeltSlots[destIndex].GetSlot().items, toolbeltSlots[sourceIndex].GetSlot().items);

		// Swap the indices of the slots
		(toolbeltSlots[sourceIndex], toolbeltSlots[destIndex])
			= (toolbeltSlots[destIndex], toolbeltSlots[sourceIndex]);

		// Update slots
		for (int i = 0; i < toolbeltSlots.Count; ++i)
		{
			toolbeltSlots[i].SetSlot(inventory.Slots[i]);
			inventory.Slots[i].itemUpdate?.Invoke();
		}

		if (inventory.Slots[destIndex] == inventory.GetActiveSlot())
		{
			inventory.SelectSlot(sourceIndex);
		}
		else if (inventory.Slots[sourceIndex] == inventory.GetActiveSlot())
		{
			inventory.SelectSlot(destIndex);
		}

		// Swap animation
		toolbeltSlots[sourceIndex].MoveTo(toolBeltSlotPositions[sourceIndex]);
		toolbeltSlots[destIndex].MoveTo(toolBeltSlotPositions[destIndex]);
	}
}
