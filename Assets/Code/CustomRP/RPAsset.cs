using System;
using UnityEngine;
using UnityEngine.Rendering;

public class RPAsset : RenderPipelineAsset
{
	[Header("Defaults")]
	[SerializeField] Shader m_DefaultShader;
	[SerializeField] Material m_DefaultMaterial;

	public override Shader defaultShader => m_DefaultShader;
	public override Material defaultMaterial => m_DefaultMaterial;


	[Serializable]
	public struct RPSettings
	{
		public bool EnableDynamicBatching;
		public bool EnableInstancing;

		public Material LightingMaterial;
		public Material BGMaterial;
	};

	[SerializeField] RPSettings m_Settings;


	protected override RenderPipeline CreatePipeline()
	{
		return new RenderPipelineInstance(m_Settings);
	}
}