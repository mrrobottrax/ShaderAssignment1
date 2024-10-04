using System;
using UnityEngine;

public class PlayerViewmodelManager : MonoBehaviour
{
	[field: SerializeField] public Transform HandTransform { get; private set; }
	[SerializeField] PlayerInventory _inventory;
	[SerializeField] RuntimeAnimatorController _handsAnimatorController;

	public Animator Animator { get; private set; }

	private void Awake()
	{
		Animator = GetComponent<Animator>();

		_inventory.OnAddItem += OnAddItem;
		_inventory.OnDropItem += OnDropItem;
		_inventory.OnActiveSlotChange += OnActiveSlotChange;
	}

	private void OnDestroy()
	{
		_inventory.OnAddItem -= OnAddItem;
		_inventory.OnDropItem -= OnDropItem;
		_inventory.OnActiveSlotChange -= OnActiveSlotChange;
	}

	void PutItemInHands(Item item, bool putInHands)
	{
		item.interactionEnabled = !putInHands;

		// Follow hand
		if (putInHands)
		{
			item.transform.parent = HandTransform;
			item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		}
		else
		{
			item.transform.parent = null;
		}

		// Disable/enable physics
		Rigidbody[] rbs = item.GetComponentsInChildren<Rigidbody>();
		foreach (var rb in rbs)
		{
			rb.isKinematic = putInHands;
			rb.detectCollisions = !putInHands;
			rb.interpolation = putInHands ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
		}
	}

	void OnAddItem(Item item, InventorySlot slot)
	{
		PutItemInHands(item, true);

		EnableCorrectItems();
		SetController();

		if (slot == _inventory.GetActiveSlot())
			Animator.SetTrigger("Equip");
	}

	void OnDropItem(Item item, InventorySlot slot)
	{
		PutItemInHands(item, false);

		EnableCorrectItems();
		SetController();

		if (slot == _inventory.GetActiveSlot())
			Animator.SetTrigger("Equip");
	}

	void OnActiveSlotChange(InventorySlot prev, InventorySlot active)
	{
		EnableCorrectItems();
		SetController();

		// Don't transition from hands to hands
		if ((prev != null && prev.items != null && prev.items.Count > 0) ||
			(active.items != null && active.items.Count > 0))
		{
			Animator.SetTrigger("Equip");
		}
	}

	void SetController()
	{
		if (_inventory.GetActiveSlot().items == null || _inventory.GetActiveSlot().items.Count == 0)
		{
			Animator.runtimeAnimatorController = _handsAnimatorController;
		}
		else
		{
			Animator.runtimeAnimatorController = _inventory.GetActiveSlot().items.Peek().animatorController;
		}
	}

	void EnableCorrectItems()
	{
		foreach (var slot in _inventory.Slots)
		{
			if (slot.items != null && slot.items.TryPeek(out Item item))
			{
				item.gameObject.SetActive(slot == _inventory.GetActiveSlot());
			}
		}
	}

	public void Fire1_AnimationEvent()
	{
		if (_inventory.GetActiveSlot().items.Count == 0) return;

		if (_inventory.GetActiveSlot().items.Peek() is Weapon weapon)
            weapon.Fire1_AnimationEvent();
	}

	public void Fire2_AnimationEvent()
	{
		if (_inventory.GetActiveSlot().items.Count == 0) return;

		if (_inventory.GetActiveSlot().items.Peek() is Weapon weapon)
            weapon.Fire2_AnimationEvent();
	}
}
