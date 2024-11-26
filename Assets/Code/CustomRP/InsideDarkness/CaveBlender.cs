using UnityEngine;

public class CaveBlender : MonoBehaviour
{
	[SerializeField] Vector3 m_EntranceDir;

	const string ambientName = "_AmbientFactor";

	void OnTriggerExit(Collider col)
	{
		// Find materials with ambient factor
		Renderer[] renderers = col.GetComponentsInChildren<Renderer>();
		foreach (var renderer in renderers)
		{
			for (int i = 0; i < renderer.sharedMaterials.Length; ++i)
			{
				int propCount = renderer.sharedMaterials[i].shader.GetPropertyCount();
				for (int j = 0; j < propCount; ++j)
				{
					string name = renderer.sharedMaterials[i].shader.GetPropertyName(j);

					if (name == ambientName)
					{
						// Get direction of exit

						if (Vector3.Dot(renderer.transform.position - transform.position, m_EntranceDir) > 0)
						{
							TransitionAmbient(renderer.materials[i], 0);
						}
						else
						{
							TransitionAmbient(renderer.materials[i], 1);
						}
					}
				}
			}
		}
	}

	void TransitionAmbient(Material mat, float value)
	{
		mat.SetFloat(ambientName, value);
	}
}
