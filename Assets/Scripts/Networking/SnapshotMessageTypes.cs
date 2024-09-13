public enum ESnapshotMessageType : byte
{
	SceneChange,
	SpawnPrefab,
	RemoveGameObject,
	NetworkBehaviourUpdate,
	VoiceData
}

struct SceneChangeMessage
{
	public int m_sceneIndex;
	public int m_playerObjectID;
}

struct SpawnPrefabMessage
{
	public int m_networkID;
	public int m_prefabIndex;
	public int m_ownerID;
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