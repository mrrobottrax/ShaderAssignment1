using UnityEngine;

public class PlayerViewmodelManager : EntityAnimationManager_Base
{
	[field: Header("Components")]
    public Animator Animator { get; private set; }
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private RuntimeAnimatorController _handsAnimatorController;
    [SerializeField] private FirstPersonCamera _firstPersonCamera;

    [field: Header("Transforms")]
    [field: SerializeField] private Transform _attackPos;
    [field: SerializeField] private Transform _handTransform;

	// System
    private string prevAttackGroup;
    private int currentChain = 0;// The current increment in an attack chain

    #region Initialization Methods

    private void Awake()
	{
		Animator = GetComponent<Animator>();

		_inventory.OnAddItem += OnAddItem;
		_inventory.OnDropItem += OnDropItem;
		_inventory.OnActiveSlotChange += OnActiveSlotChange;
	}
	#endregion

	#region Unity Callbacks

	private void OnDestroy()
	{
		_inventory.OnAddItem -= OnAddItem;
		_inventory.OnDropItem -= OnDropItem;
		_inventory.OnActiveSlotChange -= OnActiveSlotChange;
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

    public void Fire1_AnimationEvent()
	{
		if (_inventory.GetActiveSlot().items.Count == 0) return;

		if (_inventory.GetActiveSlot().items.Peek() is Weapon weapon)
			weapon.Fire1_AnimationEvent();
	}

	public void Fire2_AnimationEvent()
	{
		if (_inventory.GetActiveSlot().items.Count == 0) return;

		if (_inventory.GetActiveSlot().items.Peek() is Weapon weapon)
			weapon.Fire2_AnimationEvent();
	}

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
		if (_inventory.GetActiveSlot().items.Peek() is Weapon weapon)
		{
            // Determine the attack group and chosen attack
            chosenGroup = weapon.ViewModelAttacks.GetAttackGroup(attackGroup);
            attack = weapon.ViewModelAttacks.GetAttackFromGroup(chosenGroup, currentChain);

            // Compare the last attack group to the newly chosen one
            if (chosenGroup.GroupTitle != prevAttackGroup)
                currentChain = 0;

            // Perform the attack data using the AttackData from the weapons ViewModels AttackList.
            weapon.TryModelFunction(Entity as PlayerHealth, this, _attackPos.position, attack, attackGroup);

            // Set the attack chain length to the number of attacks in the chosen attack group
            attackChainLength = chosenGroup.Attacks.Length;

            // Increment the action chain, and loop back to zero if it reaches the end of the chain.
            SetActionChain((currentChain + 1) % attackChainLength);

            // Set the prev attack group
            prevAttackGroup = chosenGroup.GroupTitle;
        }
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
