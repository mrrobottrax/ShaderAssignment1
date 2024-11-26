using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
internal class LoadedInMessage : Message
{
	public override EMessageFilter Filter => EMessageFilter.HostOnly;

	public override void Receive(Peer sender)
	{
		bool noneLoading = true;
		foreach (var peer in NetworkManager.GetAllPeers())
		{
			if (peer.m_loading)
			{
				noneLoading = false;
				break;
			}
		}

		// Wait for all peers to load
		if (noneLoading)
		{
			NetworkManager.m_host.m_waitingForPeers = false;
			Time.timeScale = 1;
		}
	}
}