using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

public class ModelImporter : AssetPostprocessor
{
	public void OnPreprocessMaterialDescription(MaterialDescription desc, Material material, AnimationClip[] clips)
	{
		// List<string> floatNames = new();
		// desc.GetFloatPropertyNames(floatNames);

		// List<string> stringNames = new();
		// desc.GetStringPropertyNames(stringNames);

		// List<string> textureNames = new();
		// desc.GetTexturePropertyNames(textureNames);

		// List<string> vecNames = new();
		// desc.GetVector4PropertyNames(vecNames);

		// Debug.Log("Floats");
		// floatNames.ForEach(Debug.Log);
		// Debug.Log("Strings");
		// stringNames.ForEach(Debug.Log);
		// Debug.Log("Textures");
		// textureNames.ForEach(Debug.Log);
		// Debug.Log("Vecs");
		// vecNames.ForEach(Debug.Log);

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
}
