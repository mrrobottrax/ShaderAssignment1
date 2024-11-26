using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


#region Transform update message

[StructLayout(LayoutKind.Sequential)]
internal class TransformUpdateMessage : ComponentUpdateMessage<NetworkTransformSync>
{
	Vector3 m_position;
	Quaternion m_rotation;
	//Vector3 m_scale;

	public TransformUpdateMessage(NetworkTransformSync component) : base(component)
	{
		component.transform.GetPositionAndRotation(out m_position, out m_rotation);
		//m_scale = component.transform.localScale;
	}

	public override void ReceiveOnComponent(NetworkTransformSync component, Peer sender)
	{
		if (!component.OverrideTransform)
			component.transform.SetPositionAndRotation(m_position, m_rotation);
		//component.transform.localScale = m_scale;
	}
}

#endregion

public class NetworkTransformSync : NetworkBehaviour
{
	// /// <summary>
	// /// Allows the client to display the object with a different transformation
	// /// than what the owner says (good for client-predicted stuff like picking up an item).
	// /// </summary>
	public bool OverrideTransform = false;

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
		TickManager.OnTick -= Tick;
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

	public override void AddSnapshotMessages(List<Message> messages)
	{
		base.AddSnapshotMessages(messages);
		messages.Add(new TransformUpdateMessage(this));
	}
}