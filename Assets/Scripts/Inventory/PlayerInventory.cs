using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public delegate void OnAddItemDelegate(Item item, InventorySlot slot);
public delegate void OnSlotChangeDelegate(InventorySlot prev, InventorySlot active);
public delegate void OnToolbeltToggleDelegate(bool isOpen);

public class InventorySlot
{
	public Stack<Item> items = new();
	public Action ItemUpdate;
}

public class PlayerInventory : NetworkBehaviour, IInputHandler
{
	[SerializeField] int _inventorySize = 3;
	[SerializeField] float _dropForce = 1;

	[field: Header("Components")]
	[SerializeField] FirstPersonCamera _firstPersonCamera;
	[SerializeField] PlayerController _playerController;
	[field: SerializeField] Transform _dropPoint;

	// System
	private InventorySlot[] slots;
	public InventorySlot[] Slots => slots;

	public bool ToolbeltOpen { get; private set; } = false;

	// Delegates
	public OnAddItemDelegate OnAddItem;
	public OnAddItemDelegate OnDropItem;
	public OnSlotChangeDelegate OnActiveSlotChange;
	public OnToolbeltToggleDelegate OnToolbeltToggle;

	InventorySlot activeSlot;

	#region Initialization Methods

	private void Awake()
	{
		slots = new InventorySlot[_inventorySize];
		for (int i = 0; i < slots.Length; ++i)
		{
			slots[i] = new InventorySlot();
		}
	}

	private void Start()
	{
		SelectSlot(0);

		if (IsOwner)
		{
			SetControlsSubscription(true);
		}
	}

	private void OnDestroy()
	{
		if (IsOwner)
		{
			SetControlsSubscription(false);
		}
	}

	#endregion

	#region Input Methods

	public void Subscribe()
	{
		InputManager.Instance.Permanents.Inventory.performed += ToggleToolbelt;
		InputManager.Instance.Player.Drop.performed += Drop;

		// Hotbar
		InputManager.Instance.Player.Slot1.performed += Slot1;
		InputManager.Instance.Player.Slot2.performed += Slot2;
		InputManager.Instance.Player.Slot3.performed += Slot3;
		InputManager.Instance.Player.Slot4.performed += Slot4;
		InputManager.Instance.Player.Slot5.performed += Slot5;
		InputManager.Instance.Player.Slot6.performed += Slot6;
		InputManager.Instance.Player.Slot7.performed += Slot7;
		InputManager.Instance.Player.Slot8.performed += Slot8;
		InputManager.Instance.Player.Slot9.performed += Slot9;
	}

	public void Unsubscribe()
	{
		InputManager.Instance.Permanents.Inventory.performed -= ToggleToolbelt;
		InputManager.Instance.Player.Drop.performed -= Drop;

		// Hotbar
		InputManager.Instance.Player.Slot1.performed -= Slot1;
		InputManager.Instance.Player.Slot2.performed -= Slot2;
		InputManager.Instance.Player.Slot3.performed -= Slot3;
		InputManager.Instance.Player.Slot4.performed -= Slot4;
		InputManager.Instance.Player.Slot5.performed -= Slot5;
		InputManager.Instance.Player.Slot6.performed -= Slot6;
		InputManager.Instance.Player.Slot7.performed -= Slot7;
		InputManager.Instance.Player.Slot8.performed -= Slot8;
		InputManager.Instance.Player.Slot9.performed -= Slot9;
	}

	public void SetControlsSubscription(bool isInputEnabled)
	{
		if (isInputEnabled)
			Subscribe();
		else
			Unsubscribe();
	}

	public void SelectSlot(int slotIndex)
	{
		if (slotIndex >= slots.Length) return;

		InventorySlot prevSlot = activeSlot;
		activeSlot = slots[slotIndex];

		if (prevSlot == activeSlot) return;

		if (prevSlot != null && prevSlot.items.TryPeek(out var item))
		{
			item.OnUnEquip();
		}

		if (activeSlot.items.TryPeek(out var item1))
		{
			item1.OnEquip();
		}

		OnActiveSlotChange?.Invoke(prevSlot, activeSlot);
	}

	void Drop(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		if (activeSlot.items == null || activeSlot.items.Count == 0) return;

		Item active = GetActiveSlot().items.Peek();

		DropActiveItem(_dropForce, active.dropOffset, active.dropRotationOffset);
	}

	void ToggleToolbelt(InputAction.CallbackContext ctx)
	{
		_ = ctx;

		ToolbeltOpen = !ToolbeltOpen;

		_firstPersonCamera.EnableFirstPersonCamera(!ToolbeltOpen);

		if (ToolbeltOpen)
			InputManager.SetControlMode(InputManager.ControlType.UI);
		else
			InputManager.SetControlMode(InputManager.ControlType.Player);

		OnToolbeltToggle?.Invoke(ToolbeltOpen);
	}

	#region Hotbar Functions

	void Slot1(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(0);
	}

	void Slot2(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(1);
	}

	void Slot3(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(2);
	}

	void Slot4(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(3);
	}

	void Slot5(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(4);
	}

	void Slot6(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(5);
	}

	void Slot7(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(6);
	}

	void Slot8(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(7);
	}

	void Slot9(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		SelectSlot(8);
	}

	#endregion

	#endregion

	public void DropActiveItem(float dropForce, Vector3 dropPointOffset, Quaternion rotationOffset)
	{
		DropFromSlot(activeSlot, dropForce, dropPointOffset, rotationOffset);
	}

	private void DropFromSlot(InventorySlot slot, float dropForce, Vector3 dropPointOffset, Quaternion rotationOffset)
	{
		if (slot.items == null || slot.items.Count == 0) return;

		Item item = slot.items.Pop();

		OnDropItem?.Invoke(item, slot);
		slot.ItemUpdate?.Invoke();

		// Set velocity
		if (item.TryGetComponent(out Rigidbody rb))
		{
			rb.velocity = _firstPersonCamera.transform.forward * dropForce + _playerController.GetVelocity();

			// we need to set both this and the transform for stupid unity reasons
			rb.Move(_dropPoint.position + (_dropPoint.rotation * dropPointOffset), _dropPoint.rotation * rotationOffset);
		}

		item.transform.SetPositionAndRotation(_dropPoint.position + (_dropPoint.rotation * dropPointOffset), _dropPoint.rotation * rotationOffset);
		SceneManager.MoveGameObjectToScene(item.gameObject, SceneManager.GetActiveScene());

		item.OnUnEquip();

		item.SetOwnerInventory(null);
		item.SetOwnerSlot(null);

		item.OnDrop();
	}

	void AddItemToSlot(Item item, InventorySlot slot)
	{
		slot.items ??= new Stack<Item>();

		// Disable previous gameobject so we don't see multiple
		if (slot.items.TryPeek(out Item prevItem))
		{
			prevItem.gameObject.SetActive(false);
		}

		slot.items.Push(item);
		slot.ItemUpdate?.Invoke();

		item.SetOwnerInventory(this);
		item.SetOwnerSlot(slot);

		if (activeSlot == slot)
		{
			item.OnEquip();
		}

		OnAddItem?.Invoke(item, slot);
		slot.ItemUpdate?.Invoke();
	}

	public delegate bool ItemSearchDelegate(Item item);
	public Item FindItemWhere(ItemSearchDelegate searchFunc)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].items != null &&
				slots[i].items.Count > 0 &&
				searchFunc.Invoke(slots[i].items.Peek())
				)
			{
				return slots[i].items.Peek();
			}
		}

		return null;
	}

	public bool AddItem(Item item, out InventorySlot slot, bool allowAutoSelect = true)
	{
		// Try first slot with same item type
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].items != null &&
				slots[i].items.Count > 0 &&
				slots[i].items.Peek().itemName == item.itemName &&
				slots[i].items.Count < item.stackSize
				)
			{
				AddItemToSlot(item, slots[i]);
				slot = slots[i];
				return true;
			}
		}

		// Try selected slot
		if (activeSlot.items == null || activeSlot.items.Count == 0 ||
			(activeSlot.items.Peek().GetType() == item.GetType() && activeSlot.items.Count < item.stackSize))
		{
			AddItemToSlot(item, activeSlot);
			slot = activeSlot;
			return true;
		}

		// Try first free slot
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].items == null || slots[i].items.Count == 0)
			{
				AddItemToSlot(item, slots[i]);
				if (allowAutoSelect)
					SelectSlot(i);
				slot = slots[i];
				return true;
			}
		}

		slot = null;
		return false;
	}

	public InventorySlot GetActiveSlot()
	{
		return activeSlot;
	}

	public FirstPersonCamera GetCamera()
	{
		return _firstPersonCamera;
	}
}
