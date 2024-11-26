public class ItemNetworkBehaviour : NetworkBehaviour
{
	public Item Item { get; private set; }

	void Awake()
	{
		Item = GetComponent<Item>();
	}
}