public abstract class Weapon : Item
{
	protected PlayerViewmodelManager playerViewmodelManager;

	public abstract void Fire1(bool pressed);
	public virtual void Fire2(bool pressed) { }

	public virtual void Fire1_AnimationEvent() { }
	public virtual void Fire2_AnimationEvent() { }

	protected override void PickUp(PlayerInteraction interactor)
	{
		base.PickUp(interactor);
		playerViewmodelManager = interactor.GetComponentInChildren<PlayerViewmodelManager>();
	}
}
