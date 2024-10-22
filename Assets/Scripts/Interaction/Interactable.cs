using UnityEngine;

public delegate void InteractDelegate(PlayerInteraction interactor);

public class Interaction
{
	public string prompt;
	public Sprite sprite;

	public InteractDelegate interact;
}

public abstract class Interactable : NetworkBehaviour
{
	[Header("Interactable")]
	[SerializeField] Transform _interactionPoint;

	public bool interactionEnabled = true;

	public Transform GetInteractionPoint()
	{
		return _interactionPoint;
	}

	public abstract Interaction[] GetInteractions();
}
