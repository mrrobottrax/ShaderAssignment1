public abstract class MessageBase
{
	public virtual bool Peer2Peer { get { return false; } }
	public virtual bool Reliable { get { return true; } }

	public enum EMessageFilter
	{
		All,
		ClientOnly,
		HostOnly
	}
	public virtual EMessageFilter Filter { get { return EMessageFilter.All; } }

	internal abstract void Receive(Peer sender);
}