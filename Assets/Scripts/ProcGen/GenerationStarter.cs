using System.Collections.Generic;
using UnityEngine;

public class GenerationStarter : NetworkBehaviour
{
	[SerializeField] GenerationSettings m_settings;
	[SerializeField] Transform m_nextPieceAnchor;

	int m_seed;

	enum ERotation
	{
		Identity,
		ThreeOclock,
		SixOclock,
		NineOclock,
	}

	struct TilePossibility
	{
		public ProcGenTile m_prefab;
		public float m_weight;
		public ERotation m_rotation;
	}

	struct ConnectionParams
	{
		public bool m_connectNorth;
		public bool m_connectSouth;
		public bool m_connectEast;
		public bool m_connectWest;
	}

	List<TilePossibility>[,] m_possibleTiles;
	TilePossibility[,] m_finalTiles;

	private void Start()
	{
		if (IsOwner)
		{
			m_seed = Random.Range(int.MinValue, int.MaxValue);

			Debug.Log("Generated seed: " + m_seed);

			GenerateMap(m_seed);
			SpawnPrefabs();
		}
	}

	public void OnGetSeed()
	{
		Debug.Log("Got seed: " + m_seed);

		GenerateMap(m_seed);
		SpawnPrefabs();
	}

	public void SpawnPrefabs()
	{
		for (int x = 0; x < m_finalTiles.GetLength(0); x++)
		{
			for (int y = 0; y < m_finalTiles.GetLength(1); y++)
			{
				TilePossibility tile = m_finalTiles[x, y];
				if (tile.m_prefab != null)
				{
					ProcGenTile goTile = Instantiate(tile.m_prefab);

					goTile.transform.position = m_nextPieceAnchor.position +
						m_nextPieceAnchor.rotation * (new Vector3(-x + m_finalTiles.GetLength(0) / 2, y + 0.5f, 0) * 4);

					int angle = 90 * (int)tile.m_rotation + 180;
					goTile.transform.rotation = Quaternion.Euler(0, angle, 0) * m_nextPieceAnchor.rotation;
				}
			}
		}
	}

	public void GenerateMap(int seed)
	{
		Random.InitState(seed);

		// Fill in all possibilities
		m_possibleTiles = new List<TilePossibility>[m_settings.m_width, m_settings.m_depth];
		m_finalTiles = new TilePossibility[m_settings.m_width, m_settings.m_depth];

		for (int x = 0; x < m_possibleTiles.GetLength(0); x++)
		{
			for (int y = 0; y < m_possibleTiles.GetLength(1); y++)
			{
				m_possibleTiles[x, y] = null;
			}
		}

		// Fill in starting tiles
		List<TilePossibility> startingTiles = new();

		foreach (var tile in m_settings.m_tiles)
		{
			// Add each viable rotation
			for (int i = 0; i < 4; i++)
			{
				ERotation rotation = (ERotation)i;
				ConnectionParams conn = RotateTile(tile.m_prefab, rotation);

				if (conn.m_connectSouth)
				{
					startingTiles.Add(new TilePossibility
					{
						m_prefab = tile.m_prefab,
						m_rotation = rotation,
						m_weight = tile.m_weight,
					});
				}
			}
		}

		// Pick a random starting tile
		TilePossibility startTile = startingTiles[0];

		m_finalTiles[m_settings.m_width / 2, 0] = startTile;


		UpdateSurrounding(m_settings.m_width / 2, 0);

		// Iterations
		for (int i = 0; i < 3000; ++i)
		{
			if (!CollapseLowest(out int collapseX, out int collapseY))
			{
				return;
			}

			UpdateSurrounding(collapseX, collapseY);
		}

		Debug.Log("Out of iterations");
	}


	bool CollapseLowest(out int collapsedX, out int collapsedY)
	{
		int lowestCount = int.MaxValue;
		int bestX = -1;
		int bestY = -1;
		for (int x = 0; x < m_settings.m_width; x++)
		{
			for (int y = 0; y < m_settings.m_depth; y++)
			{
				if (m_possibleTiles[x, y] != null)
				{
					if (m_possibleTiles[x, y].Count < lowestCount)
					{
						lowestCount = m_possibleTiles[x, y].Count;
						bestX = x;
						bestY = y;
					}
				}
			}
		}

		if (bestX < 0 || bestY < 0)
		{
			collapsedX = -1;
			collapsedY = -1;
			return false;
		}

		TilePossibility tile = PickRandomTile(m_possibleTiles[bestX, bestY]);

		m_possibleTiles[bestX, bestY] = null;
		m_finalTiles[bestX, bestY] = tile;

		collapsedX = bestX; collapsedY = bestY;

		return true;
	}

	void UpdateSurrounding(int x, int y)
	{
		ConnectionParams conn = RotateTile(m_finalTiles[x, y].m_prefab, m_finalTiles[x, y].m_rotation);

		// Update the 4 surrounding possibilities clockwise starting from the top
		for (int i = 0; i < 4; i++)
		{
			int newX = x;
			switch (i)
			{
				case 1:
					++newX;
					break;
				case 3:
					--newX;
					break;
				default: break;
			}

			int newY = y;
			switch (i)
			{
				case 0:
					++newY;
					break;
				case 2:
					--newY;
					break;
				default: break;
			}

			// Check if inbounds
			if (newX < 0 || newX >= m_settings.m_width ||
				newY < 0 || newY >= m_settings.m_depth)
				continue;

			// Only update if the list already exists or it connects to a door
			bool connects = false;
			switch (i)
			{
				case 0:
					connects = conn.m_connectNorth;
					break;
				case 1:
					connects = conn.m_connectEast;
					break;
				case 2:
					connects = conn.m_connectSouth;
					break;
				case 3:
					connects = conn.m_connectWest;
					break;
			}

			if (m_possibleTiles[newX, newY] != null || connects)
				UpdatePossibilities(newX, newY);
		}
	}

	void UpdatePossibilities(int x, int y)
	{
		// Check if already filled
		if (m_finalTiles[x, y].m_prefab != null) return;


		// Clear list
		if (m_possibleTiles[x, y] == null)
		{
			m_possibleTiles[x, y] = new List<TilePossibility>();
		}
		else
		{
			m_possibleTiles[x, y].Clear();
		}

		// Go through each tile and rotation and check if it works
		foreach (var tile in m_settings.m_tiles)
		{
			// Directions
			for (int rot = 0; rot < 4; ++rot)
			{
				ERotation rotation = (ERotation)rot;
				ConnectionParams conn = RotateTile(tile.m_prefab, rotation);

				// Check up, right, down, left
				bool failed = false;
				for (int i = 0; i < 4; ++i)
				{
					int checkX = x;
					switch (i)
					{
						case 1:
							++checkX;
							break;
						case 3:
							--checkX;
							break;
						default: break;
					}

					int checkY = y;
					switch (i)
					{
						case 0:
							++checkY;
							break;
						case 2:
							--checkY;
							break;
						default: break;
					}

					// Don't connect to edges of map
					if (checkX < 0 && conn.m_connectWest)
					{
						failed = true;
						break;
					}
					if (checkX >= m_settings.m_width && conn.m_connectEast)
					{
						failed = true;
						break;
					}

					if (checkY < 0 && conn.m_connectSouth)
					{
						failed = true;
						break;
					}
					if (checkY >= m_settings.m_depth && conn.m_connectNorth)
					{
						failed = true;
						break;
					}

					// Check if inbounds
					if (checkX < 0 || checkX >= m_settings.m_width ||
						checkY < 0 || checkY >= m_settings.m_depth)
						continue;

					// Check if set
					if (m_finalTiles[checkX, checkY].m_prefab == null)
						continue;

					// Check if should connect
					ConnectionParams conn2 = RotateTile(m_finalTiles[checkX, checkY].m_prefab, m_finalTiles[checkX, checkY].m_rotation);

					switch (i)
					{
						case 0:
							failed = conn.m_connectNorth != conn2.m_connectSouth;
							break;
						case 1:
							failed = conn.m_connectEast != conn2.m_connectWest;
							break;
						case 2:
							failed = conn.m_connectSouth != conn2.m_connectNorth;
							break;
						case 3:
							failed = conn.m_connectWest != conn2.m_connectEast;
							break;
					}

					if (failed)
						break;
				}

				if (!failed)
				{
					m_possibleTiles[x, y].Add(new TilePossibility
					{
						m_prefab = tile.m_prefab,
						m_rotation = rotation,
						m_weight = tile.m_weight,
					});
				}
			}
		}
	}

	ConnectionParams RotateTile(ProcGenTile tile, ERotation rotation)
	{
		return rotation switch
		{
			ERotation.Identity => new ConnectionParams
			{
				m_connectNorth = tile.m_connectNorth,
				m_connectSouth = tile.m_connectSouth,
				m_connectEast = tile.m_connectEast,
				m_connectWest = tile.m_connectWest,
			},
			ERotation.ThreeOclock => new ConnectionParams
			{
				m_connectNorth = tile.m_connectWest,
				m_connectSouth = tile.m_connectEast,
				m_connectEast = tile.m_connectNorth,
				m_connectWest = tile.m_connectSouth,
			},
			ERotation.SixOclock => new ConnectionParams
			{
				m_connectNorth = tile.m_connectSouth,
				m_connectSouth = tile.m_connectNorth,
				m_connectEast = tile.m_connectWest,
				m_connectWest = tile.m_connectEast,
			},
			ERotation.NineOclock => new ConnectionParams
			{
				m_connectNorth = tile.m_connectEast,
				m_connectSouth = tile.m_connectWest,
				m_connectEast = tile.m_connectSouth,
				m_connectWest = tile.m_connectNorth,
			},
			_ => new ConnectionParams(),
		};
	}

	TilePossibility PickRandomTile(List<TilePossibility> possibilities)
	{
		float sum = 0;
		foreach (TilePossibility tile in possibilities)
		{
			sum += tile.m_weight;
		}

		if (sum == 0)
		{
			return possibilities[Random.Range(0, possibilities.Count)];
		}

		float rand = Random.Range(0, sum);
		foreach (TilePossibility tile in possibilities)
		{
			if (rand < tile.m_weight)
				return tile;

			rand -= tile.m_weight;
		}

		Debug.LogError("Error picking tile");
		return possibilities[0];
	}
}
