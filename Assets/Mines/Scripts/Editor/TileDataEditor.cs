using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFCTileData))]
public class TileDataEditor : Editor
{
	SerializedProperty m_ConnectionData;

	int m_SelectedFaceIndex = -1;

	#region Inspector

	void OnEnable()
	{
		m_ConnectionData = serializedObject.FindProperty("m_ConnectionData");
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		CheckArraySize();

		if (m_SelectedFaceIndex == -1) return;

		EditorGUILayout.Separator();
		EditorGUILayout.BeginVertical(new GUILayoutOption[] { });
		EditorGUILayout.LabelField("Selected face:", new GUILayoutOption[] { });

		DrawSelectedFaceGUI();

		EditorGUILayout.EndVertical();
	}

	void DrawSelectedFaceGUI()
	{
		if (m_SelectedFaceIndex >= m_ConnectionData.arraySize)
		{
			m_SelectedFaceIndex = -1;
			return;
		}

		SerializedProperty serializedFace = m_ConnectionData.GetArrayElementAtIndex(m_SelectedFaceIndex);

		// Door sizes dropdown
		SerializedProperty doorwayType = serializedFace.FindPropertyRelative("m_DoorwayType");
		EditorGUILayout.PropertyField(doorwayType, new GUILayoutOption[] { });

		// Banned tiles array
		SerializedProperty invertBannedTiles = serializedFace.FindPropertyRelative("m_InvertBannedTiles");
		SerializedProperty bannedTiles = serializedFace.FindPropertyRelative("m_BannedAdjacentTiles");
		EditorGUILayout.PropertyField(invertBannedTiles, new GUILayoutOption[] { });
		EditorGUILayout.PropertyField(bannedTiles, new GUILayoutOption[] { });

		serializedObject.ApplyModifiedProperties();
	}

	// Check the size of the exposed faces array is correct
	void CheckArraySize()
	{
		if (m_ConnectionData == null) return;

		int size = GetTarget().m_Size;
		int correctFaceCount = size * size * 6;

		if (m_ConnectionData.arraySize == correctFaceCount)
			return;

		GetTarget().m_ConnectionData = new WFCTileFaceConnectionData[correctFaceCount];
		m_SelectedFaceIndex = -1;
	}

	#endregion

	#region Draw Handles

	bool IsDirectionProjectedForwards(WFCDirection dir)
	{
		if (dir == WFCDirection.North) return true;
		if (dir == WFCDirection.East) return true;
		if (dir == WFCDirection.Up) return true;

		return false;
	}

	void OnSceneGUI()
	{
		// Draw handles for each connection

		WFCTileData data = GetTarget();
		Transform t = data.transform;

		// Draw Faces
		DrawFacesInDirection(WFCDirection.North, t.forward, t.up, t.right);
		DrawFacesInDirection(WFCDirection.East, t.right, t.up, t.forward);
		DrawFacesInDirection(WFCDirection.South, -t.forward, t.up, t.right);
		DrawFacesInDirection(WFCDirection.West, -t.right, t.up, t.forward);
		DrawFacesInDirection(WFCDirection.Up, t.up, t.forward, t.right);
		DrawFacesInDirection(WFCDirection.Down, -t.up, t.forward, t.right);
	}

	void DrawFacesInDirection(WFCDirection dir, Vector3 forward, Vector3 up, Vector3 right)
	{
		WFCTileData data = GetTarget();

		var fdata = data.GetConnectionDataSubset(dir);
		if (fdata == null) return;

		int edgeLength = (int)Mathf.Sqrt(fdata.Count);
		int index = 0;

		foreach (WFCTileFaceConnectionData face in fdata)
		{
			// Move forwards to match face location
			float extent = WFCGlobalGenerationSettings.s_HorizontalSize / 2;
			if (IsDirectionProjectedForwards(dir))
			{
				extent += WFCGlobalGenerationSettings.s_HorizontalSize * (edgeLength - 1);
			}

			Vector3 origin = data.transform.position + forward * extent;
			origin += WFCGlobalGenerationSettings.s_HorizontalSize / 2 * Vector3.one;

			// Move in grid pattern along face
			int x = index % edgeLength;
			int y = index / edgeLength;

			origin += x * WFCGlobalGenerationSettings.s_HorizontalSize * right
				+ y * WFCGlobalGenerationSettings.s_VerticalSize * up;

			DrawFaceHandles(face, index + (fdata.Count * (int)dir), origin, forward, up, right);

			++index;
		}
	}

	Vector2 GetDoorSize(WFCDoorwayType doorwayType)
	{
		return doorwayType switch
		{
			WFCDoorwayType.Normal4x5 => new Vector2(4, 5),
			_ => new Vector2(1, 1),
		};
	}

	void DrawFaceHandles(WFCTileFaceConnectionData face, int index, Vector3 origin, Vector3 forward, Vector3 up, Vector3 right)
	{
		// Draw X if closed
		if (face.m_DoorwayType == WFCDoorwayType.Closed)
		{
			Handles.color = new Color(1, 0, 0, 0.5f);

			Handles.DrawDottedLine(origin + up - right, origin - up + right, 5);
			Handles.DrawDottedLine(origin + up + right, origin - up - right, 5);
		}
		// Draw square if open
		else
		{
			Handles.color = Color.green;

			Vector2 size = GetDoorSize(face.m_DoorwayType);
			Vector3 x = size.x / 2 * right;
			Vector3 y = size.y / 2 * up;

			Vector3[] points = {
				origin + x - y,
				origin + x + y,
				origin - x + y,
				origin - x - y,
				origin + x - y,
			};

			Handles.DrawPolyLine(points);
		}

		// Draw selectable sphere
		if (m_SelectedFaceIndex == index) Handles.color = Color.yellow;
		else Handles.color = face.m_DoorwayType == WFCDoorwayType.Closed ? Color.red : Color.white;

		if (Handles.Button(origin, Quaternion.identity, 0.1f, 0.2f, Handles.SphereHandleCap))
		{
			m_SelectedFaceIndex = index;
			Repaint();
		}
	}

	#endregion

	#region Util

	WFCTileData GetTarget()
	{
		return target as WFCTileData;
	}

	#endregion
}