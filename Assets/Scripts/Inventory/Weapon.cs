using System.Collections.Generic;
using UnityEngine;

public class Weapon : Item
{
    [field: Header("Weapon Data")]
    [field: SerializeField] public int BaseDamage { get; private set; }
    [field: SerializeField] public float BaseWeaponRange { get; private set; }


    [field: Header("Functions")]
    [field: SerializeField] public AttackList ViewModelAttacks { get; private set; }

    protected Dictionary<string, ViewModelAction> functions = new Dictionary<string, ViewModelAction>();// Add a view models functions to this dictionary


    // System
    protected PlayerViewmodelManager playerViewmodelManager;

	public virtual void Fire1(bool pressed)
	{
		if (!pressed) return;
		playerViewmodelManager.Animator.SetTrigger("Fire1");
	}

	public virtual void Fire2(bool pressed)
	{
		if (!pressed) return;
		playerViewmodelManager.Animator.SetTrigger("Fire2");
	}

	public virtual void Fire1_AnimationEvent() { }
	public virtual void Fire2_AnimationEvent() { }

    #region ViewModel Functionality

    /// <summary>
    /// This method executes a view model's function based on the action title passed in.
    /// </summary>
    /// <param name="player">The player object performing the action.</param>
    /// <param name="viewModelManager">The view model manager handling the action.</param>
    /// <param name="weaponItem">The weapon item involved in the action.</param>
    /// <param name="attack">The attack data for the action.</param>
    /// <param name="actionTitle">The title of the action to execute.</param>
    public virtual void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, AttackList.Attack attack, string actionTitle) { }
    #endregion

    protected override void PickUp(PlayerInteraction interactor)
	{
		base.PickUp(interactor);
		playerViewmodelManager = interactor.GetComponentInChildren<PlayerViewmodelManager>();
	}
}