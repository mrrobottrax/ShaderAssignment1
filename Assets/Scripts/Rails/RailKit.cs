using UnityEngine;

public class RailKit : Item
{
	[Header("Rail Kit")]
	[SerializeField] float m_maxMetresStack = 50;
	[SerializeField] float m_metresLeft = 50;

	new void Awake()
	{
		interactions = new Interaction[1] {
			new() {
				prompt = GetPromptText(),
				sprite = itemSprite,
				interact = PickUp
			}
		};
	}

	string GetPromptText()
	{
		return $"Pick up {itemName} ({(int)m_metresLeft}m)";
	}

	static bool IsNotFull(Item item)
	{
		if (item is not RailKit) return false;

		RailKit kit = item as RailKit;

		return kit.m_metresLeft < kit.m_maxMetresStack;
	}

	protected override void PickUp(PlayerInteraction interactor)
	{
		PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

		if (inventory.FindItemWhere(IsNotFull) != null)
		{
			// Check if we have an empty hand
			if (inventory.GetActiveSlot().items.Count == 0)
			{
				inventory.AddItem(this);
				return;
			}

			// Absord metres into existing kits
			RailKit kit;
			while (kit = inventory.FindItemWhere(IsNotFull) as RailKit)
			{
				TransportMetresTo(kit);
			}
		}
		else
		{
			inventory.AddItem(this);
		}

		interactor.ForceRefresh();
	}

	public override Interaction[] GetInteractions()
	{
		interactions[0].prompt = GetPromptText();

		return base.GetInteractions();
	}

	// Move metres to another kit
	void TransportMetresTo(RailKit kit)
	{
		float maxAdd = kit.m_maxMetresStack - kit.m_metresLeft;
		float maxRemove = m_metresLeft;

		float remove = Mathf.Min(maxAdd, maxRemove);

		m_metresLeft -= remove;

		kit.m_metresLeft += remove;

		// Destroy when empty
		if (m_metresLeft <= 0)
		{
			Destroy(gameObject);
		}
	}

	public override string GetCustomStackText()
	{
		return $"{m_metresLeft}m";
	}
}
