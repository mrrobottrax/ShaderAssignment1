using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : Interactable
{
	[Header("Item Properties")]
	public string itemName = "Item Name";
	public int stackSize = 1;
	public Sprite itemSprite;
	public RuntimeAnimatorController animatorController;

	[Header("Item Dropping")]
	public Vector3 dropOffset = Vector3.zero;
	public Quaternion dropRotationOffset = Quaternion.identity;

	protected Interaction[] interactions;
	protected PlayerInventory ownerInventory;
	protected InventorySlot ownerSlot;

	protected void Awake()
	{
		interactions = new Interaction[1] {
			new() {
				prompt = $"Pick up {itemName}",
				sprite = itemSprite,
				interact = PickUp
			}
		};
	}

	protected void OnDestroy()
	{
		if (ownerInventory != null && ownerSlot != null && ownerSlot.items != null && ownerSlot.items.Contains(this))
		{
			ownerSlot.items.Pop();
			ownerSlot.ItemUpdate?.Invoke();
		}
	}

	#region Interaction

	public override Interaction[] GetInteractions()
	{
		return interactions;
	}

	public virtual string GetCustomStackText() { return null; }

	protected virtual void PickUp(PlayerInteraction interactor)
	{
		ownerInventory = GetInventoryComponent(interactor);
		ownerInventory.AddItem(this, out ownerSlot);
	}

	#endregion

	#region Virtual Callbacks

	public virtual void OnDrop() { }

	public virtual void OnEquip() { }
	public virtual void OnUnEquip() { }

	#endregion

	#region Accessors

	protected PlayerInventory GetInventoryComponent(PlayerInteraction interactor)
	{
		return interactor.transform.parent.GetComponent<PlayerInventory>();
	}

	public void SetOwnerInventory(PlayerInventory ownerInventory)
	{
		this.ownerInventory = ownerInventory;
	}

	public void SetOwnerSlot(InventorySlot slot)
	{
		ownerSlot = slot;
	}

	#endregion
}
