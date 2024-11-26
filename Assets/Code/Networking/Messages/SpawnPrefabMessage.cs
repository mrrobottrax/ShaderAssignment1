using System.Runtime.InteropServices;
using Steamworks;

[StructLayout(LayoutKind.Sequential)]
internal class SpawnPrefabMessage : Message
{
	public int m_prefabID;
	public int m_netID;
	public SteamNetworkingIdentity m_ownerID;

	public SpawnPrefabMessage(NetworkObject obj)
	{
		m_prefabID = obj.m_prefabIndex;
		m_netID = obj.NetID;
		m_ownerID = obj.m_ownerIndentity;
	}

	public override void Receive(Peer sender)
	{
		NetworkObjectManager.SpawnNetworkPrefab(this, sender);
	}
}