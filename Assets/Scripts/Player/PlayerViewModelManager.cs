using UnityEngine;
using UnityEngine.InputSystem;
using static AttackList;

public class PlayerViewmodelManager : EntityAnimationManager_Base, IInputHandler
{
	[field: Header("Components")]
    public Animator Animator { get; private set; }
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private RuntimeAnimatorController _handsAnimatorController;
    [SerializeField] private FirstPersonCamera _firstPersonCamera;

    [field: Header("Transforms")]
    [field: SerializeField] private Transform _handTransform;

	// System
    private string prevAttackGroup;
    private int currentChain = 0;// The current increment in an attack chain

    #region Initialization Methods

    protected override void Awake()
	{
        base.Awake();

		Animator = GetComponent<Animator>();

		_inventory.OnAddItem += OnAddItem;
		_inventory.OnDropItem += OnDropItem;
		_inventory.OnActiveSlotChange += OnActiveSlotChange;
	}

    private void Start()
    {
        if (IsOwner)
            SetControlsSubscription(true);
    }
    #endregion

    #region Unity Callbacks

    private void OnDestroy()
	{
		_inventory.OnAddItem -= OnAddItem;
		_inventory.OnDropItem -= OnDropItem;
		_inventory.OnActiveSlotChange -= OnActiveSlotChange;

        if (IsOwner)
            SetControlsSubscription(false);
    }

    #endregion

    #region Input Methods

    public void Subscribe()
    {
        InputManager.Instance.Player.Fire1.performed += Fire1;
        InputManager.Instance.Player.Fire1.canceled += Fire1;
        InputManager.Instance.Player.Fire2.performed += Fire2;
        InputManager.Instance.Player.Fire2.canceled += Fire2;
    }

    public void Unsubscribe()
    {
        InputManager.Instance.Player.Fire1.performed -= Fire1;
        InputManager.Instance.Player.Fire1.canceled -= Fire1;
        InputManager.Instance.Player.Fire2.performed -= Fire2;
        InputManager.Instance.Player.Fire2.canceled -= Fire2;
    }

    public void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else
            Unsubscribe();
    }

    void Fire1(InputAction.CallbackContext ctx)
    {
        // Ensure the player is not already attacking before triggering
        if (!Entity.IsAbleToAttack) return;

        bool fired = ctx.ReadValueAsButton();
        if (HasParameter("IsHoldingFire1"))
            animator.SetBool("IsHoldingFire1", fired);

        // Only trigger fire if the button is pressed, no released
        if (fired && HasParameter("Fire1"))
            animator.SetTrigger("Fire1");
    }

    void Fire2(InputAction.CallbackContext ctx)
    {
        // Ensure the player is not already attacking before triggering
        if (!Entity.IsAbleToAttack) return;

        bool fired = ctx.ReadValueAsButton();
        if (HasParameter("IsHoldingFire2"))
            animator.SetBool("IsHoldingFire2", fired);

        // Only trigger fire if the button is pressed, no released
        if (fired && HasParameter("Fire2"))
            animator.SetTrigger("Fire2");
    }
    #endregion

    #region Animator State Methods

    void PutItemInHands(Item item, bool putInHands)
	{
		item.interactionEnabled = !putInHands;

		// Follow hand
		if (putInHands)
		{
			item.transform.parent = _handTransform;
			item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		}
		else
		{
			item.transform.parent = null;
		}

		// Disable/enable physics
		Rigidbody[] rbs = item.GetComponentsInChildren<Rigidbody>();
		foreach (var rb in rbs)
		{
			rb.isKinematic = putInHands;
			rb.detectCollisions = !putInHands;
			rb.interpolation = putInHands ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
		}
	}

	void OnAddItem(Item item, InventorySlot slot)
	{
		PutItemInHands(item, true);

		EnableCorrectItems();
		SetController();

		if (slot == _inventory.GetActiveSlot())
			Animator.SetTrigger("Equip");
	}

	void OnDropItem(Item item, InventorySlot slot)
	{
		PutItemInHands(item, false);

		EnableCorrectItems();
		SetController();

		if (slot == _inventory.GetActiveSlot())
			Animator.SetTrigger("Equip");
	}

	void OnActiveSlotChange(InventorySlot prev, InventorySlot active)
	{
		EnableCorrectItems();
		SetController();

		// Don't transition from hands to hands
		if ((prev != null && prev.items != null && prev.items.Count > 0) ||
			(active.items != null && active.items.Count > 0))
		{
			Animator.SetTrigger("Equip");
		}
	}

	void SetController()
	{
		if (_inventory.GetActiveSlot().items == null || _inventory.GetActiveSlot().items.Count == 0)
		{
			Animator.runtimeAnimatorController = _handsAnimatorController;
		}
		else
		{
			Animator.runtimeAnimatorController = _inventory.GetActiveSlot().items.Peek().animatorController;
		}
	}

	void EnableCorrectItems()
	{
		foreach (var slot in _inventory.Slots)
		{
			if (slot.items != null && slot.items.TryPeek(out Item item))
			{
				item.gameObject.SetActive(slot == _inventory.GetActiveSlot());
			}
		}
	}

    #endregion

    #region Animation Events

    public override void StartAttack_AnimationEvent()
    {
        Entity.EntityBeginAttack();
    }

    public override void TryAction_AnimationEvent(string actionTitle)
    {
		Vector3 functionPos = _firstPersonCamera.transform.position;

        AttackGroup chosenGroup;
        Attack attack;

        if (_inventory.GetActiveSlot().items.Count >= 1 &&
            _inventory.GetActiveSlot().items.Peek() is UseableItem usableItem)
		{

            // Determine attack data for weapons
            if (_inventory.GetActiveSlot().items.Peek() is WeaponItem weapon)
            {

                // Determine the attack group and chosen attack
                chosenGroup = weapon.ViewModelAttacks.GetAttackGroup(actionTitle);
                attack = weapon.ViewModelAttacks.GetAttackFromGroup(chosenGroup, currentChain);

                // Perform the attack data using the AttackData from the weapons ViewModels AttackList.
                PerformAttack(actionTitle, weapon, chosenGroup, attack, functionPos);
				return;
            }

            // Use normal UsableItem functionality
            usableItem.TryModelFunction(Entity as PlayerHealth, this, functionPos, actionTitle);
			prevAttackGroup = null;
        }
        else
        {
            // Use fists when unarmed
            chosenGroup = entityAttacks.GetAttackGroup(actionTitle);
            attack = entityAttacks.GetAttackFromGroup(chosenGroup, currentChain);

            PerformAttack(actionTitle, null, chosenGroup, attack, functionPos);
        }
    }

    private void PerformAttack(string actionTitle, WeaponItem weapon, AttackGroup chosenGroup, Attack attack, Vector3 attackPos)
    {
        // Compare the last attack group to the newly chosen one
        if (chosenGroup.GroupTitle != prevAttackGroup)
            currentChain = 0;

        if (weapon != null)
        {
            // Perform the attack data using the AttackData from the weapons ViewModels AttackList.
            weapon.TryModelFunction(Entity as PlayerHealth, this, attackPos, actionTitle, attack);
        }
        else
        {
            // Perform the attack data using the AttackData from the entities AttackList.
            Entity.EntityPerformOngoingAttack(this, attack.AttackData, attackPos, _firstPersonCamera.CameraTransform.forward);
        }

        // Increment the action chain, and loop back to zero if it reaches the end of the chain.
        SetActionChain((currentChain + 1) % chosenGroup.Attacks.Length);

        prevAttackGroup = chosenGroup.GroupTitle;
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
}
