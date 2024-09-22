using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkAnimatorSync : NetworkBehaviour
{
	[SerializeField] private Animator m_animator;

	[NetVar(nameof(OnRcvParams))] byte[] m_paramsBuffer;


	private void Awake()
	{
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
			byte[] bytes;

			switch (param.type)
			{
				case AnimatorControllerParameterType.Bool:
					bytes = BitConverter.GetBytes(m_animator.GetBool(param.nameHash));
					CopyParamToBuffer(bytes, index);
					break;
				case AnimatorControllerParameterType.Float:
					bytes = BitConverter.GetBytes(m_animator.GetFloat(param.nameHash));
					CopyParamToBuffer(bytes, index);
					break;
				case AnimatorControllerParameterType.Int:
					bytes = BitConverter.GetBytes(m_animator.GetInteger(param.nameHash));
					CopyParamToBuffer(bytes, index);
					break;
					//case AnimatorControllerParameterType.Trigger:
					//	ParamToBytes(m_animator.GetBool(param.nameHash), index);
					//	break;
			}

			++index;
		}
	}

	void CopyParamToBuffer(byte[] bytes, int index)
	{
		Array.Copy(bytes, 0, m_paramsBuffer, index * 4, bytes.Length);
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
				//case AnimatorControllerParameterType.Trigger:
				//	bool trigger = Marshal.PtrToStructure<bool>(pValue);
				//	if (trigger)
				//		m_animator.SetTrigger(param.nameHash);
				//	break;
			}

			++index;
		}
	}
}