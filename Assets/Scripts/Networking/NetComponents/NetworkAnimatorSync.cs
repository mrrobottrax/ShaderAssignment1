using System;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkAnimatorSync : NetworkBehaviour
{
	Animator m_animator;

	[NetVar(nameof(OnRcvParams))] byte[] m_paramsBuffer;


	private void Awake()
	{
		m_animator = GetComponent<Animator>();

		m_paramsBuffer = new byte[m_animator.parameterCount * 4];
	}

	private void Update()
	{
		if (!IsOwner) return;

		if (TickManager.ShouldTick())
		{
			UpdateParameters();
		}
	}

	void UpdateParameters()
	{
		int index = 0;
		foreach (var param in m_animator.parameters)
		{
			switch (param.type)
			{
				case AnimatorControllerParameterType.Bool:
					ParamToBytes(m_animator.GetBool(param.nameHash), index);
					break;
				case AnimatorControllerParameterType.Float:
					ParamToBytes(m_animator.GetFloat(param.nameHash), index);
					break;
				case AnimatorControllerParameterType.Int:
					ParamToBytes(m_animator.GetInteger(param.nameHash), index);
					break;
				//case AnimatorControllerParameterType.Trigger:
				//	ParamToBytes(m_animator.GetBool(param.nameHash), index);
				//	break;
			}

			++index;
		}
	}

	void ParamToBytes(object paramValue, int index)
	{
		// Get parameters as binary

		GCHandle handle = GCHandle.Alloc(paramValue, GCHandleType.Pinned);
		try
		{
			IntPtr pValue = handle.AddrOfPinnedObject();
			Marshal.Copy(pValue, m_paramsBuffer, index * 4, 4);
		}
		finally
		{
			handle.Free();
		}
	}

	void OnRcvParams()
	{
		// Set parameters using binary data

		GCHandle handle = GCHandle.Alloc(m_paramsBuffer, GCHandleType.Pinned);
		try
		{
			int index = 0;
			foreach (var param in m_animator.parameters)
			{
				IntPtr pValue = handle.AddrOfPinnedObject() + 4 * index;

				switch (param.type)
				{
					case AnimatorControllerParameterType.Bool:
						bool bValue = Marshal.PtrToStructure<bool>(pValue);
						m_animator.SetBool(param.nameHash, bValue);
						break;
					case AnimatorControllerParameterType.Float:
						float fValue = Marshal.PtrToStructure<float>(pValue);
						m_animator.SetFloat(param.nameHash, fValue);
						break;
					case AnimatorControllerParameterType.Int:
						int iValue = Marshal.PtrToStructure<int>(pValue);
						m_animator.SetInteger(param.nameHash, iValue);
						break;
					case AnimatorControllerParameterType.Trigger:
						bool trigger = Marshal.PtrToStructure<bool>(pValue);
						if (trigger)
							m_animator.SetTrigger(param.nameHash);
						break;
				}

				++index;
			}
		}
		finally
		{
			handle.Free();
		}
	}
}