using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(NetworkObject))]
public class LutBlender : Interactable
{
	[SerializeField] UniversalRendererData m_Renderer;
	[SerializeField] float m_BlendTime = 1;
	[SerializeField] Texture2D m_NeutralLUT;
	[SerializeField] Texture2D m_CaveLUT;
	[SerializeField] Texture2D m_OutsideLUT;
	[SerializeField] Texture2D m_TrainLUT;

	void Awake()
	{
		ResetNeutral();
	}

	void OnDestroy()
	{
		ResetNeutral();
	}

	public void ResetNeutral()
	{
		var arr = m_Renderer.rendererFeatures.Where((feature) => feature.name == "LUT").ToArray();
		var feature = arr[0] as FullScreenPassRendererFeature;

		var mat = feature.passMaterial;
		mat.SetTexture("_LutTex0", m_NeutralLUT);
		mat.SetTexture("_LutTex1", m_NeutralLUT);
	}

	public override Interaction[] GetInteractions()
	{
		//return null;

		return new Interaction[]
		{
			new()
			{
				prompt = "Neutral LUT",
				sprite = null,
				interact = (_) => SetLUT(m_NeutralLUT)
			},
			new()
			{
				prompt = "Cave LUT",
				sprite = null,
				interact = (_) => SetLUT(m_CaveLUT)
			},
			new()
			{
				prompt = "Outside LUT",
				sprite = null,
				interact = (_) => SetLUT(m_OutsideLUT)
			},
			new()
			{
				prompt = "Train LUT",
				sprite = null,
				interact = (_) => SetLUT(m_TrainLUT)
			},
		};
	}

	public void SetLUT(Texture2D lut)
	{
		StartCoroutine(BlendToLut(lut));
	}

	IEnumerator BlendToLut(Texture2D lut)
	{
		interactionEnabled = false;

		var arr = m_Renderer.rendererFeatures.Where((feature) => feature.name == "LUT").ToArray();
		var feature = arr[0] as FullScreenPassRendererFeature;

		var oldLut = feature.passMaterial.GetTexture("_LutTex1");
		var mat = feature.passMaterial;
		mat.SetTexture("_LutTex0", oldLut);
		mat.SetTexture("_LutTex1", lut);

		mat.SetFloat("_LutBlend", 0);

		float fract = 0;

		while (fract < 1)
		{
			mat.SetFloat("_LutBlend", fract);

			fract += Time.deltaTime / m_BlendTime;
			yield return null;
		}

		mat.SetFloat("_LutBlend", 1);
		interactionEnabled = true;
	}
}
