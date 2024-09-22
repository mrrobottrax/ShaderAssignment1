using System;
using UnityEngine;

public class TickManager : MonoBehaviour
{
	float m_nextTickTime;

	public static Action OnTick;
	public static Action OnLateTick;

	[RuntimeInitializeOnLoadMethod]
	static void Initialize()
	{
		new GameObject("Tick Manager").AddComponent<TickManager>();
	}

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);

		m_nextTickTime = Time.time;
	}

	private void Update()
	{
		if (Time.time >= m_nextTickTime)
		{
			m_nextTickTime += NetworkData.GetTickDelta();

			OnTick?.Invoke();
			OnLateTick?.Invoke();
		}
	}
}