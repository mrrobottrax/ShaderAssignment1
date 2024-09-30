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
	private InteractionUIManager interactionUI;
	private PlayerController playerController;

	[Header("System")]
	private float sqrInteractionRange;
	private Vector3 lastKnownLocalHitPoint;
	private bool interactableWasNull = false;

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
		interactionUI = PlayerUIManager.InteractionPromptDisplay;
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
		if (isInteractPressed && interactionUI.HasOptionSelected())
			Interact(interactionUI.GetSelectedInteraction());
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
		Interactable interactable = RaycastForInteractable();
		if (interactable != interactionUI.GetCurrentInteractable())
		{
			interactionUI.SetCurrentInteractable(interactable);
			interactableWasNull = interactable == null;
		}
		// A scene change can make interactionUI.GetCurrentInteractable() null, making it miss deselecting the interactable
		else if (!interactionUI.GetCurrentInteractable() && !interactableWasNull)
		{
			interactionUI.SetCurrentInteractable(null);
			interactableWasNull = true;
		}
	}
	#endregion

	void Interact(Interaction interaction)
	{
		interaction.interact(this);
	}

	Interactable RaycastForInteractable()
	{
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
				// Get the hitpoint in local space
				lastKnownLocalHitPoint = hit.transform.worldToLocalMatrix * new Vector4(hit.point.x, hit.point.y, hit.point.z, 1);
				return hitInteractable;
			}
		}

		// Check if we should keep the current interactable selected.
		// This can happen when the interaction prompts are larger than the object itself.
		if (interactionUI.GetCurrentInteractable())
		{
			if (interactionUI.IsCursorInsideRect())
			{
				// Check whichever dist is lowest, either from the center or from the last position the raycast hit.
				// This helps when right on the edge of interacting with an object. The raycast fails because we aren looking
				// at the interaction prompt, and the distance from the center is longer than the raycast would be.
				float sqrDist1 = (interactionUI.GetCurrentInteractable().transform.position - _cameraTransform.position).sqrMagnitude;

				Vector3 globalHitpoint = interactionUI.GetCurrentInteractable().transform.localToWorldMatrix *
					new Vector4(lastKnownLocalHitPoint.x, lastKnownLocalHitPoint.y, lastKnownLocalHitPoint.z, 1);

				float sqrDist2 = (globalHitpoint - _cameraTransform.position).sqrMagnitude;

				if (Mathf.Min(sqrDist1, sqrDist2) <= sqrInteractionRange)
					return interactionUI.GetCurrentInteractable();
			}
		}

		return null;
	}
}
