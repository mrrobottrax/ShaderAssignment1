using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RenderPipelineInstance : RenderPipeline
{
	class MeshBuffers
	{
		public GraphicsBuffer m_vertexBuffer;
		public GraphicsBuffer m_adjacentBuffer;

		public void Create(Mesh mesh)
		{
			GraphicsBuffer indexBuffer = mesh.GetIndexBuffer();

			if (mesh.indexFormat == IndexFormat.UInt16 || mesh.triangles.Length == indexBuffer.count)
			{
				indexBuffer.Dispose();
				return;
			}

			// Vertex buffer

			m_vertexBuffer = new(GraphicsBuffer.Target.Structured, mesh.vertexCount, 4 * 3);
			//m_vertexBuffer = new(mesh.vertexCount, 4 * 3, ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
			m_vertexBuffer.SetData(mesh.vertices);

			// Adjacent buffer

			uint[] data = new uint[indexBuffer.count];
			indexBuffer.GetData(data);

			// Skip actual indices (first half)

			uint[] data2 = new uint[data.Length / 2];
			Array.Copy(data, data2.Length, data2, 0, data2.Length);

			m_adjacentBuffer = new(GraphicsBuffer.Target.Structured, data2.Length, 4);
			m_adjacentBuffer.SetData(data2);

			indexBuffer.Dispose();
		}

		public void Dispose()
		{
			m_vertexBuffer?.Dispose();
			m_adjacentBuffer?.Dispose();
		}

		public bool Valid()
		{
			return m_vertexBuffer != null && m_adjacentBuffer != null;
		}
	}

	static readonly Dictionary<Mesh, MeshBuffers> m_meshData = new();

	public RenderPipelineInstance()
	{
		SceneManager.sceneUnloaded += (_) => OnSceneChange();

#if UNITY_EDITOR
		AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#endif
	}

	public void Dispose()
	{
		foreach (var buffers in m_meshData.Values)
		{
			buffers.Dispose();
		}
	}

	void OnSceneChange()
	{
		// Delete buffers for old meshes
		var remove = m_meshData.Where(kv => kv.Key == null).ToArray();

		foreach (var kv in remove)
		{
			var buffers = m_meshData[kv.Key];
			buffers.Dispose();

			m_meshData.Remove(kv.Key);
		}
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		CommandBuffer clearCmd = new();
		clearCmd.ClearRenderTarget(true, true, Color.clear);
		clearCmd.SetGlobalVector("_AmbientColor", new Color(0.1f, 0.1f, 0.1f, 1));

		foreach (Camera camera in cameras)
		{
			// Setup
			if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullParams))
			{
				Debug.LogError("Failed to get culling params");
				continue;
			}

			context.SetupCameraProperties(camera);

			context.ExecuteCommandBuffer(clearCmd);

			// Cull
			CullingResults cullingResults = context.Cull(ref cullParams);

			// Draw objects
			{
				ShaderTagId shaderPassName = new("Forward");
				SortingSettings sortingSettings = new(camera);
				DrawingSettings drawingSettings = new(shaderPassName, sortingSettings);

				FilteringSettings filteringSettings = FilteringSettings.defaultValue;

				context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
			}

			// Draw shadows
			// Only do first directional light
			for (int i = 0; i < cullingResults.visibleLights.Length; ++i)
			{
				var light = cullingResults.visibleLights[i];

				if (light.lightType == LightType.Directional && light.light.shadows != LightShadows.None)
				{
					// todo: cache stuff from last frame
					var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

					CommandBuffer shadowCmd = new();

					foreach (var renderer in renderers)
					{
						if (renderer is not MeshRenderer) continue;

						MeshFilter filter = renderer.GetComponent<MeshFilter>();

						// Create buffers if they don't exist
						if (!m_meshData.TryGetValue(filter.sharedMesh, out MeshBuffers buffers))
						{
							buffers = new MeshBuffers();

							buffers.Create(filter.sharedMesh);

							m_meshData.Add(filter.sharedMesh, buffers);
						}

						if (!buffers.Valid()) continue;

						//shadowCmd.SetGlobalBuffer("adjacentBuffer", buffers.m_adjacentBuffer);
						uint offset = 0;
						shadowCmd.SetGlobalBuffer("_VertexBuffer", buffers.m_vertexBuffer);
						shadowCmd.SetGlobalBuffer("_AdjacentBuffer", buffers.m_adjacentBuffer);
						for (int submesh = 0; submesh < filter.sharedMesh.subMeshCount && submesh < renderer.sharedMaterials.Length; offset += filter.sharedMesh.GetIndexCount(submesh), ++submesh)
						{
							Material mat = renderer.sharedMaterials[submesh];

							if (mat == null) continue;

							shadowCmd.SetGlobalInteger("indexOffset", (int)offset);

							Shader shader = mat.shader;
							for (int pass = 0; pass < shader.passCount; ++pass)
							{
								if (shader.FindPassTagValue(pass, new ShaderTagId("LightMode")) == new ShaderTagId("ShadowCaster"))
								{
									// shadowCmd.SetGlobalFloat("_StencilRef", 1);
									// shadowCmd.SetGlobalFloat("_StencilComp", (float)CompareFunction.Less);
									// shadowCmd.SetGlobalFloat("_StencilOp", (float)StencilOp.DecrementWrap);

									//shadowCmd.

									shadowCmd.DrawMesh(
										filter.sharedMesh,
										renderer.localToWorldMatrix,
										renderer.sharedMaterials[submesh],
										submesh,
										pass
									);
								}
							}
						}
					}

					shadowCmd.SetGlobalVector("_AmbientColor", new Color(1, 1, 1, 1));
					context.ExecuteCommandBuffer(shadowCmd);
					shadowCmd.Release();
				}
			}

			// Draw objects
			{
				ShaderTagId shaderPassName = new("Forward");
				SortingSettings sortingSettings = new(camera);
				DrawingSettings drawingSettings = new(shaderPassName, sortingSettings);

				FilteringSettings filteringSettings = FilteringSettings.defaultValue;

				context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
			}

			// Draw skybox
			context.DrawSkybox(camera);

			context.Submit();
		}

		clearCmd.Release();
	}
}
