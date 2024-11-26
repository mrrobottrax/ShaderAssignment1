using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public abstract class Message
{
	public virtual bool Peer2Peer { get { return false; } }
	public virtual bool Reliable { get { return true; } }

	/// <summary>
	/// Determines who can receive the message
	/// </summary>
	public enum EMessageFilter
	{
		All,
		ClientOnly,
		HostOnly
	}
	public virtual EMessageFilter Filter { get { return EMessageFilter.All; } }

	public abstract void Receive(Peer sender);
}