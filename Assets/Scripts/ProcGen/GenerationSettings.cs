using UnityEngine;

[CreateAssetMenu(fileName = "Proc Gen Settings", menuName = "ProcGen/Generation Settings")]
public class GenerationSettings : ScriptableObject
{
	[SerializeField] ProcGenTile[] m_tiles;
}
