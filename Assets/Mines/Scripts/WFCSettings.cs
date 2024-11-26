using UnityEngine;

[CreateAssetMenu(menuName = "WFC/Settings")]
public class WFCSettings : ScriptableObject
{
	public WFCTilePack m_TilePack;

	public int m_Width = 20;
	public int m_Depth = 20;
	public int m_Height = 20;
}