using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetworkAnimator : NetworkBehaviour
{
	Animator m_animator;

	private void Awake()
	{
		m_animator = GetComponent<Animator>();
	}

	#region Update Methods

	public void SetInt(int id, int value)
	{
		m_animator.SetInteger(id, value);
		BroadcastUpdate(
			new AnimatorParameterUpdateMessage(
			this,
			id,
			AnimatorParameterUpdateMessage.ParamType.Int,
			value));
	}

	public void SetFloat(int id, float value)
	{
		m_animator.SetFloat(id, value);
		BroadcastUpdate(
			new AnimatorParameterUpdateMessage(
			this,
			id,
			AnimatorParameterUpdateMessage.ParamType.Float,
			value));
	}

	public void SetBool(int id, bool value)
	{
		m_animator.SetBool(id, value);
		BroadcastUpdate(
			new AnimatorParameterUpdateMessage(
			this,
			id,
			AnimatorParameterUpdateMessage.ParamType.Bool,
			value ? 1 : 0));
	}

	public void SetTrigger(int id)
	{
		m_animator.SetTrigger(id);
		BroadcastUpdate(
			new AnimatorParameterUpdateMessage(
			this,
			id,
			AnimatorParameterUpdateMessage.ParamType.Trigger,
			1));
	}

	#endregion

	#region String Update Methods

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

	#endregion


	#region Message Class
	// Send parameter updates
	[StructLayout(LayoutKind.Sequential)]
	class AnimatorParameterUpdateMessage : ComponentUpdateMessage<NetworkAnimator>
	{
		readonly int m_ParamID;

		public enum ParamType
		{
			Float,
			Int,
			Bool,
			Trigger
		}
		readonly ParamType m_ParamType;
		readonly float m_Value;

		public AnimatorParameterUpdateMessage(NetworkAnimator component, int paramID, ParamType type, float value) : base(component)
		{
			m_ParamID = paramID;
			m_ParamType = type;
			m_Value = value;
		}

		public override void ReceiveOnComponent(NetworkAnimator component, Peer sender)
		{
			switch (m_ParamType)
			{
				case ParamType.Float:
					component.m_animator.SetFloat(m_ParamID, m_Value);
					break;
				case ParamType.Int:
					component.m_animator.SetInteger(m_ParamID, (int)m_Value);
					break;
				case ParamType.Bool:
					component.m_animator.SetBool(m_ParamID, m_Value != 0);
					break;
				case ParamType.Trigger:
					component.m_animator.SetTrigger(m_ParamID);
					break;
			}
		}
	}
	#endregion
}