using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
	[Header("Parameters")]
	[SerializeField] private bool _ignoreInteractionPoint;

	[SerializeField] private Transform _interactionPoint;

	/// <summary>
	/// Returns all of the interaction components on this GameObject
	/// </summary>
	public IInteraction[] GetInteractions(out bool isCenterApproach)
	{
		isCenterApproach = _ignoreInteractionPoint;

		MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
		List<IInteraction> validInterations = new();

		foreach (MonoBehaviour i in allComponents)
		{
			// Validate that each interaction is enabled and its component is enabled (valid)
			if (i is IInteraction && (i as IInteraction).IsInteractionEnabled && i.enabled)
			{
				validInterations.Add(i as IInteraction);

				// Break out of the loop if the interaction point is ignored because only a single interaction can work for the center screen approach
				if (_ignoreInteractionPoint && validInterations.Count >= 1)
					break;
			}
		}

		// Return all valid interactions
		return validInterations.ToArray();
	}

	/// <summary>
	/// Returns the transform that will be used to update the interaction displays position
	/// </summary>
	public Transform GetInteractionPoint()
	{
		return _interactionPoint;
	}
}
