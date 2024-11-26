using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
class DestroyObjectMessage : ObjectUpdateMessage
{
	public DestroyObjectMessage(NetworkObject networkObject) : base(networkObject)
	{ }

	public override void ReceiveOnObject(NetworkObject obj, Peer sender)
	{
		Object.Destroy(NetworkObjectManager.GetNetworkObject(m_objectNetID).gameObject);
	}
}