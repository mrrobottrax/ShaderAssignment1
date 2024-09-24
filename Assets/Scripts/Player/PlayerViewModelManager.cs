using UnityEngine;

public class PlayerViewModelManager : EntityAnimationManager_Base
{
    [Header("View Model")]
    [SerializeField] private ViewModel_Base[] _viewModelIndex;

    [Header("Components")]
    private PlayerHealth player;
    private FirstPersonCamera firstPersonCamera;

    [Header("System")]
    private Weapon_Item currentItem;
    private ViewModel_Base currentViewModel;
    private bool isTransitionInProgress;

    private string prevAttackGroup;
    private int currentChain = 0;// The current increment in an attack chain

    #region Initialization Methods
    protected override void Awake()
    {
        base.Awake();

        // Get a refference to the player using the manager bases entity
        if (Entity is PlayerHealth player)
            this.player = player;

        // Disable all ViewModels on start
        foreach (ViewModel_Base i in _viewModelIndex)
        {
            if (i != null)
            {
                i.SetViewModelMeshesActive(false);
                animator.SetBool(i.AnimationSet, false);
            }
        }

        animator.SetBool("IsHoldingNothing", true);
    }

    private void Start()
    {
        firstPersonCamera = player.GetPlayerCamera();
    }
    #endregion

    #region View Model Methods

    /// <summary>
    /// This method will search through the view model index and enable the correct view model and its animations
    /// </summary>
    public void SetViewModel(Weapon_Item weaponItem)
    {
        // Hide current view model if possible
        ClearCurrentViewModel();

        // Cache weapon data
        currentItem = weaponItem;

        // Set new view model data
        currentViewModel = _viewModelIndex[currentItem.GetWeaponData().ViewModelID];

        // Enable the ViewModel's primary mesh
        currentViewModel.SetPrimaryMeshActive(true);

        // Update animator to use the view models animation set
        animator.SetBool("IsHoldingNothing", false);
        animator.SetBool(currentViewModel.AnimationSet, true);
    }

    /// <summary>
    /// This method disables then clears the current view model
    /// </summary>
    public void ClearCurrentViewModel()
    {
        if (currentViewModel != null)
        {
            // Disable the view models meshes
            currentViewModel.SetViewModelMeshesActive(false);

            // Stop the animator from using the view models animation set
            animator.SetBool(currentViewModel.AnimationSet, false);
        }

        // Transition to the none clip
        TriggerHolster();

        // Clear current item data & view model
        currentItem = null;
        currentViewModel = null;

        // Update the animator
        animator.SetBool("IsHoldingNothing", true);
        prevAttackGroup = "";
        SetActionChain(0);

        // Finish the ongoing attack
        if(Entity.IsAttackInProgress)
            FinishAttack_AnimationEvent();
    }

    /// <summary>
    /// Triggers the "Ready" trigger on the view model animator
    /// </summary>
    public void TriggerReady()
    {
        animator.SetTrigger("Ready");
    }

    /// <summary>
    /// Triggers the "Holster" trigger on the view model animator
    /// </summary>
    public void TriggerHolster()
    {
        animator.SetTrigger("Holster");
    }

    /// <summary>
    /// Sets the state of the "IsReady" boolean on the view model animator
    /// </summary>
    /// <remarks>
    /// This is should be used when transitioning to layers when already ready
    /// </remarks>
    public void SetReady(bool isReady)
    {
        animator.SetBool("IsReady", isReady);
    }
    #endregion

    #region Input Receiving Methods

    /// <summary>
    /// Attempts to trigger the primary attack function.
    /// </summary>
    public void TryPrimaryFunction()
    {
        // Ensure the player is not already attacking before triggering
        if (Entity.IsAbleToAttack)
            animator.SetTrigger("Fire1");
    }

    /// <summary>
    /// Sets the animator's "IsHoldingFire1" boolean based on whether the primary attack button is being held down.
    /// </summary>
    /// <param name="isHolding">Whether the primary attack button is being held down.</param>
    public void SetIsHoldingFire1(bool isHolding)
    {
        animator.SetBool("IsHoldingFire1", isHolding);
    }

    /// <summary>
    /// Attempts to trigger the secondary attack function.
    /// </summary>
    public void TrySecondaryFunction()
    {
        animator.SetTrigger("Fire2");
    }

    /// <summary>
    /// Sets the animator's "IsHoldingFire2" boolean based on whether the secondary attack button is being held down.
    /// </summary>
    /// <param name="isHolding">Whether the secondary attack button is being held down.</param>
    public void SetIsHoldingFire2(bool isHolding)
    {
        animator.SetBool("IsHoldingFire2", isHolding);
    }

    /// <summary>
    /// Attempts to reload the weapon. Currently, this method always returns false as reloading is not implemented.
    /// </summary>
    /// <param name="isSuccessful">Returns whether the reload was successful. Always false in this implementation.</param>
    public void TryReload(out bool isSuccessful)
    {
        isSuccessful = false;
    }

    /// <summary>
    /// Sets the action chain value used in view model animations.
    /// </summary>
    /// <param name="value">The new value of the view model's attack chain.</param>
    private void SetActionChain(int value)
    {
        currentChain = value;

        // Set the chain value on the animator
        animator.SetInteger("ViewModelActionChain", value);
    }

#endregion

    #region Implemented Animation Event Methods

    public override void StartAttack_AnimationEvent()
    {
        Entity.EntityBeginAttack();
    }

    public override void TryAttack_AnimationEvent(string attackGroup)
    {
        AttackList.AttackGroup chosenGroup;
        AttackList.Attack attack;
        int attackChainLength;

        // Store attack source
        bool isUsingViewModel = currentViewModel != null;
        AttackList attackSourceList = isUsingViewModel ? currentViewModel.ViewModelAttacks : entityAttacks;

        // Determine the attack group and chosen attack
        chosenGroup = attackSourceList.GetAttackGroup(attackGroup);
        attack = attackSourceList.GetAttackFromGroup(chosenGroup, currentChain);

        // Compare the last attack group to the newly chosen one
        if (chosenGroup.GroupTitle != prevAttackGroup)
            currentChain = 0;

        // Check if an item is equipped
        if (currentViewModel == null)
        {
            // Perform the attack data using the AttackData from the entities AttackList.
            Entity.EntityPerformOngoingAttack(this, attack.AttackData, attack.AttackPosition.position, firstPersonCamera.CameraTransform.forward);
        }
        else
        {
            // Perform the attack data using the AttackData from the weapons ViewModels AttackList.
            currentViewModel.TryModelFunction(player, this, currentItem, attack, attackGroup);
        }

        // Ensure the view model has not been cleared as a result of the attack before progressing
        if(!isUsingViewModel || isUsingViewModel && currentViewModel != null)
        {
            // Set the attack chain length to the number of attacks in the chosen attack group
            attackChainLength = chosenGroup.Attacks.Length;

            // Increment the action chain, and loop back to zero if it reaches the end of the chain.
            SetActionChain((currentChain + 1) % attackChainLength);

            // Set the prev attack group
            prevAttackGroup = chosenGroup.GroupTitle;
        }
    }
    #endregion

    #region Animation Event Methods

    /// <summary>
    /// Enables all of the current ViewModel's meshes
    /// </summary>
    public void EnableViewModelMeshes()
    {
        currentViewModel.SetViewModelMeshesActive(true);
    }

    /// <summary>
    /// Disables all of the current ViewModel's meshes
    /// </summary>
    public void DisableViewModelMeshes()
    {
        currentViewModel.SetViewModelMeshesActive(false);
    }

    /// <summary>
    /// Enables the current ViewModel's primary mesh
    /// </summary>
    public void EnableViewModelPrimaryMesh()
    {
        currentViewModel.SetPrimaryMeshActive(true);
    }

    /// <summary>
    /// Disables the current ViewModel's primary mesh
    /// </summary>
    public void DisableViewModelPrimaryMesh()
    {
        currentViewModel.SetPrimaryMeshActive(false);
    }

    /// <summary>
    /// Enables one of the current ViewModel's additional meshes
    /// </summary>
    public void EnableViewModelAdditionalMesh(int ID)
    {
        currentViewModel.SetAdditionalMeshActive(ID, true);
    }

    /// <summary>
    /// Disables one of the current ViewModel's additional meshes
    /// </summary>
    public void DisableViewModelAdditionalMesh(int ID)
    {
        currentViewModel.SetAdditionalMeshActive(ID, false);
    }
    #endregion
}
