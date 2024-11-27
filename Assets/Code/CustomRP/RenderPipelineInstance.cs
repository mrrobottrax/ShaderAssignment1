using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderPipelineInstance : RenderPipeline
{
	public static RenderPipelineInstance Singleton;

	public RPAsset.RPSettings m_Settings;

	Mesh k_FullscreenQuad = null;

	public static bool DisableTextures = false;

	void RegenMesh()
	{
		if (k_FullscreenQuad != null) return;

		k_FullscreenQuad = new()
		{
			vertices = new Vector3[]{
				new(0, 0, 1),
				new(1, 0, 1),
				new(1, 1, 1),
				new(0, 1, 1),
			},
			triangles = new int[]{
				0, 1, 2,
				0, 2, 3
			},
			uv = new Vector2[]{
				new(0, 0),
				new(1, 0),
				new(1, 1),
				new(0, 1),
			}
		};
	}

	public RenderPipelineInstance(RPAsset.RPSettings settings) : base()
	{
		m_Settings = settings;
		Singleton = this;

#if UNITY_EDITOR
		TierSettings tierSettings = EditorGraphicsSettings.GetTierSettings(UnityEditor.BuildTargetGroup.Standalone, GraphicsTier.Tier3);
		tierSettings.renderingPath = RenderingPath.DeferredShading;
		EditorGraphicsSettings.SetTierSettings(UnityEditor.BuildTargetGroup.Standalone, GraphicsTier.Tier3, tierSettings);
#endif
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		RegenMesh();

		foreach (Camera camera in cameras)
		{
#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView)
			{
				ScriptableRenderContext.EmitGeometryForCamera(camera);
				ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			}
#endif

			context.SetupCameraProperties(camera);

			// Setup
			if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
			{
				Debug.LogError("Failed to get culling params");
				continue;
			}

			CullingResults cullingResults = context.Cull(ref cullingParameters);

			// Lower res
			float scalingFactor = 1.5f;
			int width = (int)(camera.pixelWidth / scalingFactor);
			int height = (int)(camera.pixelHeight / scalingFactor);

			// Create GBuffer
			RenderTexture color = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default);
			RenderTexture depth = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Depth);

			RenderTexture albedo = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
			RenderTexture normal = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture position = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat);

			// !!!--- USE THIS TO SET THE TEXTURE TO VIEW ---!!!
			RenderTexture viewTex = color; // <--
			viewTex.filterMode = FilterMode.Point;

			// Render Deferred
			FillGBuffer(ref context, camera, cullingResults, albedo, normal, position, depth);

			// Alvaro's Togglable Textures Requirement (tm)
			if (DisableTextures)
			{
				CommandBuffer whiteTextures = new()
				{
					name = "Whiten Textures"
				};
				whiteTextures.SetRenderTarget(albedo);
				float reflectivity = 0.4f;
				whiteTextures.ClearRenderTarget(RTClearFlags.Color, new Color(reflectivity, reflectivity, reflectivity, 1), 1, 0);
				context.ExecuteCommandBuffer(whiteTextures);
				whiteTextures.Dispose();
			}

			DrawLighting(context, cullingResults, color, depth, albedo, normal, position, camera);

			context.DrawSkybox(camera);
			DrawBG(context);

			// Render transparent
			DrawTransparent(context, camera, cullingResults, color, depth);

			DrawPostProcessing(context, width, height, color, depth, position, camera);

			// Blit over to camera texture
			CommandBuffer blit = new()
			{
				name = "Blit Colour"
			};
			blit.Blit(viewTex, BuiltinRenderTextureType.CameraTarget);

			context.ExecuteCommandBuffer(blit);
			blit.Dispose();

#if UNITY_EDITOR
			if (UnityEditor.Handles.ShouldRenderGizmos())
			{
				context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
				context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
			}
#endif

			context.Submit();

			RenderTexture.ReleaseTemporary(color);
			RenderTexture.ReleaseTemporary(depth);

			RenderTexture.ReleaseTemporary(albedo);
			RenderTexture.ReleaseTemporary(normal);
			RenderTexture.ReleaseTemporary(position);
		}
	}

	void DrawPostProcessing(ScriptableRenderContext context, int width, int height,
		RenderTexture color, RenderTexture depth, RenderTexture position, Camera camera)
	{
		CommandBuffer cmd = new()
		{
			name = "Post Processing"
		};

		// Setup
		int colorID = Shader.PropertyToID("_Color");
		cmd.GetTemporaryRT(colorID, width, height, 0, FilterMode.Point, RenderTextureFormat.Default);
		cmd.Blit(color, colorID);

		cmd.SetRenderTarget(color, depth);

		cmd.SetGlobalTexture("_Position", position);

		// Fog
		if (RenderSettings.fog)
		{
			cmd.SetGlobalVector("_CameraPos", camera.transform.position);
			cmd.SetGlobalColor("_FogColor", RenderSettings.fogColor);
			cmd.SetGlobalFloat("_FogStart", RenderSettings.fogStartDistance);
			cmd.SetGlobalFloat("_FogEnd", RenderSettings.fogEndDistance);

			cmd.DrawMesh(k_FullscreenQuad, Matrix4x4.identity, m_Settings.FogMaterial, 0, 0);
		}

		cmd.Blit(color, colorID);
		cmd.SetRenderTarget(color, depth);

		// LUT
		{
			cmd.DrawMesh(k_FullscreenQuad, Matrix4x4.identity, m_Settings.LUTMaterial, 0, 0);
		}

		cmd.ReleaseTemporaryRT(colorID);

		context.ExecuteCommandBuffer(cmd);
		cmd.Dispose();



		// CommandBuffer pass2 = new()
		// {
		// 	name = "Pass 2"
		// };

		// pass2.SetRenderTarget(color, depth);
		// context.ExecuteCommandBuffer(pass2);
		// pass2.Dispose();
	}

	void DrawBG(ScriptableRenderContext context)
	{
		//Draw lighting
		CommandBuffer bg = new()
		{
			name = "Clear BG"
		};

		// Light
		bg.DrawMesh(k_FullscreenQuad, Matrix4x4.identity, m_Settings.BGMaterial, 0, 0);

		context.ExecuteCommandBuffer(bg);
		bg.Dispose();
	}

	void DrawTransparent(ScriptableRenderContext context, Camera camera, CullingResults cullingResults,
		RenderTexture color, RenderTexture depth)
	{
		CommandBuffer cmd = new()
		{
			name = "Init Transparent"
		};
		cmd.SetRenderTarget(color, depth);

		context.ExecuteCommandBuffer(cmd);
		cmd.Dispose();

		SortingSettings sortingSettings = new(camera)
		{
			criteria = SortingCriteria.BackToFront
		};

		DrawingSettings drawingSettings = new(new ShaderTagId("ForwardBase"), sortingSettings)
		{
			enableDynamicBatching = m_Settings.EnableDynamicBatching,
			enableInstancing = m_Settings.EnableInstancing
		};

		FilteringSettings filteringSettings = FilteringSettings.defaultValue;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	void FillGBuffer(ref ScriptableRenderContext context, Camera camera, CullingResults cullingResults,
		RenderTexture albedo, RenderTexture normal, RenderTexture position, RenderTexture depth)
	{
		RenderTargetIdentifier[] gbuffer = {
			albedo,
			normal,
			position,
		};

		// Bind GBuffer
		CommandBuffer cmd = new()
		{
			name = "Init GBuffer"
		};
		cmd.SetRenderTarget(position);
		cmd.ClearRenderTarget(RTClearFlags.Color, new Color(float.PositiveInfinity, 0, 0), 1, 0);
		cmd.SetRenderTarget(gbuffer, depth, 0, CubemapFace.Unknown, 0);
		cmd.ClearRenderTarget(RTClearFlags.DepthStencil, new Color(), 1, 0);

		context.ExecuteCommandBuffer(cmd);
		cmd.Dispose();

		SortingSettings sortingSettings = new(camera);

		FilteringSettings filteringSettings = FilteringSettings.defaultValue;
		filteringSettings.renderQueueRange = RenderQueueRange.opaque;

		DrawingSettings drawingSettings = new(new ShaderTagId("Deferred"), sortingSettings)
		{
			enableDynamicBatching = m_Settings.EnableDynamicBatching,
			enableInstancing = m_Settings.EnableInstancing
		};

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	void DrawLighting(ScriptableRenderContext context, CullingResults cullingResults, RenderTexture color,
		RenderTexture depth, RenderTexture albedo, RenderTexture normal, RenderTexture position, Camera camera)
	{
		CommandBuffer cmd = new()
		{
			name = "Init Lighting"
		};
		cmd.SetRenderTarget(color, depth);
		cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1, 0);
		cmd.SetGlobalTexture("_Albedo", albedo);
		cmd.SetGlobalTexture("_Normal", normal);
		cmd.SetGlobalTexture("_Position", position);

		// CG STUFF
		cmd.DisableShaderKeyword("NO_LIGHTING");
		cmd.DisableShaderKeyword("DIFFUSE_ONLY");
		cmd.DisableShaderKeyword("SPECULAR_ONLY");
		cmd.DisableShaderKeyword("DIFFUSE_SPECULAR");
		cmd.SetGlobalVector("_CameraPos", camera.transform.position);
		switch (CGClass.mode)
		{
			case RenderMode.NO_LIGHTING:
				cmd.EnableShaderKeyword("NO_LIGHTING");
				break;

			case RenderMode.DIFFUSE:
				cmd.EnableShaderKeyword("DIFFUSE_ONLY");
				break;

			case RenderMode.SPECULAR:
				cmd.EnableShaderKeyword("SPECULAR_ONLY");
				break;

			case RenderMode.DIFFUSE_SPECULAR:
				cmd.EnableShaderKeyword("DIFFUSE_SPECULAR");
				break;
		}
		// CG STUFF

		context.ExecuteCommandBuffer(cmd);
		cmd.Dispose();

		for (int i = 0; i < cullingResults.visibleLights.Length; ++i)
		{
			if (cullingResults.visibleLights[i].lightType == LightType.Directional)
			{
				DrawDirectionalLight(context, cullingResults, i);
			}
			else if (cullingResults.visibleLights[i].lightType == LightType.Spot)
			{
				DrawSpotLight(context, cullingResults, i);
			}
			else
			{
				DrawPointLight(context, cullingResults, i);
			}
		}
	}

	void DrawDirectionalLight(ScriptableRenderContext context, CullingResults cullingResults,
		int lightIndex)
	{
		// Setup
		CommandBuffer cmd = new()
		{
			name = "Set Up Directional Shadows"
		};

		cmd.EnableShaderKeyword("DIRECTIONAL");
		cmd.ClearRenderTarget(RTClearFlags.Stencil, Color.clear, 1, 0);

		Light light = cullingResults.visibleLights[lightIndex].light;
		Vector3 forward = -light.transform.forward;
		cmd.SetGlobalVector("_WorldSpaceLightPos0", new Vector4(forward.x, forward.y, forward.z, 1));

		context.ExecuteCommandBuffer(cmd);
		cmd.Release();

		// Shadows
		if (cullingResults.visibleLights[lightIndex].light.shadows != LightShadows.None)
		{
			if (cullingResults.GetShadowCasterBounds(lightIndex, out _))
			{
				ShadowDrawingSettings settings = new(cullingResults, lightIndex, BatchCullingProjectionType.Orthographic);
				context.DrawShadows(ref settings);
			}
		}

		//Draw lighting
		CommandBuffer lighting = new()
		{
			name = "Directional Light"
		};

		lighting.SetGlobalColor("_LightColor", light.color * light.intensity);
		lighting.SetGlobalColor("_AmbientColor", RenderSettings.subtractiveShadowColor);

		// Light
		lighting.DrawMesh(k_FullscreenQuad, Matrix4x4.identity, m_Settings.LightingMaterial, 0, 0);

		// Shadow
		lighting.DrawMesh(k_FullscreenQuad, Matrix4x4.identity, m_Settings.LightingMaterial, 0, 1);

		context.ExecuteCommandBuffer(lighting);
		lighting.Dispose();
	}

	void DrawPointLight(ScriptableRenderContext context, CullingResults cullingResults,
		int lightIndex, bool spot = false)
	{
		// Setup
		CommandBuffer cmd = new()
		{
			name = "Set Up Point Shadows"
		};

		cmd.DisableShaderKeyword("DIRECTIONAL");
		cmd.ClearRenderTarget(RTClearFlags.Stencil, Color.clear, 1, 0);

		Light light = cullingResults.visibleLights[lightIndex].light;
		Vector3 position = light.transform.position;
		cmd.SetGlobalVector("_WorldSpaceLightPos0", new Vector4(position.x, position.y, position.z, 1));

		context.ExecuteCommandBuffer(cmd);
		cmd.Release();

		// Shadows
		if (cullingResults.visibleLights[lightIndex].light.shadows != LightShadows.None)
		{
			if (cullingResults.GetShadowCasterBounds(lightIndex, out _))
			{
				ShadowDrawingSettings settings = new(cullingResults, lightIndex, BatchCullingProjectionType.Orthographic);
				context.DrawShadows(ref settings);
			}
		}

		//Draw lighting
		CommandBuffer lighting = new()
		{
			name = "Point Light"
		};

		// Light
		lighting.SetGlobalColor("_LightColor", light.color * light.intensity);

		Vector3 forward = light.transform.forward;
		lighting.SetGlobalVector("_LightDir", new Vector4(forward.x, forward.y, forward.z, light.range));

		lighting.SetGlobalFloat("_TanTheta", Mathf.Tan(light.spotAngle / 2 * Mathf.Deg2Rad));

		lighting.DrawMesh(k_FullscreenQuad, Matrix4x4.identity, m_Settings.LightingMaterial, 0, spot ? 3 : 2);

		context.ExecuteCommandBuffer(lighting);
		lighting.Dispose();
	}

	void DrawSpotLight(ScriptableRenderContext context, CullingResults cullingResults,
		int lightIndex)
	{
		DrawPointLight(context, cullingResults, lightIndex, true);
	}
}
