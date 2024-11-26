using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkRigidbody), typeof(ItemNetworkBehaviour))]
public partial class Item : Interactable
{
	[Header("Item Properties")]
	public string itemName = "Item Name";
	public int stackSize = 1;
	public int value = 1;
	public Sprite itemSprite;
	public RuntimeAnimatorController animatorController;

	[Header("Item Dropping")]
	public Vector3 dropOffset = Vector3.zero;
	public Quaternion dropRotationOffset = Quaternion.identity;

	// System
	protected PlayerInventory ownerInventory;
	protected InventorySlot ownerSlot;

	public ItemNetworkBehaviour NetworkComponent { get; private set; }
	public bool HeldByLocalPlayer {get; private set;}

	protected virtual void Awake()
	{
		NetworkComponent = GetComponent<ItemNetworkBehaviour>();
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
		return new Interaction[1] {
			new() {
				prompt = $"Pick up {itemName}",
				sprite = itemSprite,
				interact = PickUpInteraction
			}
		};
	}

	public virtual string GetCustomStackText() { return null; }

	/// <summary>
	/// Called by player interaction system to request to pick up the item.
	/// </summary>
	protected virtual void PickUpInteraction(PlayerInteraction interactor)
	{
		PlayerInventory inv = interactor.GetComponentInParent<PlayerInventory>();

		if (!inv.HasSpaceForItem(this, out InventorySlot slot))
		{
			return;
		}

		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			// Host gets to just pick it up
			inv.AddItemToSlot(this, slot);

			NetworkManager.BroadcastMessage(new PickUpItemSuccessMessage(
				this,
				NetworkManager.GetLocalPlayer(),
				inv.IndexOfSlot(slot)));
		}
		else
		{
			// Client has to tell host they want to pick it up
			NetworkManager.SendToServer(
				new PickUpItemRequestMessage(this, inv.IndexOfSlot(slot)));
		}
	}

	#endregion

	#region Virtual Callbacks

	public virtual void OnAddToInventory(PlayerInventory inventory, InventorySlot slot)
	{
		ownerInventory = inventory;
		ownerSlot = slot;
	}

	public virtual void OnDrop()
	{
		ownerInventory = null;
		ownerSlot = null;
	}

	public virtual void OnEquipFirstPerson() { }
	public virtual void OnUnEquipFirstPerson() { }

	#endregion

	#region Accessors

	protected PlayerInventory GetInventoryComponent(PlayerInteraction interactor)
	{
		return interactor.transform.parent.GetComponent<PlayerInventory>();
	}

	public void SetHeldByLocalPlayer(bool b)
	{
		HeldByLocalPlayer = b;
	}

	#endregion
}
