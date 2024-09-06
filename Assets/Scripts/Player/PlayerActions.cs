using UnityEngine;
using UnityEngine.InputSystem;

// TODO: This class is pretty nasty. We should either fully commit and have it manage all player input by itself, or have each
// class manage their own relevant input by themselves. As it exists now it's spaghetti incarnate.

// Is this class not managing its own input? I am pretty sure all classes that have input keep it contained to themselves.
public class PlayerActions : MonoBehaviour, IInputHandler
{
    [Header("Components")]
    private PlayerViewModelManager viewModelManager;

    [Header("System")]
    private bool isPlayerReady;

    private bool isHoldingReload;
    private float timeReloadHeld = 0;

    #region Initialization Methods

    private void Awake()
    {
        viewModelManager = GetComponentInChildren<PlayerViewModelManager>();
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

    /// <summary>
    /// When not ready this method will ready the player when called. If they are ready, it will execute the held items primary function.
    /// </summary>
    private void OnPressFire1(InputAction.CallbackContext context)
    {
        if (!viewModelManager.Entity.IsAbleToAttack)
            return;

        if (!isPlayerReady)
        {
            isPlayerReady = true;
            viewModelManager.SetPlayerReady(isPlayerReady);
        }
        else
        {
            // Try primary function
            viewModelManager.TryPrimaryFunction();
        }

    }

    /// <summary>
    /// This method sets the fire 1 bools state on the player view model
    /// </summary>
    public void Fire1Input(InputAction.CallbackContext context)
    {
        viewModelManager.SetIsHoldingFire1(context.ReadValueAsButton());
    }

    /// <summary>
    /// When called and the player is ready this method will execute the held items secondary function.
    /// </summary>
    private void OnPressFire2(InputAction.CallbackContext context)
    {
        if (!viewModelManager.Entity.IsAbleToAttack)
            return;

        if (isPlayerReady)
            viewModelManager.TrySecondaryFunction();
    }

    /// <summary>
    /// This method sets the fire 2 bools state on the player view model
    /// </summary>
    public void Fire2Input(InputAction.CallbackContext context)
    {
        viewModelManager.SetIsHoldingFire2(context.ReadValueAsButton());
    }

    /// <summary>
    /// When called, this method will start the reloading process if possible.
    /// </summary>
    private void OnPressReload(InputAction.CallbackContext context)
    {
        if (!viewModelManager.Entity.IsAbleToAttack)
            return;

        isHoldingReload = true;
        timeReloadHeld = 0;
    }

    /// <summary>
    /// When called, this method will determine if reloading is possible based on the players ready state.
    /// Trying to reload when not ready will cause the player to ready.
    /// </summary>
    private void OnReleaseReload(InputAction.CallbackContext context)
    {
        if (!viewModelManager.Entity.IsAbleToAttack)
            return;

        isHoldingReload = false;

        if (isPlayerReady)
        {
            // Check if the held item can be reloaded
        }
        else if (!isPlayerReady && timeReloadHeld <= 0.2f)// Only ready the player when they briefly click reload, not after they release it after waiting.
        {
            isPlayerReady = true;
            viewModelManager.SetPlayerReady(isPlayerReady);
        }

        // Reset reloading timer
        timeReloadHeld = 0;
    }

    public void Subscribe()
    {
        // Fire 1
        InputManager.Instance.controls.Player.Fire1.performed += OnPressFire1;

        InputManager.Instance.controls.Player.Fire1.performed += Fire1Input;
        InputManager.Instance.controls.Player.Fire1.canceled += Fire1Input;

        // Fire 2
        InputManager.Instance.controls.Player.Fire2.performed += OnPressFire2;

        InputManager.Instance.controls.Player.Fire2.performed += Fire2Input;
        InputManager.Instance.controls.Player.Fire2.canceled += Fire2Input;

        // Reload
        InputManager.Instance.controls.Player.Reload.performed += OnPressReload;
        InputManager.Instance.controls.Player.Reload.canceled += OnReleaseReload;
    }

    public void Unsubscribe()
    {
        // Fire 1
        InputManager.Instance.controls.Player.Fire1.performed -= OnPressFire1;

        InputManager.Instance.controls.Player.Fire1.performed -= Fire1Input;
        InputManager.Instance.controls.Player.Fire1.canceled -= Fire1Input;

        // Fire 2
        InputManager.Instance.controls.Player.Fire2.performed -= OnPressFire2;

        InputManager.Instance.controls.Player.Fire2.performed -= Fire2Input;
        InputManager.Instance.controls.Player.Fire2.canceled -= Fire2Input;

        // Reload
        InputManager.Instance.controls.Player.Reload.performed -= OnPressReload;
        InputManager.Instance.controls.Player.Reload.canceled -= OnReleaseReload;
    }
    #endregion

    #region Unity Callbacks

    void OnDestroy()
    {
        SetControlsSubscription(false);
    }

    private void Update()
    {
        // Count up if the player is holding reload
        if (isHoldingReload && timeReloadHeld < 1)
        {
            timeReloadHeld += Time.deltaTime;

            // If reload passes threshold, un-ready the player
            if (timeReloadHeld > 1)
            {
                isHoldingReload = false;
                isPlayerReady = false;
                viewModelManager.SetPlayerReady(isPlayerReady);
            }
        }

    }
    #endregion
}
