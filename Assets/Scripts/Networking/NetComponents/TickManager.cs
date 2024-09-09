using UnityEngine;
using UnityEngine.EventSystems;

internal class TickManager : MonoBehaviour
{
	static float m_nextTickTime = 0;
	static bool m_shouldTick = true;

	[RuntimeInitializeOnLoadMethod]
	static void Initialize()
	{
		new GameObject("Tick Manager").AddComponent<TickManager>();
	}

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		m_shouldTick = false;

		if (Time.time >= m_nextTickTime)
		{
			m_nextTickTime = Time.time + NetworkData.GetTickDelta();
			m_shouldTick = true;
		}
	}

	public static bool ShouldTick()
	{
		return m_shouldTick;
	}
}