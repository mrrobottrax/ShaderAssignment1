using UnityEditor;
using UnityEngine;

// Provide some UI for testing local clients

[CustomEditor(typeof(ClientTester))]
public class ClientTesterEditor : Editor
{
	int clientCount = 0;

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (!Application.isPlaying)
			return;

		if (GUILayout.Button("Add Client"))
		{
			Client client = new GameObject("Client " + clientCount++).AddComponent<Client>();
			//client.ConnectLoopback();
		}
	}
}
