using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu]
public class RPAsset : RenderPipelineAsset
{
	[SerializeField] Shader m_DefaultShader;
	[SerializeField] Material m_DefaultMaterial;

	public override Shader defaultShader => m_DefaultShader;
	public override Material defaultMaterial => m_DefaultMaterial;

	protected override RenderPipeline CreatePipeline()
	{
		return new RenderPipelineInstance();
	}
}