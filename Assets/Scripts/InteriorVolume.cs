using UnityEngine;

public class InteriorVolume : MonoBehaviour
{
	float m_prevAmbientIntensity = 0;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.Equals(NetworkManager.GetLocalPlayer().gameObject))
		{
			m_prevAmbientIntensity = RenderSettings.ambientIntensity;
			RenderSettings.ambientIntensity = 0.1f;
			RenderSettings.reflectionIntensity = 0.0f;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.Equals(NetworkManager.GetLocalPlayer().gameObject))
		{
			RenderSettings.ambientIntensity = m_prevAmbientIntensity;
		}
	}
}
