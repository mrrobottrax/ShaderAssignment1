using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkAnimator : NetworkBehaviour
{
	Animator m_animator;

	byte[] m_paramsBuffer;

	Dictionary<int, int> m_paramIndices;
	List<int> m_triggers;

	private void Awake()
	{
		m_animator = GetComponent<Animator>();
		m_paramsBuffer = new byte[m_animator.parameterCount * 4];

		m_paramIndices = new Dictionary<int, int>();
		m_triggers = new List<int>();
		for (int i = 0; i < m_animator.parameterCount; ++i)
		{
			m_paramIndices.Add(m_animator.parameters[i].nameHash, i);

			if (m_animator.parameters[i].type == AnimatorControllerParameterType.Trigger)
			{
				m_triggers.Add(m_animator.parameters[i].nameHash);
			}
		}
	}

	private void Start()
	{
		if (IsOwner)
		{
			TickManager.OnLateTick += ResetTriggers;
		}
	}

	private void OnDestroy()
	{
		if (IsOwner)
		{
			TickManager.OnLateTick -= ResetTriggers;
		}
	}

	void ResetTriggers()
	{
		foreach (var triggerID in m_triggers)
		{
			CopyIntoBuffer(triggerID, false);
		}
	}

	void CopyIntoBuffer(int id, object value)
	{
		IntPtr p = Marshal.AllocHGlobal(4);
		try
		{
			Marshal.StructureToPtr(value, p, false);
			Marshal.Copy(p, m_paramsBuffer, m_paramIndices[id] * 4, 4);
		}
		finally
		{
			Marshal.FreeHGlobal(p);
		}
	}

	public void SetInt(int id, int value)
	{
		m_animator.SetInteger(id, value);
		CopyIntoBuffer(id, value);
	}

	public void SetFloat(int id, float value)
	{
		m_animator.SetFloat(id, value);
		CopyIntoBuffer(id, value);
	}

	public void SetBool(int id, bool value)
	{
		m_animator.SetBool(id, value);
		CopyIntoBuffer(id, value);
	}

	public void SetTrigger(int id)
	{
		m_animator.SetTrigger(id);
		CopyIntoBuffer(id, true);
	}


	public void SetInt(string name, int value)
	{
		SetInt(Animator.StringToHash(name), value);
	}

	public void SetFloat(string name, float value)
	{
		SetFloat(Animator.StringToHash(name), value);
	}
	public void SetBool(string name, bool value)
	{
		SetBool(Animator.StringToHash(name), value);
	}

	public void SetTrigger(string name)
	{
		SetTrigger(Animator.StringToHash(name));
	}

	void OnRcvParams()
	{
		// Set parameters using binary data

		int index = 0;
		foreach (var param in m_animator.parameters)
		{
			ArraySegment<byte> paramBytes = new(m_paramsBuffer, 4 * index, 4);

			switch (param.type)
			{
				case AnimatorControllerParameterType.Bool:
					bool bValue = BitConverter.ToBoolean(paramBytes);
					m_animator.SetBool(param.nameHash, bValue);
					break;
				case AnimatorControllerParameterType.Float:
					float fValue = BitConverter.ToSingle(paramBytes);
					m_animator.SetFloat(param.nameHash, fValue);
					break;
				case AnimatorControllerParameterType.Int:
					int iValue = BitConverter.ToInt32(paramBytes);
					m_animator.SetInteger(param.nameHash, iValue);
					break;
				case AnimatorControllerParameterType.Trigger:
					bool trigger = BitConverter.ToBoolean(paramBytes);
					if (trigger)
						m_animator.SetTrigger(param.nameHash);
					break;
			}

			++index;
		}
	}
}