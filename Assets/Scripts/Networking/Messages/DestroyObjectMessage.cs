using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
class DestroyObjectMessage : MessageBase
{
	int m_networkID;

	public DestroyObjectMessage(NetworkObject networkObject)
	{
		m_networkID = networkObject.m_netID;
	}

	public override void Receive(Peer sender)
	{
		Object.Destroy(NetworkObjectManager.GetNetworkObject(m_networkID).gameObject);
	}
}