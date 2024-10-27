using UnityEngine;

public class UseableItem : Item
{
	// System
	protected PlayerViewmodelManager playerViewmodelManager;

	#region Item Functionality

	protected override void PickUp(PlayerInteraction interactor)
	{
		base.PickUp(interactor);
		playerViewmodelManager = interactor.GetComponentInChildren<PlayerViewmodelManager>();
	}

	public virtual void OnFire1Pressed()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire1"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire1", true);

		if (playerViewmodelManager.HasParameter("Fire1"))
			playerViewmodelManager.Animator.SetTrigger("Fire1");
	}
	public virtual void OnFire1Released()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire1"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire1", false);
	}

	public virtual void OnFire2Pressed()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire2"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire2", true);

		if (playerViewmodelManager.HasParameter("Fire2"))
			playerViewmodelManager.Animator.SetTrigger("Fire2");
	}
	public virtual void OnFire2Released()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire2"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire2", false);
	}

	#endregion

	#region ViewModel Functionality

	/// <summary>
	/// This method executes a view model's function based on the action title passed in.
	/// </summary>
	public virtual void TryModelFunction(PlayerStats player, PlayerViewmodelManager viewModelManager, Vector3 functionPos, string actionTitle, AttackList.Attack attack = null) { }
	#endregion
}
