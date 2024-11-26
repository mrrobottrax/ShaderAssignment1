using System;
using UnityEngine;

public enum WFCDoorwayType
{
	Closed,
	Normal4x5
}

[Serializable]
public struct WFCTileFaceConnectionData
{
	public bool m_InvertBannedTiles;
	public WFCTileData[] m_BannedAdjacentTiles;

	public WFCDoorwayType m_DoorwayType;
}

public enum WFCDirection
{
	North,
	South,
	East,
	West,

	Up,
	Down
}

public class WFCTileData : MonoBehaviour
{
	public float m_RelativeWeight = 1;
	public int m_Size = 1;

	[HideInInspector]
	public WFCTileFaceConnectionData[] m_ConnectionData;


	/// <summary>
	/// Get a subset of all connection points facing a certain direction
	/// </summary>
	public ArraySegment<WFCTileFaceConnectionData> GetConnectionDataSubset(WFCDirection facingDirection)
	{
		if (m_ConnectionData == null) return null;

		int dataElementsPerFace = m_ConnectionData.Length / 6;

		return new ArraySegment<WFCTileFaceConnectionData>(m_ConnectionData, (int)facingDirection * dataElementsPerFace, dataElementsPerFace);
	}

	public WFCTileFaceConnectionData GetConnectionDataForSubTile(WFCDirection direction, int subTileIndexX, int subTileIndexY, int subTileIndexZ)
	{
		var facingSubset = GetConnectionDataSubset(direction);

		int right;
		int up;

		switch (direction)
		{
			case WFCDirection.North:
				right = subTileIndexX;
				up = subTileIndexY;
				break;
			case WFCDirection.South:
				right = subTileIndexX;
				up = subTileIndexY;
				break;

			case WFCDirection.East:
				right = subTileIndexZ;
				up = subTileIndexY;
				break;
			case WFCDirection.West:
				right = subTileIndexZ;
				up = subTileIndexY;
				break;

			case WFCDirection.Up:
				right = subTileIndexX;
				up = subTileIndexZ;
				break;
			case WFCDirection.Down:
				right = subTileIndexX;
				up = subTileIndexZ;
				break;

			default:
				throw new Exception("Direction error");
		}

		int index = right + up * m_Size;

		return facingSubset[index];
	}
}