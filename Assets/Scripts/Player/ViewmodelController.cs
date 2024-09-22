using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ViewmodelController : MonoBehaviour
{
	[SerializeField] Animator m_animator;

	private void Awake()
	{
		Assert.IsNotNull(m_animator);
	}

	private void Start()
	{
		m_animator.SetBool("IsReady", true);
		m_animator.SetBool("IsHoldingNothing", true);
	}
}
