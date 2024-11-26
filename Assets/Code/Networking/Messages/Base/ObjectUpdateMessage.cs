using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public abstract class ObjectUpdateMessage : Message
{
	public int m_objectNetID;

	public ObjectUpdateMessage(NetworkObject obj)
	{
		m_objectNetID = obj.NetID;
	}

	public override void Receive(Peer sender)
	{
		var obj = NetworkObjectManager.GetNetworkObject(m_objectNetID);

		ReceiveOnObject(obj, sender);
	}

	public abstract void ReceiveOnObject(NetworkObject obj, Peer sender);
}