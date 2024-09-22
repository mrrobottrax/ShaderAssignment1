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
	private PlayerUIManager playerUIManager;
	private InteractionDisplay promptDisplay;
	private PlayerController playerController;


	[Header("Systems")]
	private float sqrInteractionRange;
	public bool IsUsingInteractable { get; private set; }
	private Interactable currentInteractable;
	private InteractionOptionDisplay interactionOptions;

	#region Initialization Methods

	private void Awake()
	{
		playerController = GetComponent<PlayerController>();
		playerUIManager = GetComponentInChildren<PlayerUIManager>();

        sqrInteractionRange = _interactionRange * _interactionRange;

		Assert.IsNotNull(_cameraTransform);
	}

	private void Start()
	{
		// Enable controls
		SetControlsSubscription(true);

		// Cache prompt display
		promptDisplay = playerUIManager.InteractionPromptDisplay;
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
		if (!IsUsingInteractable && Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _interactionRange,
			_interactionLayer))
		{
			// Ensure an interactable was hit
			if (hit.collider.TryGetComponent(out Interactable hitInteractable) || hit.collider.GetComponentInParent<Interactable>() != null)
			{
				// If it was not on the collider hit, check the parent.
				if (hitInteractable == null)
					hitInteractable = hit.collider.GetComponentInParent<Interactable>();

				// Compare the new interactable to the previous
				if (hitInteractable.GetInteractions(out bool isUsingCenterApproach).Length > 0 && prevInteractable != hitInteractable)
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

	public void SetUsingInteractable(bool usingInteractable)
	{
		IsUsingInteractable = usingInteractable;
	}

	/// <summary>
	/// Puts the player into an interacting state and sets their parenting, position and rotation.
	/// </summary>
	/// <param name="interactionParent">The new parent</param>
	/// <param name="offset">The positional offset</param>
	/// <param name="rot">The rotation</param>
	public void UseInvolvedInteractable(Transform interactionParent = null, Vector3? offset = null, Quaternion? rot = null)
	{
		SetUsingInteractable(true);
		//playerController.BeginUsingInvolvedInteractable(interactionParent, offset, rot);
	}

	/// <summary>
	/// Removes the players parenting from an interactable
	/// </summary>
	/// <param name="offset">The position they will be at after leaving</param>
	/// <param name="rot">The rotation they will have after leaving</param>
	public void LeaveInvolvedInteractable(Vector3? offset = null, Quaternion? rot = null)
	{
		SetUsingInteractable(false);
		//playerController.EndUsingInvolvedInteractable(offset, rot);
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

	#region Helper Methods

	/// <summary>
	/// Returns the player controller for interactions that need it
	/// </summary>
	/// <returns>PlayerController ref</returns>
	public PlayerController GetPlayerController()
	{
		return playerController;
	}
    #endregion
}
