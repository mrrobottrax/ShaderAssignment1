using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : Weapon
{
	[SerializeField] float _throwForce = 10;
	PlayerInventory ownerInventory;

	protected override void PickUp(PlayerInteraction interactor)
	{
		base.PickUp(interactor);
		ownerInventory = interactor.GetComponent<PlayerInventory>();
	}

	public override void Drop()
	{
		base.Drop();
		ownerInventory = null;
	}

	public override void Fire1(bool pressed)
	{
		playerViewmodelManager.Animator.SetBool("PreparingThrow", pressed);
	}

	public override void Fire1_AnimationEvent()
	{
		ownerInventory.DropActiveItem(_throwForce, Quaternion.Euler(90, 0, 0));
	}
}
