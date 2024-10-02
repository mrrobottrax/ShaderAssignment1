using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : Interactable
{
	[Header("Item")]
	public string itemName = "Item Name";
	public int stackSize = 1;
	public Sprite itemSprite;
	public RuntimeAnimatorController animatorController;

	[Header("Item Dropping")]
	public Vector3 dropOffset = Vector3.zero;
	public Quaternion dropRotationOffset = Quaternion.identity;

	protected Interaction[] interactions;

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

	protected virtual void PickUp(PlayerInteraction interactor)
	{
		interactor.GetComponent<PlayerInventory>().AddItem(this);
	}

	public virtual void Drop() { }

	public override Interaction[] GetInteractions()
	{
		return interactions;
	}
}
