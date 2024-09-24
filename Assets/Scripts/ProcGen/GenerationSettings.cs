using UnityEngine;

[CreateAssetMenu(fileName = "Proc Gen Settings", menuName = "ProcGen/Generation Settings")]
public class GenerationSettings : ScriptableObject
{
	[System.Serializable]
	public struct TileEntry
	{
		public ProcGenTile m_prefab;
		public float m_weight;
	}

	public TileEntry[] m_tiles;

	public int m_width;
	public int m_depth;
}
