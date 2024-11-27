using System.Collections;
using System.Linq;
using UnityEngine;

public class LutBlender : Interactable
{
	[SerializeField] float m_BlendTime = 1;
	[SerializeField] Texture3D m_NeutralLUT;
	[SerializeField] Texture3D m_CaveLUT;
	[SerializeField] Texture3D m_OutsideLUT;
	[SerializeField] Texture3D m_TrainLUT;

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
		var mat = RenderPipelineInstance.Singleton.m_Settings.LUTMaterial;
		mat.SetTexture("_LutTex0", m_NeutralLUT);
		mat.SetTexture("_LutTex1", m_NeutralLUT);

		mat.SetFloat("_LutBlend", 1);
		mat.SetFloat("_Contribution", 1);
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

	public void SetLUT(Texture3D lut)
	{
		StartCoroutine(BlendToLut(lut));
	}

	IEnumerator BlendToLut(Texture3D lut)
	{
		interactionEnabled = false;

		var mat = RenderPipelineInstance.Singleton.m_Settings.LUTMaterial;

		var oldLut = mat.GetTexture("_LutTex1");
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