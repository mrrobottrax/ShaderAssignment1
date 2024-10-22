using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public abstract class ObjectUpdateMessageBase<T> : MessageBase where T : NetworkBehaviour
{
	int m_objectNetID;
	int m_objectComponentIndex;

	public ObjectUpdateMessageBase(T component)
	{
		m_objectComponentIndex = component.m_index;
		m_objectNetID = component.m_object.m_netID;
	}

	public override void Receive(Peer sender)
	{
		var obj = NetworkObjectManager.GetNetworkObject(m_objectNetID);
		var comp = obj.m_networkBehaviours[m_objectComponentIndex];

		ReceiveOnComponent((T)comp, sender);
	}

	public abstract void ReceiveOnComponent(T component, Peer sender);
}