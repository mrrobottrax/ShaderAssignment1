using Steamworks;

public enum ESteamNetworkingSend : int
{
	k_nSteamNetworkingSend_Unreliable = 0,
	k_nSteamNetworkingSend_NoNagle = 1,

	k_nSteamNetworkingSend_UnreliableNoNagle = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoNagle,

	k_nSteamNetworkingSend_NoDelay = 4,

	k_nSteamNetworkingSend_UnreliableNoDelay = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoDelay | k_nSteamNetworkingSend_NoNagle,

	k_nSteamNetworkingSend_Reliable = 8,

	k_nSteamNetworkingSend_ReliableNoNagle = k_nSteamNetworkingSend_Reliable | k_nSteamNetworkingSend_NoNagle
}

public class Peer
{
	internal HSteamNetConnection m_hConn;
	internal SteamNetworkingIdentity m_identity;
	internal NetworkObject m_player;
	internal bool m_loading = true;

	public Peer(HSteamNetConnection hConn, SteamNetworkingIdentity identity, NetworkObject player)
	{
		m_hConn = hConn;
		m_identity = identity;
		m_player = player;
	}

	public void UpdateConnection(HSteamNetConnection hConn)
	{
		m_hConn = hConn;
	}

	// Send all queued messages
	public void FlushQueuedMessages()
	{
		SteamNetworkingSockets.FlushMessagesOnConnection(m_hConn);
	}
}