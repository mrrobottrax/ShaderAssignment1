using UnityEngine.SceneManagement;

public class SceneChangeMessage : MessageBase
{
	public override EMessageFilter Filter => EMessageFilter.ClientOnly;

	public int m_sceneIndex;

	public override void Receive(Peer sender)
	{
		SceneManager.LoadSceneAsync(m_sceneIndex);
		NetworkManager.m_localClient.m_ignoreObjectUpdates = true;
	}
}