public class Weapon : Item
{
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

	protected override void PickUp(PlayerInteraction interactor)
	{
		base.PickUp(interactor);
		playerViewmodelManager = interactor.GetComponentInChildren<PlayerViewmodelManager>();
	}
}
