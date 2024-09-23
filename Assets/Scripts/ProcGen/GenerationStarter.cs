using UnityEngine;

public class GenerationStarter : MonoBehaviour
{
	[SerializeField] GenerationSettings m_settings;
	[SerializeField] Transform m_nextPieceAnchor;

	private void Start()
	{
		GenerateMap();
	}

	public void GenerateMap()
	{

	}
}
