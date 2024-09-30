using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Interactable))]
public class Item : Interactable
{
	[SerializeField] string itemName;
	[SerializeField] Sprite pickupIcon;

	Interaction[] interactions;

	private void Awake()
	{
		interactions = new Interaction[1] {
			new() {
				prompt = $"Pick up {itemName}",
				sprite = pickupIcon,
				interact = PickUp
			}
		};
	}

	void PickUp(PlayerInteraction interactor)
	{
		Debug.Log("Pick up");
	}

	public override Interaction[] GetInteractions()
	{
		return interactions;
	}
}
