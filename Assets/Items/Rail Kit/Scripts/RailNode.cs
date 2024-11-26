using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RailNode : MonoBehaviour
{
	public RailNode prev;
	public RailNode next;

	RailGenerator.Curve m_curve;

	public void GenerateMesh(RailGenerator generator, RailGenerator.Curve curve)
	{
		if (next == null)
		{
			Debug.LogWarning("Next node required to generate a mesh");
			return;
		}

		m_curve = curve;

		GetComponent<MeshFilter>().mesh = generator.GenerateMesh(curve, transform.position, transform.rotation);
	}

	// Used when a node can have multiple meshes (connecting two same ends)
	public void GenerateMesh2(RailGenerator generator, RailGenerator.Curve curve)
	{
		m_curve = curve;

		GameObject go = transform.GetChild(0).gameObject;
		go.SetActive(true);

		go.GetComponent<MeshFilter>().mesh = generator.GenerateMesh(curve, transform.position, transform.rotation);
	}
}
