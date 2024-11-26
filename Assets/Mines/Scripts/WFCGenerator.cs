using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Variable size wave fuction collapse
/*
	Initialize:
		Add all tiles that aren't obviously invalid to the superposition
			-Can't extend out of bounds
			-Can't connect to out of bounds
		Big tiles get added as a sub tile in each space it would take up

	Iteration:
		Collapse the tile with the least possibilities (may be 0)
		If this is a big tile, make sure to collapse all sub tiles
		Propagate the results:
			All tiles along the border get their properties updated
				-They must connect to the collapsed tile
				-They must not collide with the collapsed tile
			If the updated tile is a big tile, make sure to remove all sub tiles when one becomes invalid

		Repeat

	Notes:
		Connections to NULL tiles are allowed for now. Generate a wall when this happens.
		This constraint may be removed, but there's a possibility it could cause situations where no collapse is possible.

		Floating tiles that can't pathfind back to the entrance are removed (maybe we could generate a collapsed path to them?).

*/

public class WFCGenerator : MonoBehaviour
{
	[SerializeField] WFCSettings m_Settings;
	[SerializeField] WFCDoorwayType m_EntranceDoorType = WFCDoorwayType.Normal4x5;

	enum WFCRotation
	{
		None,
		CL_90,
		CL_180,
		CL_270,

		Max
	}

	struct Tile
	{
		public WFCTileData m_ParentTile;
		public WFCRotation m_Rotation;

		public int m_SubTileIndexX;
		public int m_SubTileIndexY;
		public int m_SubTileIndexZ;
	}

	struct TileSuperPosition
	{
		public HashSet<Tile> m_PossibleTiles;
	}

	Tile[,,] m_PlacedTiles;
	TileSuperPosition[,,] m_SuperPositionTiles;

	void Start()
	{
		Run();
	}

	#region Algorithm

	[ContextMenu("Run")]
	public void Run()
	{
		Setup();
		while (Step()) ;
	}

	[ContextMenu("Setup")]
	public void Setup()
	{
		ClearChildren();

		m_PlacedTiles = new Tile[m_Settings.m_Width, m_Settings.m_Depth, m_Settings.m_Height];
		m_SuperPositionTiles = new TileSuperPosition[m_Settings.m_Width, m_Settings.m_Depth, m_Settings.m_Height];

		SetupInitialPossibilities();
	}

	[ContextMenu("Step")]
	public bool Step()
	{
		Vector3Int lowestEntropyPos = GetLowestEntropyTile(out bool done);

		if (done)
		{
			return false;
		}

		//Debug.DrawRay(GetTileWorldPosition(lowestEntropyPos.x, lowestEntropyPos.y, lowestEntropyPos.z), Vector3.up, Color.blue, 15);

		// Special case for collapsing with no possibilities
		if (m_SuperPositionTiles[lowestEntropyPos.x, lowestEntropyPos.z, lowestEntropyPos.y].m_PossibleTiles.Count == 0)
		{
			m_SuperPositionTiles[lowestEntropyPos.x, lowestEntropyPos.z, lowestEntropyPos.y].m_PossibleTiles = null;

			Debug.LogWarning("Collapsed to NULL, TBD if this is okay or not. Also reminder to generate a dead end when this happens.");
			return true;
		}

		WFCTileData rootTile = CollapseTile(lowestEntropyPos, out Vector3Int rootPos, out WFCRotation rotation);

		// Spawn prefab
		Vector3 pos = GetTileWorldPosition(rootTile, rootPos, rotation);
		Instantiate(rootTile, pos, Quaternion.Euler(0, RotationToNum(rotation) * 90, 0), transform);

		PropagateTile(rootTile, rootPos, rotation);

		return true;
	}

	void PropagateInDirection(WFCTileData tile, Vector3Int root, WFCDirection direction, WFCRotation rotation)
	{
		Vector3Int forward = DirectionAsVector(direction);

		// Extend from tile root
		Vector3Int pos = root;
		if (IntDot(forward, Vector3Int.one) == 1) // check if facing a positive of negative direction
		{
			pos += forward * (tile.m_Size - 1);
		}

		// Count is m_Size in any direction but forwards
		Vector3Int absForward = new(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z));
		int xCount = (1 - absForward.x) * tile.m_Size + absForward.x;
		int yCount = (1 - absForward.y) * tile.m_Size + absForward.y;
		int zCount = (1 - absForward.z) * tile.m_Size + absForward.z;

		// Loop over all faces in this direction
		for (int x = 0; x < xCount && x >= 0 && x < m_Settings.m_Width; ++x)
		{
			for (int y = 0; y < yCount && y >= 0 && x < m_Settings.m_Height; ++y)
			{
				for (int z = 0; z < zCount && z >= 0 && x < m_Settings.m_Depth; ++z)
				{
					Vector3Int positionToTest = pos + forward + new Vector3Int(x, y, z);

					// Ensure in-bounds
					if (positionToTest.x < 0) return;
					if (positionToTest.y < 0) return;
					if (positionToTest.z < 0) return;

					if (positionToTest.x >= m_Settings.m_Width) return;
					if (positionToTest.y >= m_Settings.m_Height) return;
					if (positionToTest.z >= m_Settings.m_Depth) return;

					var possibilities = m_SuperPositionTiles[positionToTest.x, positionToTest.z, positionToTest.y].m_PossibleTiles;
					if (possibilities == null) continue; // tile is collapsed

					Vector3Int subTile = GetRotatedSubTileIndicesFromPosition(tile, pos + new Vector3Int(x, y, z) - root, rotation);

					WFCDirection relativeDirection = InverseRotateDirection(direction, rotation);
					var connectionData = tile.GetConnectionDataForSubTile(relativeDirection, subTile.x, subTile.y, subTile.z);

					List<Tile> removeTiles = new();
					foreach (var possibleTile in possibilities)
					{
						bool compatible = CheckIfTilesAreCompatible(connectionData, tile, direction, possibleTile);

						if (!compatible)
						{
							// Cull this tile
							removeTiles.Add(possibleTile);
						}
					}

					foreach (var r in removeTiles)
					{
						RemovePossibleTile(r, positionToTest);
					}
				}
			}
		}
	}

	bool CheckIfTilesAreCompatible(WFCTileFaceConnectionData tile1ConnectionData, WFCTileData tile1, WFCDirection tile1Direction, Tile tile2)
	{
		// Check if this tile is allowed to connect
		bool tileBanned1 = tile1ConnectionData.m_BannedAdjacentTiles.Contains(tile2.m_ParentTile);
		tileBanned1 = tileBanned1 != tile1ConnectionData.m_InvertBannedTiles;

		// Check if the connection is allowed in the other direction too
		WFCDirection relativeDirection2 = InverseRotateDirection(
			GetOppositeDirection(tile1Direction),
			tile2.m_Rotation);
		var connectionData2 = tile2.m_ParentTile.GetConnectionDataForSubTile(
			relativeDirection2,
			tile2.m_SubTileIndexX,
			tile2.m_SubTileIndexY,
			tile2.m_SubTileIndexZ);

		bool tileBanned2 = connectionData2.m_BannedAdjacentTiles.Contains(tile1);
		tileBanned2 = tileBanned2 != connectionData2.m_InvertBannedTiles;

		if (tileBanned1 || tileBanned2)
		{
			return false;
		}

		// Check if doors connect
		bool doorsConnect = tile1ConnectionData.m_DoorwayType == connectionData2.m_DoorwayType;

		if (!doorsConnect)
		{
			return false;
		}

		return true;
	}

	void PropagateTile(WFCTileData tile, Vector3Int pos, WFCRotation rotation)
	{
		PropagateInDirection(tile, pos, WFCDirection.North, rotation);
		PropagateInDirection(tile, pos, WFCDirection.South, rotation);
		PropagateInDirection(tile, pos, WFCDirection.East, rotation);
		PropagateInDirection(tile, pos, WFCDirection.West, rotation);
		PropagateInDirection(tile, pos, WFCDirection.Up, rotation);
		PropagateInDirection(tile, pos, WFCDirection.Down, rotation);
	}

	WFCTileData CollapseTile(Vector3Int tileToCollapse, out Vector3Int rootPos, out WFCRotation rotation)
	{
		// Pick tile
		Tile tileToPlace = PickRandomTile(m_SuperPositionTiles[tileToCollapse.x, tileToCollapse.z, tileToCollapse.y].m_PossibleTiles.ToArray());
		WFCTileData rootTile = tileToPlace.m_ParentTile;

		// Get rotation and position of tile
		rotation = tileToPlace.m_Rotation;
		rootPos = GetTileRootPosition(tileToPlace, rotation, tileToCollapse);

		// Collapse all sub-tiles
		for (int x = 0; x < rootTile.m_Size; ++x)
		{
			for (int y = 0; y < rootTile.m_Size; ++y)
			{
				for (int z = 0; z < rootTile.m_Size; ++z)
				{
					Vector3Int subTilePos = rootPos + new Vector3Int(x, y, z);

					// Remove tiles from possibilities
					// Can't just set to null because of big tiles
					List<Tile> removeTiles = new();
					foreach (var t in m_SuperPositionTiles[subTilePos.x, subTilePos.z, subTilePos.y].m_PossibleTiles)
					{
						removeTiles.Add(t);
					}

					foreach (var t in removeTiles)
					{
						RemovePossibleTile(t, subTilePos);
					}

					m_SuperPositionTiles[subTilePos.x, subTilePos.z, subTilePos.y].m_PossibleTiles = null;

					// Collapse sub-tile
					Vector3Int subTileIndices = GetRotatedSubTileIndicesFromPosition(rootTile, subTilePos, rotation);

					m_PlacedTiles[subTilePos.x, subTilePos.z, subTilePos.y] = new Tile()
					{
						m_ParentTile = rootTile,
						m_Rotation = rotation,
						m_SubTileIndexX = subTileIndices.x,
						m_SubTileIndexY = subTileIndices.y,
						m_SubTileIndexZ = subTileIndices.z,
					};
				}
			}
		}

		return rootTile;
	}

	void RemovePossibleTile(Tile tile, Vector3Int position)
	{
		Vector3Int root = GetTileRootPosition(tile, tile.m_Rotation, position);
		WFCTileData parent = tile.m_ParentTile;

		// Remove all sub tiles
		for (int x = 0; x < parent.m_Size; ++x)
		{
			for (int y = 0; y < parent.m_Size; ++y)
			{
				for (int z = 0; z < parent.m_Size; ++z)
				{
					Vector3Int subTilePos = new(x, y, z);
					Vector3Int indices = GetRotatedSubTileIndicesFromPosition(tile.m_ParentTile, subTilePos, tile.m_Rotation);

					subTilePos += root;

					Tile removeTile = new()
					{
						m_ParentTile = parent,
						m_Rotation = tile.m_Rotation,
						m_SubTileIndexX = indices.x,
						m_SubTileIndexY = indices.y,
						m_SubTileIndexZ = indices.z
					};

					var possibilities = m_SuperPositionTiles[subTilePos.x, subTilePos.z, subTilePos.y].m_PossibleTiles;

					possibilities?.Remove(removeTile);
				}
			}
		}
	}

	Vector3Int GetLowestEntropyTile(out bool done)
	{
		Vector3Int leastEntropyTile = new();
		int lowestEntropy = int.MaxValue;
		done = true;
		for (int x = 0; x < m_SuperPositionTiles.GetLength(0); ++x)
		{
			for (int z = 0; z < m_SuperPositionTiles.GetLength(1); ++z)
			{
				for (int y = 0; y < m_SuperPositionTiles.GetLength(2); ++y)
				{
					// Check if tile is collapsed
					if (m_SuperPositionTiles[x, z, y].m_PossibleTiles == null)
					{
						continue;
					}
					done = false;

					int entropy = m_SuperPositionTiles[x, z, y].m_PossibleTiles.Count;

					if (entropy < lowestEntropy)
					{
						lowestEntropy = entropy;
						leastEntropyTile.Set(x, y, z);
					}
				}
			}
		}

		return leastEntropyTile;
	}

	void SetupInitialPossibilities()
	{
		for (int x = 0; x < m_Settings.m_Width; ++x)
		{
			for (int z = 0; z < m_Settings.m_Depth; ++z)
			{
				for (int y = 0; y < m_Settings.m_Height; ++y)
				{
					m_SuperPositionTiles[x, z, y].m_PossibleTiles = new();
				}
			}
		}

		// Add all tiles to all positions that dont:
		// - Cause their parent tile to go off map
		// - Lead off map

		for (int i = 0; i < m_Settings.m_TilePack.m_AllTiles.Length; ++i)
		{
			var tile = m_Settings.m_TilePack.m_AllTiles[i];
			for (int rotation = 0; rotation < (int)WFCRotation.Max; ++rotation)
			{
				// Try to place this tile in all slots
				for (int x = 0; x < m_Settings.m_Width; ++x)
				{
					for (int z = 0; z < m_Settings.m_Depth; ++z)
					{
						for (int y = 0; y < m_Settings.m_Height; ++y)
						{
							Vector3Int rootPos = new(x, y, z);
							TryAddSuperpositionPossiblility(tile, (WFCRotation)rotation, rootPos);
						}
					}
				}
			}
		}
	}

	void TryAddSuperpositionPossiblility(WFCTileData tile, WFCRotation rotation, Vector3Int root)
	{
		if (DoesTileGoOOB(tile, rotation, root))
		{
			return;
		}

		// Place all sub-tiles into superposition
		for (int x = 0; x < tile.m_Size; ++x)
		{
			for (int y = 0; y < tile.m_Size; ++y)
			{
				for (int z = 0; z < tile.m_Size; ++z)
				{
					Vector3Int position = new Vector3Int(x, y, z);
					Vector3Int subTileIndices = GetRotatedSubTileIndicesFromPosition(tile, position, rotation);

					Tile subTile = new()
					{
						m_ParentTile = tile,
						m_Rotation = rotation,
						m_SubTileIndexX = subTileIndices.x,
						m_SubTileIndexY = subTileIndices.y,
						m_SubTileIndexZ = subTileIndices.z
					};

					position += root;

					m_SuperPositionTiles[position.x, position.z, position.y].m_PossibleTiles.Add(subTile);
				}
			}
		}
	}

	#endregion

	#region Utility

	WFCDirection GetOppositeDirection(WFCDirection direction)
	{
		return direction switch
		{
			WFCDirection.Up => WFCDirection.Down,
			WFCDirection.Down => WFCDirection.Up,
			WFCDirection.North => WFCDirection.South,
			WFCDirection.South => WFCDirection.North,
			WFCDirection.East => WFCDirection.West,
			WFCDirection.West => WFCDirection.East,
			_ => WFCDirection.North,
		};
	}

	int IntDot(Vector3Int a, Vector3Int b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	Vector3Int DirectionAsVector(WFCDirection direction)
	{
		return direction switch
		{
			WFCDirection.Down => Vector3Int.down,
			WFCDirection.Up => Vector3Int.up,
			WFCDirection.North => Vector3Int.forward,
			WFCDirection.South => Vector3Int.back,
			WFCDirection.East => Vector3Int.right,
			WFCDirection.West => Vector3Int.left,

			_ => throw new System.Exception("Direction error"),
		};
	}

	Vector3Int GetRotatedSubTileIndicesFromPosition(WFCTileData tile, Vector3Int position, WFCRotation rotation)
	{
		int rotationNum = RotationToNum(rotation);

		for (int i = 0; i < rotationNum; ++i)
		{
			var size = tile.m_Size;

			// Rotate 90d CW
			(position.x, position.z) = (size - 1 - position.z, position.x);
		}

		return position;
	}

	Vector3Int GetRotatedSubTilePositionFromIndices(WFCTileData tile, Vector3Int indices, WFCRotation rotation)
	{
		int rotationNum = RotationToNum(rotation);

		for (int i = 0; i < rotationNum; ++i)
		{
			var size = tile.m_Size;

			// Rotate 90d CCW
			(indices.x, indices.z) = (indices.z, size - 1 - indices.x);
		}

		return indices;
	}

	Vector3Int GetTileRootPosition(Tile tile, WFCRotation rotation, Vector3Int position)
	{
		Vector3Int indices = new(tile.m_SubTileIndexX, tile.m_SubTileIndexY, tile.m_SubTileIndexZ);
		Vector3Int subTilePos = GetRotatedSubTilePositionFromIndices(tile.m_ParentTile, indices, rotation);

		position -= subTilePos;

		return position;
	}

	Tile PickRandomTile(Tile[] tiles)
	{
		float totalWeight = 0;
		foreach (var tile in tiles)
		{
			totalWeight += tile.m_ParentTile.m_RelativeWeight;
		}

		float rand = UnityEngine.Random.Range(0, totalWeight);
		foreach (var tile in tiles)
		{
			if (rand <= tile.m_ParentTile.m_RelativeWeight)
			{
				return tile;
			}

			rand -= tile.m_ParentTile.m_RelativeWeight;
		}

		Debug.LogWarning("Weighted random fallback");
		return tiles[0];
	}

	Vector3Int GetEntranceTile()
	{
		return new Vector3Int(m_Settings.m_Width / 2, m_Settings.m_Height - 1, 0);
	}

	bool DoesTileGoOOB(WFCTileData tile, WFCRotation rotation, Vector3Int pos)
	{
		// Check if causes parent to go off map
		int maxX = pos.x + tile.m_Size - 1;
		int maxY = pos.y + tile.m_Size - 1;
		int maxZ = pos.z + tile.m_Size - 1;

		bool offMap = maxX >= m_Settings.m_Width ||
			maxY >= m_Settings.m_Height ||
			maxZ >= m_Settings.m_Depth;

		if (offMap)
		{
			return true;
		}

		// Check if a door connects to off map

		bool onEdgePX = maxX == m_Settings.m_Width - 1;
		bool onEdgePY = maxY == m_Settings.m_Height - 1;
		bool onEdgePZ = maxZ == m_Settings.m_Depth - 1;

		bool onEdgeX = pos.x == 0;
		bool onEdgeY = pos.y == 0;
		bool onEdgeZ = pos.z == 0;

		// Must be first (for entrance)
		if (onEdgeZ)
		{
			for (int x = 0; x < tile.m_Size; ++x)
			{
				for (int y = 0; y < tile.m_Size; ++y)
				{
					Vector3Int indices = GetRotatedSubTileIndicesFromPosition(tile, new Vector3Int(x, y, 0), rotation);
					Vector3Int doorPos = pos + GetRotatedSubTilePositionFromIndices(tile, indices, rotation);
					bool requireConnectionToOutside = doorPos.Equals(GetEntranceTile());

					var door = tile.GetConnectionDataForSubTile(InverseRotateDirection(WFCDirection.South, rotation), indices.x, indices.y, indices.z);

					if (!requireConnectionToOutside)
					{
						if (door.m_DoorwayType != WFCDoorwayType.Closed)
						{
							return true;
						}
					}
					else
					{
						if (door.m_DoorwayType != m_EntranceDoorType)
						{
							return true;
						}
					}
				}
			}
		}

		if (onEdgePX)
		{
			foreach (var door in tile.GetConnectionDataSubset(InverseRotateDirection(WFCDirection.East, rotation)))
			{
				if (door.m_DoorwayType != WFCDoorwayType.Closed)
					return true;
			}
		}

		if (onEdgePY)
		{
			foreach (var door in tile.GetConnectionDataSubset(InverseRotateDirection(WFCDirection.Up, rotation)))
			{
				if (door.m_DoorwayType != WFCDoorwayType.Closed)
					return true;
			}
		}

		if (onEdgePZ)
		{
			foreach (var door in tile.GetConnectionDataSubset(InverseRotateDirection(WFCDirection.North, rotation)))
			{
				if (door.m_DoorwayType != WFCDoorwayType.Closed)
					return true;
			}
		}

		if (onEdgeX)
		{
			foreach (var door in tile.GetConnectionDataSubset(InverseRotateDirection(WFCDirection.West, rotation)))
			{
				if (door.m_DoorwayType != WFCDoorwayType.Closed)
					return true;
			}
		}

		if (onEdgeY)
		{
			foreach (var door in tile.GetConnectionDataSubset(InverseRotateDirection(WFCDirection.Down, rotation)))
			{
				if (door.m_DoorwayType != WFCDoorwayType.Closed)
					return true;
			}
		}

		return false;
	}

	int RotationToNum(WFCRotation rotation)
	{
		return rotation switch
		{
			WFCRotation.None => 0,
			WFCRotation.CL_90 => 1,
			WFCRotation.CL_180 => 2,
			WFCRotation.CL_270 => 3,

			_ => throw new Exception("Rotation error"),
		};
	}

	WFCDirection InverseRotateDirection(WFCDirection direction, WFCRotation rotation)
	{
		int rotationNum = RotationToNum(rotation);

		for (int i = 0; i < rotationNum; ++i)
		{
			// Rotate 90 degrees counter clockwise
			direction = direction switch
			{
				WFCDirection.North => WFCDirection.West,
				WFCDirection.West => WFCDirection.South,
				WFCDirection.South => WFCDirection.East,
				WFCDirection.East => WFCDirection.North,

				_ => direction,
			};
		}

		return direction;
	}

	void ClearChildren()
	{
		for (int i = 0; i < transform.childCount; ++i)
		{
			if (Application.isEditor)
			{
				DestroyImmediate(transform.GetChild(i).gameObject);
			}
			else
			{
				Destroy(transform.GetChild(i).gameObject);
			}
		}
	}

	Vector3 GetTileWorldPosition(int x, int y, int z)
	{
		if (m_Settings == null)
			return Vector3.zero;

		Vector3 pos = transform.position;
		//pos += WFCGlobalGenerationSettings.s_HorizontalSize / 2 * transform.forward;
		pos += WFCGlobalGenerationSettings.s_HorizontalSize * z * transform.forward;

		//pos += WFCGlobalGenerationSettings.s_HorizontalSize / 2 * transform.right;
		pos -= WFCGlobalGenerationSettings.s_HorizontalSize * m_Settings.m_Width / 2 * transform.right;
		pos += WFCGlobalGenerationSettings.s_HorizontalSize * x * transform.right;

		//pos += WFCGlobalGenerationSettings.s_VerticalSize / 2 * transform.up;
		pos -= WFCGlobalGenerationSettings.s_VerticalSize * (m_Settings.m_Height - 1) * transform.up;
		pos += WFCGlobalGenerationSettings.s_VerticalSize * y * transform.up;

		return pos;
	}

	Vector3 GetTileWorldPosition(WFCTileData tile, Vector3Int position, WFCRotation rotation)
	{
		// Rotate around pivot
		Vector3Int pivotOffset = Vector3Int.zero;
		int rotationNum = RotationToNum(rotation);

		for (int i = 0; i < rotationNum; ++i)
		{
			var size = tile.m_Size;

			// Rotate 90d
			(pivotOffset.x, pivotOffset.z) = (pivotOffset.z, size - pivotOffset.x);
		}
		position += pivotOffset;

		return GetTileWorldPosition(position.x, position.y, position.z);
	}

	#endregion

	#region Gizmos/Handles

	void OnDrawGizmos()
	{
		DrawBB();
		DrawTileData();
	}

	void DrawTileData()
	{
#if UNITY_EDITOR
		if (m_SuperPositionTiles == null) return;

		for (int x = 0; x < m_SuperPositionTiles.GetLength(0); ++x)
		{
			for (int z = 0; z < m_SuperPositionTiles.GetLength(1); ++z)
			{
				for (int y = 0; y < m_SuperPositionTiles.GetLength(2); ++y)
				{
					var tile = m_SuperPositionTiles[x, z, y];

					if (tile.m_PossibleTiles == null) continue;

					Vector3 pos = GetTileWorldPosition(x, y, z);

					string text = $"{tile.m_PossibleTiles.Count}\n";

					foreach (var t in tile.m_PossibleTiles)
					{
						text += $" -{t.m_ParentTile.gameObject.name} r:{t.m_Rotation}\n";
					}

					Handles.Label(pos, text);
				}
			}
		}
#endif
	}

	void DrawBB()
	{
		if (m_Settings == null) return;

		// Draw bounding box
		Gizmos.color = Color.green;

		Vector3 f = m_Settings.m_Depth * WFCGlobalGenerationSettings.s_HorizontalSize * transform.forward;
		Vector3 r = m_Settings.m_Width * WFCGlobalGenerationSettings.s_HorizontalSize / 2 * transform.right;
		Vector3 u = m_Settings.m_Height * WFCGlobalGenerationSettings.s_VerticalSize * transform.up;

		Vector3 origin = transform.position + WFCGlobalGenerationSettings.s_VerticalSize * transform.up;

		// Bottom square
		Vector3[] bottom = new Vector3[]
		{
			origin - u + r,
			origin - u + r + f,
			origin - u - r + f,
			origin - u - r,
		};
		Gizmos.DrawLineStrip(bottom, true);

		// Top square
		Vector3[] top = new Vector3[]
		{
			origin + r,
			origin + r + f,
			origin - r + f,
			origin - r,
		};
		Gizmos.DrawLineStrip(top, true);

		// Vertical lines
		Vector3[] vert = new Vector3[]
		{
			origin + r,
			origin + r - u,
			origin - r,
			origin - r - u,
			origin + f + r,
			origin + f + r - u,
			origin + f - r,
			origin + f - r - u,
		};
		Gizmos.DrawLineList(vert);
	}

	#endregion
}
