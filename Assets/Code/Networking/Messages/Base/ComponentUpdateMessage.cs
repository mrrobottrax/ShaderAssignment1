using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public abstract class ComponentUpdateMessage<T> : ObjectUpdateMessage where T : NetworkBehaviour
{
	public int m_objectComponentIndex;

	public ComponentUpdateMessage(T component) : base(component.NetObj)
	{
		m_objectComponentIndex = component.m_index;
	}

	public override void Receive(Peer sender)
	{
		var obj = NetworkObjectManager.GetNetworkObject(m_objectNetID);
		var comp = obj.NetworkBehaviours[m_objectComponentIndex];

		ReceiveOnComponent((T)comp, sender);
	}

	public abstract void ReceiveOnComponent(T component, Peer sender);

	public override void ReceiveOnObject(NetworkObject obj, Peer sender)
	{ }
}