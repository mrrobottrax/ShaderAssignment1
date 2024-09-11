using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkAnimatorSync : NetworkBehaviour
{
	Animator m_animator;

	AnimatorControllerParameter[] m_parameters;


	private void Awake()
	{
		m_animator = GetComponent<Animator>();

		m_parameters = m_animator.parameters;
	}

	private void Update()
	{
		if (TickManager.ShouldTick())
		{
			
		}
	}
}