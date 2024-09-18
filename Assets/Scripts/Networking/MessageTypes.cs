using Steamworks;

public enum EMessageType : byte
{
	SceneChange,
	SpawnPrefab,
	RemoveGameObject,
	NetworkBehaviourUpdate,
	VoiceData,
	AddPeer,
}

struct SceneChangeMessage
{
	public int m_sceneIndex;
}

struct SpawnPrefabMessage
{
	public int m_prefabIndex;
	public int m_networkID;
	public SteamNetworkingIdentity m_ownerIdentity;
}

struct RemoveObjectMessage
{
	public int m_networkID;
}

struct NetworkBehaviourUpdateMessage
{
	public int m_networkID;
	public int m_componentIndex;
}

struct AddPeerMessage
{
	public SteamNetworkingIdentity m_steamIdentity;
	public int m_networkID;
}

struct VoiceDataMessage
{
	public bool m_isSeperate;
}