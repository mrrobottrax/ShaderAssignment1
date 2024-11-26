using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFCGenerator))]
class WFCGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		WFCGenerator gen = target as WFCGenerator;

		if (GUILayout.Button("Setup", new GUILayoutOption[] { }))
		{
			gen.Setup();
		}

		if (GUILayout.Button("Step", new GUILayoutOption[] { }))
		{
			gen.Step();
		}
	}
}