using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour, IInputHandler
{
	[Header("Parameters")]
	[SerializeField] private float _interactionRange = 3f;
	[SerializeField] private LayerMask _interactionLayer;

	[Header("Componenets")]
	[SerializeField] Transform _cameraTransform;
	private InteractionDisplay promptDisplay;
	private PlayerController playerController;

	[Header("System")]
	private float sqrInteractionRange;
	private Interactable currentInteractable;
	private InteractionOptionDisplay interactionOptions;

	#region Initialization Methods

	private void Awake()
	{
		playerController = GetComponent<PlayerController>();

		sqrInteractionRange = _interactionRange * _interactionRange;

		Assert.IsNotNull(_cameraTransform);
	}

	private void Start()
	{
		// Enable controls
		SetControlsSubscription(true);

		// Cache prompt display
		promptDisplay = PlayerUIManager.InteractionPromptDisplay;
	}
	#endregion

	#region Input Methods

	public void SetControlsSubscription(bool isInputEnabled)
	{
		if (isInputEnabled)
			Subscribe();
		else if (InputManager.Instance != null)
			Unsubscribe();
	}

	public void Subscribe()
	{
		InputManager.Instance.Permanents.Interact.performed += TryInteractInput;
	}

	public void Unsubscribe()
	{
		InputManager.Instance.Permanents.Interact.performed -= TryInteractInput;
	}

	private void TryInteractInput(InputAction.CallbackContext context)
	{
		// Gets if the button is pressed
		bool isInteractPressed = context.ReadValueAsButton();

		// Ensure that interact was pressed, there is a current interactable, an option is highlighted, and both display types are valid.
		if (isInteractPressed && currentInteractable != null && interactionOptions != null)
			Interact(interactionOptions.GetInteractionData());
	}

	#endregion

	#region Unity Callbacks

	private void OnEnable()
	{
		SetControlsSubscription(true);
	}


	private void OnDisable()
	{
		SetControlsSubscription(false);
	}

	private void Update()
	{
		// Cache what the player was just looking at
		Interactable prevInteractable = currentInteractable;

		Debug.DrawRay(_cameraTransform.position, _cameraTransform.forward);

		// Send out a raycast
		if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _interactionRange,
			_interactionLayer))
		{
			// Ensure an interactable was hit
			if (!hit.collider.TryGetComponent(out Interactable hitInteractable))
			{
				// If it was not on the collider hit, check the parent.
				hitInteractable = hit.collider.GetComponentInParent<Interactable>();
			}

			if (hitInteractable != null)
			{
				// Compare the new interactable to the previous
				if (hitInteractable.GetInteractions(out bool _).Length > 0 && prevInteractable != hitInteractable)
					SetCurrentInteractable(hitInteractable);
			}
		}
		// If nothing was hit, the player has a current interactable, the player is cursor is not within the interaction display or the player out of the interaction range
		else if (currentInteractable != null && (!promptDisplay.ValidateInteraction() || (currentInteractable.transform.position - transform.position).sqrMagnitude > sqrInteractionRange))
		{
			// Nothing was hit so clear the current interactable
			ClearCurrentInteractable();
		}
	}
	#endregion

	#region Current Interactable Methods

	/// <summary>
	/// Enables interaction options for an interactable
	/// </summary>
	/// <remarks>
	/// This should be called when a player looks at an interactable
	/// </remarks>
	/// <param name="interactable">The interactable chosen</param>
	public void SetCurrentInteractable(Interactable interactable)
	{
		// Clear the prev interactable
		if (currentInteractable != null)
			ClearCurrentInteractable();

		// Set the new current interactable
		currentInteractable = interactable;

		// Enable promopt & outline
		promptDisplay.SetInteratableOptions(this, currentInteractable.GetInteractionPoint(), currentInteractable);
	}

	/// <summary>
	/// Clears the current interactable and resets the system
	/// </summary>
	public void ClearCurrentInteractable()
	{
		// Set to null
		currentInteractable = null;

		// Disable prompt & outline
		promptDisplay.ClearInteractableOptions();

		// Clear the highlighted interaction prompt
		ClearInteractionOptions();
	}

	#endregion

	#region Interaction Methods

	/// <summary>
	/// Sets the currently interaction options
	/// </summary>
	/// <param name="interaction">The interact</param>
	public void SetInteractionOptions(InteractionOptionDisplay interaction)
	{
		interactionOptions = interaction;
	}

	/// <summary>
	/// Clear the current interaction options
	/// </summary>
	public void ClearInteractionOptions()
	{
		interactionOptions = null;
	}

	/// <summary>
	/// Executes an interactions logic
	/// </summary>
	/// <param name="interaction">The interaction with logic to execute</param>
	private void Interact(IInteraction interaction)
	{
		interaction.Interact(transform);

		// Clear current interactions
		ClearCurrentInteractable();
		ClearInteractionOptions();
	}
	#endregion
}
