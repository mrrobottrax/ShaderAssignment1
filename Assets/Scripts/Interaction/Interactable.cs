using UnityEngine;

public delegate void InteractDelegate(PlayerInteraction interactor);

public class Interaction
{
	public string prompt;
	public Sprite sprite;

	public InteractDelegate interact;
}

public abstract class Interactable : MonoBehaviour
{
	[SerializeField] Transform _interactionPoint;
	public Transform GetInteractionPoint()
	{
		return _interactionPoint;
	}

	public abstract Interaction[] GetInteractions();
}
