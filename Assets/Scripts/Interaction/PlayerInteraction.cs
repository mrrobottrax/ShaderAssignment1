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
	}

	#endregion

	#region Input Methods

	public void SetControlsSubscription(bool isInputEnabled)
	{
		if (isInputEnabled)
			Subscribe();
		else
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
		if (isInteractPressed && PlayerUIManager.InteractionPromptDisplay && PlayerUIManager.InteractionPromptDisplay.HasOptionSelected() && PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable().interactionEnabled)
			PlayerUIManager.InteractionPromptDisplay.GetSelectedInteraction().interact(this);
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
		if (interactable != PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable())
		{
			PlayerUIManager.InteractionPromptDisplay.SetCurrentInteractable(interactable);
			interactableWasNull = interactable == null;
		}
		// A scene change can make interactionUI.GetCurrentInteractable() null, making it miss deselecting the interactable
		else if (!PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable() && !interactableWasNull)
		{
			PlayerUIManager.InteractionPromptDisplay.SetCurrentInteractable(null);
			interactableWasNull = true;
		}
	}
	#endregion

	public void ForceRefresh()
	{
		PlayerUIManager.InteractionPromptDisplay.SetCurrentInteractable(null);
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

			if (hitInteractable != null && hitInteractable.interactionEnabled)
			{
				// Get the hitpoint in local space
				lastKnownLocalHitPoint = hit.transform.worldToLocalMatrix * new Vector4(hit.point.x, hit.point.y, hit.point.z, 1);
				return hitInteractable;
			}
		}

		// Check if we should keep the current interactable selected.
		// This can happen when the interaction prompts are larger than the object itself.
		if (PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable() && PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable().interactionEnabled)
		{
			if (PlayerUIManager.InteractionPromptDisplay.IsCursorInsideRect())
			{
				// Check whichever dist is lowest, either from the center or from the last position the raycast hit.
				// This helps when right on the edge of interacting with an object. The raycast fails because we aren looking
				// at the interaction prompt, and the distance from the center is longer than the raycast would be.
				float sqrDist1 = (PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable().transform.position - _cameraTransform.position).sqrMagnitude;

				Vector3 globalHitpoint = PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable().transform.localToWorldMatrix *
					new Vector4(lastKnownLocalHitPoint.x, lastKnownLocalHitPoint.y, lastKnownLocalHitPoint.z, 1);

				float sqrDist2 = (globalHitpoint - _cameraTransform.position).sqrMagnitude;

				if (Mathf.Min(sqrDist1, sqrDist2) <= sqrInteractionRange)
					return PlayerUIManager.InteractionPromptDisplay.GetCurrentInteractable();
			}
		}

		return null;
	}
}
