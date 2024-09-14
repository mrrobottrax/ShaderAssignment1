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

		TickManager.OnTick += Tick;
	}

	private void OnDestroy()
	{
		TickManager.OnTick -= Tick;
	}

	private void Tick()
	{
		if (!IsOwner) return;

		UpdateParameters();
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