using UnityEngine;

public enum RenderMode
{
	NO_LIGHTING,
	DIFFUSE,
	SPECULAR,
	DIFFUSE_SPECULAR,
}

public class CGClass : MonoBehaviour
{
	public static RenderMode mode = RenderMode.DIFFUSE;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.H))
		{
			RenderPipelineInstance.DisableTextures = !RenderPipelineInstance.DisableTextures;
		}

		if (Input.GetKeyDown(KeyCode.Y))
		{
			mode = RenderMode.NO_LIGHTING;
		}

		if (Input.GetKeyDown(KeyCode.U))
		{
			mode = RenderMode.DIFFUSE;
		}

		if (Input.GetKeyDown(KeyCode.I))
		{
			mode = RenderMode.SPECULAR;
		}

		if (Input.GetKeyDown(KeyCode.O))
		{
			mode = RenderMode.DIFFUSE_SPECULAR;
		}
	}
}