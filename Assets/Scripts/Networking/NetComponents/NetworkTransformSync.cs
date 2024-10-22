using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkTransformSync : NetworkBehaviour
{

	// Transform update message

	[StructLayout(LayoutKind.Sequential)]
	internal class TransformUpdateMessage : ObjectUpdateMessageBase<NetworkTransformSync>
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		public TransformUpdateMessage(NetworkTransformSync component) : base(component)
		{
			position = component.transform.position;
			rotation = component.transform.rotation;
			scale = component.transform.localScale;
		}

		public override void ReceiveOnComponent(NetworkTransformSync component, Peer sender)
		{
			throw new System.NotImplementedException();
		}
	}



	// /// <summary>
	// /// Allows the client to display the object with a different transformation
	// /// than what the owner says (good for client-predicted stuff like picking up an item).
	// /// </summary>
	public bool overrideTransform = false;

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
		NetworkManager.BroadcastMessage(new TransformUpdateMessage(this));
	}

	public override void AddSnapshotMessages(List<MessageBase> messages)
	{
		base.AddSnapshotMessages(messages);
		messages.Add(new TransformUpdateMessage(this));
	}
}