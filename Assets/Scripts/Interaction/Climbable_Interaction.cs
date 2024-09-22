using UnityEngine;
using UnityEngine.InputSystem;

public class Climbable_Interaction : MonoBehaviour, IInteraction, IInputHandler
{
	[Header("Interaction Parameters")]
	[SerializeField] private string interactionPrompt;
	public string InteractionPrompt => interactionPrompt;

	[SerializeField] private Sprite interactSprite;
	public Sprite InteractSprite => interactSprite;

	[SerializeField] private bool interactionEnabled = true;
	public bool IsInteractionEnabled => interactionEnabled;

	[Header("Parenting")]
	[SerializeField] private Transform top;
	[SerializeField] private Transform bottom;

	[Header("System")]
	private PlayerInteraction interactor;

    #region Input Methods

    public void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }

    /// <summary>
    /// This allows the player to leave the adjustment interactable
    /// </summary>
    public void DismountInput(InputAction.CallbackContext context)
    {
        KickClimber();
    }

    public void Subscribe()
    {
        InputManager.Instance.Permanents.Interact.performed += DismountInput;
    }

    public void Unsubscribe()
    {
        InputManager.Instance.Permanents.Interact.performed -= DismountInput;
    }
    #endregion

	public void Interact(Transform interactorTransform)
	{
		// Get the interactor component from the passed in transform
		interactorTransform.TryGetComponent(out interactor);
		PlayerController playerController = interactor.GetPlayerController();

		interactor.UseInvolvedInteractable();

		// Enter climbing state
		//playerController.BeginClimb(bottom, top, this);

		// Enable ladder controls
		SetControlsSubscription(true);
	}

	/// <summary>
	/// Removes the interactor from this ladder and resets the system to a null state
	/// </summary>
	public void KickClimber()
	{
		// Disable ladder controls
		SetControlsSubscription(false);

		interactor.LeaveInvolvedInteractable();

		// Exit climbing state
		PlayerController playerController = interactor.GetPlayerController();
		//playerController.DismountClimb();

		interactor = null;
	}

    void OnDestroy()
    {
        SetControlsSubscription(false);
    }

    public void SetInteractionEnabled(bool enabled)
    {
		interactionEnabled = enabled;
    }
}
