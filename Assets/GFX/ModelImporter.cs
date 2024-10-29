using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomModelImporter : AssetPostprocessor
{
	public void OnPreprocessMaterialDescription(MaterialDescription desc, Material material, AnimationClip[] clips)
	{
		var shader = Shader.Find("BasicLit");
		if (shader == null)
			return;
		material.shader = shader;

		// Read a texture property from the material description.
		if (desc.TryGetProperty("DiffuseColor", out TexturePropertyDescription textureProperty))
		{
			// Assign the texture to the material.
			material.SetTexture("_MainTex", textureProperty.texture);
		}
		else if (desc.TryGetProperty("DiffuseColor", out Vector4 colorProperty))
		{
			material.SetColor("_Color", colorProperty);
		}
	}

	public void OnPostprocessModel(GameObject g)
	{
		MeshFilter[] filters = g.GetComponentsInChildren<MeshFilter>();
		Renderer[] renderers = g.GetComponentsInChildren<Renderer>();

		for (int i = 0; i < filters.Length; ++i)
		{
			// Only run for static meshes
			// todo: support bones
			if (renderers[i] is not SkinnedMeshRenderer)
				FilterMesh(filters[i].sharedMesh, renderers[i]);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Vertex
	{
		public Vector3 pos;
		public Vector3 normal;
		public Vector2 uv;
	}

	void FilterMesh(Mesh mesh, Renderer renderer)
	{
		// Get adjacent tris

		// Get total indices count and submeshes
		List<SubMeshDescriptor> subMeshes = new();
		List<int[]> oldIndices = new();
		uint totalIndices = 0;
		for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh)
		{
			totalIndices += mesh.GetIndexCount(submesh);

			subMeshes.Add(mesh.GetSubMesh(submesh));
			oldIndices.Add(mesh.GetIndices(submesh));
		}

		var adjacentIndices = new uint[totalIndices];

		// Loop tris
		// todo: this does not work
		int[] indices = mesh.triangles;
		for (int iTri = 0; iTri < indices.Length; iTri += 3)
		{
			// For each edge
			for (int iEdge = 0; iEdge < 3; ++iEdge)
			{
				int iiVertA = iTri + iEdge;
				int iiVertB = iTri + (iEdge + 1) % 3;

				int iVertA = indices[iiVertA];
				int iVertB = indices[iiVertB];

				Vector3 vertA = mesh.vertices[iVertA];
				Vector3 vertB = mesh.vertices[iVertB];

				uint iAdjacent = uint.MaxValue; // Stand in for no adjacent

				// Find the tri with the edge iVertB, iVertA (adjacent edge)
				for (int iTri2 = 0; iTri2 < indices.Length; iTri2 += 3)
				{
					// Loop edges
					for (int iEdge2 = 0; iEdge2 < 3; ++iEdge2)
					{
						int iiVertA2 = iTri2 + iEdge2;
						int iiVertB2 = iTri2 + (iEdge2 + 1) % 3;
						int iiVertC2 = iTri2 + (iEdge2 + 2) % 3;

						int iVertA2 = indices[iiVertA2];
						int iVertB2 = indices[iiVertB2];

						Vector3 vertA2 = mesh.vertices[iVertA2];
						Vector3 vertB2 = mesh.vertices[iVertB2];

						const float k_weldDist = 0.0001f;

						if (Vector3.Distance(vertA2, vertB) < k_weldDist && 
							Vector3.Distance(vertB2, vertA) < k_weldDist)
						{
							// Get the next vert
							iAdjacent = (uint)indices[iiVertC2];

							break;
						}
					}
				}

				//if (iAdjacent == uint.MaxValue) Debug.Log("TEST");
				adjacentIndices[iiVertA] = iAdjacent;
			}
		}

		// Set verts

		VertexAttributeDescriptor[] vertParams = new VertexAttributeDescriptor[]{
			new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
			new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
		};

		int vertCount = mesh.vertexCount;
		var verts = new Vertex[vertCount];

		for (int i = 0; i < vertCount; ++i)
		{
			verts[i] = new Vertex()
			{
				pos = mesh.vertices[i],
				normal = mesh.normals[i],
				uv = mesh.uv[i],
			};
		}

		mesh.SetVertexBufferParams(vertCount, vertParams);
		mesh.SetVertexBufferData(verts, 0, 0, vertCount);

		// Set indices

		uint[] indexBuffer = new uint[totalIndices + adjacentIndices.Length];

		// Copy old indices
		int copyIndex = 0;
		for (int i = 0; i < subMeshes.Count; ++i)
		{
			for (int j = 0; j < oldIndices[i].Length; ++j)
			{
				indexBuffer[j + copyIndex] = (uint)oldIndices[i][j];
			}
			copyIndex += oldIndices[i].Length;
		}

		// Store adjacency data in index buffer
		adjacentIndices.CopyTo(indexBuffer, copyIndex);

		mesh.SetIndexBufferParams(indexBuffer.Length, IndexFormat.UInt32);
		mesh.SetIndexBufferData<uint>(indexBuffer, 0, 0, indexBuffer.Length, MeshUpdateFlags.DontValidateIndices);

		mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.Default);
	}
}
