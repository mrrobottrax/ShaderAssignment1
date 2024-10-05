using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : Weapon
{
	[SerializeField] float _throwForce = 10;
	[SerializeField] Vector3 _throwOffset = new(0, 0, 0.3f);

	public override void Fire1(bool pressed)
	{
		playerViewmodelManager.Animator.SetBool("PreparingThrow", pressed);
	}

	public override void Fire1_AnimationEvent()
	{
		ownerInventory.DropActiveItem(_throwForce, _throwOffset, Quaternion.Euler(90, 0, 0));
	}
}
