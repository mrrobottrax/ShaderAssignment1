using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;

[StructLayout(LayoutKind.Sequential)]
internal class SceneChangeMessage : Message
{
	public override EMessageFilter Filter => EMessageFilter.ClientOnly;

	public int m_sceneIndex;

	public SceneChangeMessage()
	{
		m_sceneIndex = SceneManager.GetActiveScene().buildIndex;
	}

	public override void Receive(Peer sender)
	{
		SceneManager.LoadSceneAsync(m_sceneIndex);

		if (NetworkManager.m_localClient)
			NetworkManager.m_localClient.m_ignoreObjectUpdates = true;
	}
}