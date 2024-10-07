using UnityEngine;
using UnityEngine.Rendering;

public class RailGenerator : ScriptableObject
{
	[SerializeField] Vector3 m_railExtents = new(1, 0.1f, 0.5f);
	[SerializeField] float m_segmentDensity = 1;
	[SerializeField] float m_handleLength = 1;
	//[SerializeField] float m_railDotCutoff = 0.7f;

	[Header("Mesh")]
	[SerializeField] Vector3[] m_railProfile;
	[SerializeField] Material m_metalMat;

	public struct Curve
	{
		public float m_length;
		public Vector3[] m_points;
		public Quaternion[] m_orientations;
	}

	public Vector3 GetRailExtents()
	{
		return m_railExtents;
	}

	public int GetSegmentCount(Vector3 start, Vector3 end)
	{
		int segments = (int)(Vector3.Distance(start, end) * m_segmentDensity + 0.001f);
		segments = Mathf.Max(segments, 1);
		return segments;
	}

	public float GetTangentLength(Vector3 start, Vector3 end)
	{
		return Mathf.Min(m_handleLength, Vector3.Distance(start, end));
	}

	public Mesh GenerateMesh(Curve curve, Vector3 relativePos, Quaternion relativeRot)
	{
		int segments = curve.m_points.Length - 1;

		int vertCount = 2 * m_railProfile.Length * (curve.m_points.Length);
		int indexCount = 2 * (m_railProfile.Length - 1) * segments * 2 * 3;

		Vector3[] vertices = new Vector3[vertCount];
		int[] indices = new int[indexCount];

		Quaternion inverseRot = Quaternion.Inverse(relativeRot);
		Vector3 inversePos = -relativePos;

		// Generate verts
		for (int leftRight = 0; leftRight < 2; ++leftRight)
		{
			for (int s = 0; s < curve.m_points.Length; ++s)
			{
				int ivbase = m_railProfile.Length * (s + leftRight * curve.m_points.Length);

				Vector3 pos = inverseRot * (curve.m_points[s] + inversePos);
				Quaternion rot = inverseRot * curve.m_orientations[s];

				Vector3 offset = (leftRight == 1 ? -1 : 1) * m_railExtents.x * Vector3.left;
				for (int i = 0; i < m_railProfile.Length; ++i)
				{
					vertices[i + ivbase] = rot * (offset + m_railProfile[i]) + pos;
				}
			}
		}

		// Draw verts
		//for (int i = 0; i < vertices.Length; ++i)
		//{
		//	Debug.DrawRay(start + startRotation * vertices[i], Vector3.up * 0.02f, Color.HSVToRGB(i / (float)vertices.Length, 1, 1));
		//}

		// Generate indices
		for (int leftRight = 0; leftRight < 2; ++leftRight)
		{
			for (int s = 0; s < segments; ++s)
			{
				// Quads
				for (int i = 0; i < m_railProfile.Length - 1; ++i)
				{
					int ivbase = i + m_railProfile.Length * (s + leftRight * (segments + 1));
					int ivbase2 = m_railProfile.Length + ivbase;

					int quadsPerSeg = m_railProfile.Length - 1;
					int quadIndex = i + quadsPerSeg * (s + leftRight * segments);
					int iibase = 6 * quadIndex;

					indices[iibase + 0] = ivbase;
					indices[iibase + 1] = ivbase2;
					indices[iibase + 2] = ivbase + 1;

					indices[iibase + 3] = ivbase2;
					indices[iibase + 4] = ivbase2 + 1;
					indices[iibase + 5] = ivbase + 1;
				}
			}
		}

		// Generate vert positions

		Mesh mesh = new()
		{
			vertices = vertices
		};
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	public Curve GenerateCurve(Vector3 start, Quaternion startRot, Vector3 end, Quaternion endRot)
	{
		int segments = (int)(Vector3.Distance(start, end) * m_segmentDensity + 0.001f);
		segments = Mathf.Max(segments, 1);

		float length = GetTangentLength(start, end);

		Vector3 startTan = startRot * Vector3.forward * length;
		Vector3 endTan = endRot * Vector3.forward * length;

		Debug.DrawRay(start, startTan, Color.red);
		Debug.DrawRay(end, endTan, Color.red);

		Curve curve = new()
		{
			m_points = new Vector3[segments + 1],
			m_orientations = new Quaternion[segments + 1]
		};

		// Get points
		for (int i = 0; i < segments + 1; ++i)
		{
			float t = (float)i / segments;

			EvalutateCurve(t, start, end, startTan, endTan, startRot, endRot, out Vector3 pos, out Quaternion rotation);

			curve.m_points[i] = pos;
			curve.m_orientations[i] = rotation;
		}

		// Get length
		for (int i = 1; i < segments + 1; ++i)
		{
			curve.m_length += Vector3.Distance(curve.m_points[i], curve.m_points[i - 1]);
		}

		return curve;
	}

	public void EvalutateCurve(float progress, Vector3 start, Vector3 end, Vector3 startTan, Vector3 endTan, Quaternion startRotation, Quaternion endRotation, out Vector3 pos, out Quaternion rotation)
	{
		Vector3 startUp = startRotation * Vector3.up;
		Vector3 endUp = endRotation * Vector3.up;
		Vector3 up = Vector3.Slerp(startUp, endUp, progress);

		// Cubic Hermite spline
		float t = progress;
		float t3 = t * t * t;
		float t2 = t * t;
		pos = (2 * t3 - 3 * t2 + 1) * start + (t3 - 2 * t2 + t) * startTan + (-2 * t3 + 3 * t2) * end + (t3 - t2) * endTan;

		// Get derivitive
		Vector3 slope = (6 * t2 - 6 * t) * start + (3 * t2 - 4 * t + 1) * startTan + (-6 * t2 + 6 * t) * end + (3 * t2 - 2 * t) * endTan;

		Vector3 tan = slope.normalized;

		rotation = Quaternion.LookRotation(tan, up);
	}
}
