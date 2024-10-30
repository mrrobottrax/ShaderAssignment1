using System.Collections.Generic;
using UnityEngine;

public class InteractionUIManager : MonoBehaviour
{
	[Header("Display")]
	[SerializeField] private RectTransform _optionHolder; // The rect that contains the displays options
	[SerializeField] private Sprite keySprite; // In the future we should take this from a sprite atlas so we can show contexts based on different platforms

	[Header("Components")]
	[SerializeField] private InteractionOptionButton _optionPrefab; // Used to clone more prompt buttons if needed

	[Header("System")]
	private readonly List<InteractionOptionButton> availableOptionsPool = new();
	private Interactable currentInteractable;
	private InteractionOptionButton selectedOption;

	#region Callbacks

	private void Awake()
	{
		// Add the object used to clone more options to the pool
		_optionPrefab.SetManager(this);
		availableOptionsPool.Add(_optionPrefab);
	}

	private void Update()
	{
		selectedOption = null;
		if (currentInteractable == null) return;

		// Follow the object in world space
		if (currentInteractable.GetInteractionPoint())
		{
			_optionHolder.transform.position = Camera.main.WorldToScreenPoint(currentInteractable.GetInteractionPoint().position);
		}
		else
		{
			_optionHolder.transform.position = Camera.main.WorldToScreenPoint(currentInteractable.transform.position);
		}

		// Check if any options are selected
		foreach (var option in availableOptionsPool)
		{
			if (!option.gameObject.activeInHierarchy)
				continue;

			if (option.IsSelected())
			{
				selectedOption = option;
				option.ShowButtonImage(true);
			}
			else
			{
				option.ShowButtonImage(false);
			}
		}
	}

	#endregion

	public bool IsCursorInsideRect()
	{
		Vector2 screenCenter = new(Screen.width / 2, Screen.height / 2);
		return RectTransformUtility.RectangleContainsScreenPoint(_optionHolder, screenCenter);
	}

	public void SetCurrentInteractable(Interactable interactable)
	{
		// Intially disable all options
		foreach (var optionButton in availableOptionsPool)
		{
			optionButton.gameObject.SetActive(false);
		}

		// Early quit when null
		if (interactable == null)
		{
			currentInteractable = null;
			return;
		}

		// Set up interaction option buttons
		currentInteractable = interactable;
		Interaction[] options = interactable.GetInteractions();

		if (options == null)
		{
			//Debug.LogWarning($"No interactions on object {interactable.gameObject.name}");
			return;
		}

		while (availableOptionsPool.Count < options.Length)
		{
			availableOptionsPool.Add(Instantiate(_optionPrefab, _optionHolder));
		}

		for (int i = 0; i < options.Length; ++i)
		{
			availableOptionsPool[i].gameObject.SetActive(true);
			availableOptionsPool[i].Setup(options[i]);
		}
	}

	public bool HasOptionSelected()
	{
		return selectedOption != null;
	}

	public Interaction GetSelectedInteraction()
	{
		if (selectedOption == null) return null;

		return selectedOption.GetInteraction();
	}

	public Interactable GetCurrentInteractable()
	{
		return currentInteractable;
	}

	public Sprite GetButtonSprite()
	{
		return keySprite;
	}
}
