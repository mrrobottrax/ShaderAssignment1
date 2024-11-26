using System;
using System.Collections.Generic;
using System.Linq;
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
	[SerializeField] private int _inventorySize = 3;
	[SerializeField] private float _dropForce = 1;

	[field: Header("Components")]
	[SerializeField] private FirstPersonCamera _firstPersonCamera;
	[SerializeField] private PlayerController _playerController;
	public Transform DropPoint;
	private PlayerStats playerStats;

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
		playerStats = GetComponent<PlayerStats>();

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
		InputManager.Instance.Permanents.QuickDrop.performed += Drop;

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
		InputManager.Instance.Permanents.QuickDrop.performed -= Drop;

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

	public void SelectSlot(InventorySlot slot)
	{
		for (int i = 0; i < slots.Length; ++i)
		{
			if (slots[i] == slot)
			{
				SelectSlot(i);
				return;
			}
		}
	}

	public void SelectSlot(int slotIndex)
	{
		if (!playerStats.IsAbleToAttack)
			return;


		if (slotIndex >= slots.Length) return;

		InventorySlot prevSlot = activeSlot;
		activeSlot = slots[slotIndex];

		if (prevSlot == activeSlot) return;

		if (prevSlot != null && prevSlot.items.TryPeek(out var item))
		{
			if (item.HeldByLocalPlayer)
				item.OnUnEquipFirstPerson();
		}

		if (activeSlot.items.TryPeek(out var item1))
		{
			if (item1.HeldByLocalPlayer)
				item1.OnEquipFirstPerson();
		}

		OnActiveSlotChange?.Invoke(prevSlot, activeSlot);
	}

	void Drop(InputAction.CallbackContext ctx)
	{
		_ = ctx;
		if (activeSlot.items == null || activeSlot.items.Count == 0) return;

		Item active = GetActiveSlot().items.Peek();

		TryDropActiveItem(_dropForce, active.dropOffset, active.dropRotationOffset);
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

	public void TryDropActiveItem(float dropForce, Vector3 dropPointOffset, Quaternion rotationOffset)
	{
		CalcDrop(dropForce, dropPointOffset, rotationOffset, out Vector3 dropVel, out Vector3 dropPos, out Quaternion dropRot);

		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			DropFromSlot(activeSlot, dropPos, dropVel, dropRot);
			NetworkManager.BroadcastMessage(new Item.DropItemSuccessMessage(this, dropPos, dropVel, dropRot, GetActiveSlotIndex()));
		}
		else
		{
			NetworkManager.SendToServer(new Item.DropItemRequestMessage(this, dropPos, dropVel, dropRot));
		}
	}

	void CalcDrop(float dropForce, Vector3 dropPointOffset, Quaternion rotationOffset, out Vector3 dropVel, out Vector3 dropPos, out Quaternion dropRot)
	{
		dropVel = _firstPersonCamera.transform.forward * dropForce + _playerController.GetVelocity();
		dropPos = DropPoint.position + (DropPoint.rotation * dropPointOffset);
		dropRot = DropPoint.rotation * rotationOffset;
	}

	public void DropFromSlot(InventorySlot slot, Vector3 dropPos, Vector3 dropVel, Quaternion dropRot)
	{
		if (slot.items == null || slot.items.Count == 0) return;

		Item item = slot.items.Pop();

		item.SetHeldByLocalPlayer(false);

		OnDropItem?.Invoke(item, slot);
		slot.ItemUpdate?.Invoke();

		// Set velocity
		if (item.TryGetComponent(out Rigidbody rb))
		{
			if (!rb.isKinematic)
				rb.velocity = dropVel;

			// we need to set both this and the transform for stupid unity reasons
			rb.Move(dropPos, dropRot);
		}

		item.transform.SetPositionAndRotation(dropPos, dropRot);
		if (item.transform.parent == null)
			SceneManager.MoveGameObjectToScene(item.gameObject, SceneManager.GetActiveScene());

		if (item.HeldByLocalPlayer)
			item.OnUnEquipFirstPerson();

		item.OnDrop();
	}

	public void AddItemToSlot(Item item, InventorySlot slot)
	{
		slot.items ??= new Stack<Item>();

		item.SetHeldByLocalPlayer(NetObj == NetworkManager.GetLocalPlayer());

		// Disable previous gameobject so we don't see multiple
		if (slot.items.TryPeek(out Item prevItem))
		{
			prevItem.gameObject.SetActive(false);
		}

		slot.items.Push(item);
		slot.ItemUpdate?.Invoke();

		item.OnAddToInventory(this, slot);

		if (activeSlot == slot)
		{
			item.OnEquipFirstPerson();
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

	public bool HasSpaceForItem(Item item, out InventorySlot slot)
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
				slot = slots[i];
				return true;
			}
		}

		// Try selected slot
		if (activeSlot.items == null || activeSlot.items.Count == 0 ||
			(activeSlot.items.Peek().GetType() == item.GetType() && activeSlot.items.Count < item.stackSize))
		{
			slot = activeSlot;
			return true;
		}

		// Try first free slot
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].items == null || slots[i].items.Count == 0)
			{
				slot = slots[i];
				return true;
			}
		}

		slot = null;
		return false;
	}

	public bool TryAddItem(Item item, out InventorySlot slot, bool allowAutoSelect = true)
	{
		if (!HasSpaceForItem(item, out slot))
		{
			return false;
		}

		// Auto select empty slots
		if (slot.items == null || slot.items.Count == 0)
		{
			AddItemToSlot(item, slot);
			if (allowAutoSelect)
				SelectSlot(slot);

			return true;
		}

		AddItemToSlot(item, slot);
		return true;
	}

	public InventorySlot GetActiveSlot()
	{
		return activeSlot;
	}

	public int IndexOfSlot(InventorySlot slot)
	{
		for (int i = 0; i < slots.Length; ++i)
		{
			if (slots[i] == slot)
				return i;
		}

		return -1;
	}

	public int GetActiveSlotIndex()
	{
		return IndexOfSlot(activeSlot);
	}

	public FirstPersonCamera GetCamera()
	{
		return _firstPersonCamera;
	}
}
