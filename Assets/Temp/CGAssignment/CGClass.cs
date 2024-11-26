using UnityEngine;

public class CGClass : MonoBehaviour
{
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.H))
		{
			RenderPipelineInstance.DisableTextures = !RenderPipelineInstance.DisableTextures;
		}
	}
}