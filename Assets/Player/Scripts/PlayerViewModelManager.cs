using UnityEngine;
using UnityEngine.InputSystem;
using static AttackList;

public class PlayerViewmodelManager : EntityAnimationManager_Base, IInputHandler
{
	[field: Header("Components")]
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
	}

	private void Start()
	{
		if (IsOwner)
		{
			SetControlsSubscription(true);

			_inventory.OnAddItem += OnAddItem;
			_inventory.OnDropItem += OnDropItem;
			_inventory.OnActiveSlotChange += OnActiveSlotChange;
		}
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

		if (_inventory.GetActiveSlot().items.TryPeek(out Item item))
		{
			if (item is UseableItem)
			{
				if (fired)
					(item as UseableItem).OnFire1Pressed();
				else
					(item as UseableItem).OnFire1Released();
			}
		}
	}

	void Fire2(InputAction.CallbackContext ctx)
	{
		// Ensure the player is not already attacking before triggering
		if (!Entity.IsAbleToAttack) return;

		bool fired = ctx.ReadValueAsButton();

		if (_inventory.GetActiveSlot().items.TryPeek(out Item item))
		{
			if (item is UseableItem)
			{
				if (fired)
					(item as UseableItem).OnFire2Pressed();
				else
					(item as UseableItem).OnFire2Released();
			}
		}
	}
	#endregion

	#region Animator State Methods

	void PutItemInHands(Item item, bool putInHands)
	{
		item.interactionEnabled = !putInHands;

		// Follow hand
		if (putInHands)
		{
            Vector3 localPos = Vector3.zero;
            Quaternion localRot = Quaternion.identity;

            if (item is UseableItem usableItem)
            {
                localPos += usableItem.HoldOffset;
                localRot = Quaternion.Euler(usableItem.HoldRotation);
            }

            item.transform.parent = _handTransform;
            item.transform.SetLocalPositionAndRotation(localPos, localRot);
        }
		else
		{
			item.transform.parent = null;
		}

		// Disable/enable physics
		Rigidbody[] rbs = item.GetComponentsInChildren<Rigidbody>();
		foreach (var rb in rbs)
		{
			if (!putInHands && rb.gameObject.TryGetComponent(out NetworkRigidbody netRB))
			{
				// Only set to non-kinematic when owned
				if (netRB.IsOwner) rb.isKinematic = false;
			}
			else
			{
				rb.isKinematic = putInHands;
			}

			rb.detectCollisions = !putInHands;
			rb.interpolation = putInHands ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
		}

        // Toggle shadow casting
        if (item.TryGetComponent(out MeshRenderer meshRenderer))
            meshRenderer.shadowCastingMode = putInHands ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;

        // Enable/disable net sync
        NetworkTransformSync[] trans = item.GetComponentsInChildren<NetworkTransformSync>();
		foreach (var t in trans)
		{
			t.OverrideTransform = putInHands;
		}
	}

	void OnAddItem(Item item, InventorySlot slot)
	{
		PutItemInHands(item, true);

		EnableCorrectItems();
		SetAnimationController();

		if (slot == _inventory.GetActiveSlot())
			Animator.SetTrigger("Equip");
	}

	void OnDropItem(Item item, InventorySlot slot)
	{
		PutItemInHands(item, false);

		EnableCorrectItems();
		SetAnimationController();

		if (slot == _inventory.GetActiveSlot())
			Animator.SetTrigger("Equip");
	}

	void OnActiveSlotChange(InventorySlot prev, InventorySlot active)
	{
		EnableCorrectItems();
		SetAnimationController();

		// Don't transition from hands to hands
		if ((prev != null && prev.items != null && prev.items.Count > 0) ||
			(active.items != null && active.items.Count > 0))
		{
			Animator.SetTrigger("Equip");
		}
	}

	void SetAnimationController()
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
        // Ensure the player is holding a usable item
        if (_inventory.GetActiveSlot().items.Count >= 1 && _inventory.GetActiveSlot().items.Peek() is UseableItem)
            Entity.EntityBeginAttack();
    }

	public override void TryAction_AnimationEvent(string actionTitle)
	{
		Vector3 functionPos = _firstPersonCamera.transform.position;
		AttackGroup chosenGroup;
		AttackData attack;

        if (_inventory.GetActiveSlot().items.Count >= 1 && _inventory.GetActiveSlot().items.Peek() is UseableItem usableItem)
		{
			if (_inventory.GetActiveSlot().items.Peek() is WeaponItem weapon)
			{
				chosenGroup = weapon.ViewModelAttacks.GetAttackGroup(actionTitle);
				attack = weapon.ViewModelAttacks.GetAttackFromGroup(chosenGroup, currentChain);

				UseWeaponItem(weapon, actionTitle, chosenGroup, attack, functionPos);

				return;
			}

			// Use normal UsableItem functionality
			usableItem.TryItemFunction(Entity as PlayerStats, this, functionPos, actionTitle);
            prevAttackGroup = null;
		}
	}

	private void UseWeaponItem(WeaponItem weapon, string actionTitle, AttackGroup chosenGroup, AttackData attack, Vector3 attackPos)
	{
        if (weapon != null)
		{
            Debug.Log(actionTitle);

            // Compare the last attack group to the newly chosen one
            if (chosenGroup.GroupTitle != prevAttackGroup)
                currentChain = 0;

            weapon.TryItemFunction(Entity as PlayerStats, this, attackPos, actionTitle, attack);

            // Increment the action chain
            SetActionChain((currentChain + 1) % chosenGroup.Attacks.Length);
            prevAttackGroup = chosenGroup.GroupTitle;
        }
	}

	/// <summary>
	/// Sets the action chain value used in view model animations.
	/// </summary>
	private void SetActionChain(int value)
	{
		currentChain = value;

		// Set the chain value on the animator
		if (HasParameter("ViewModelActionChain"))
			Animator.SetInteger("ViewModelActionChain", value);
	}

	#endregion
}
