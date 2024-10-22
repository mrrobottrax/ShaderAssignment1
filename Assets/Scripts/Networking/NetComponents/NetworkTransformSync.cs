using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkTransformSync : NetworkBehaviour
{

	// Transform update message

	[StructLayout(LayoutKind.Sequential)]
	internal class TransformUpdateMessage : ObjectUpdateMessageBase<NetworkTransformSync>
	{
		Vector3 m_position;
		Quaternion m_rotation;
		Vector3 m_scale;

		public TransformUpdateMessage(NetworkTransformSync component) : base(component)
		{
			m_position = component.transform.position;
			m_rotation = component.transform.rotation;
			m_scale = component.transform.localScale;
		}

		public override void ReceiveOnComponent(NetworkTransformSync component, Peer sender)
		{
			component.transform.position = m_position;
			component.transform.rotation = m_rotation;
			component.transform.localScale = m_scale;
		}
	}



	// /// <summary>
	// /// Allows the client to display the object with a different transformation
	// /// than what the owner says (good for client-predicted stuff like picking up an item).
	// /// </summary>
	public bool overrideTransform = false;

	Vector3 m_lastPos;
	Quaternion m_lastRot;
	Vector3 m_lastScale;

	private void Start()
	{
		if (IsOwner)
		{
			TickManager.OnTick += Tick;
		}
	}

	private void OnDestroy()
	{
		if (IsOwner)
		{
			TickManager.OnTick -= Tick;
		}
	}

	private void Tick()
	{
		if (transform.position != m_lastPos ||
			transform.rotation != m_lastRot ||
			transform.localScale != m_lastScale)
		{
			m_lastPos = transform.position;
			m_lastRot = transform.rotation;
			m_lastScale = transform.localScale;
			NetworkManager.BroadcastMessage(new TransformUpdateMessage(this));
		}
	}

	public override void AddSnapshotMessages(List<MessageBase> messages)
	{
		base.AddSnapshotMessages(messages);
		messages.Add(new TransformUpdateMessage(this));
	}
}