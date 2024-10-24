using Steamworks;
using UnityEditor;
using UnityEngine;

public class ModelImporter : AssetPostprocessor
{
	static void OnPostprocessMaterial(Material material)
	{
		material.color = Color.red;
	}
}
