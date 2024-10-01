using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
struct EventInteraction
{
	public string prompt;
	public Sprite sprite;

	public UnityEvent interact;
}

public class UnityEventInteractable : Interactable
{
	[SerializeField] EventInteraction[] _interactions;
	Interaction[] interactions;

	private void Awake()
	{
		// Copy from inspector friendly struct to classes
		interactions = new Interaction[_interactions.Length];
		for (int i = 0; i < interactions.Length; ++i)
		{
			int j = i; // This is a stupid hack but unfortunately it has to be here
			interactions[i] = new Interaction()
			{
				prompt = _interactions[i].prompt,
				sprite = _interactions[i].sprite,
				interact = (interactor) => _interactions[j].interact.Invoke(),
			};
		}
	}

	public override Interaction[] GetInteractions()
	{
		return interactions;
	}
}
