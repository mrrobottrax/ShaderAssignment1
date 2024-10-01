using UnityEngine;

public abstract class Item : Interactable
{
	[Header("Item")]
	public string itemName = "Item Name";
	public Sprite itemSprite;
	public int stackSize = 1;
	public RuntimeAnimatorController animatorController;

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
