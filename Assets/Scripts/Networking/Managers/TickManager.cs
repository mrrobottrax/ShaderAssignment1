using System;
using UnityEngine;

public class TickManager : MonoBehaviour
{
	float m_nextTickTime = 0;

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
	}

	private void Update()
	{
		if (Time.time >= m_nextTickTime)
		{
			m_nextTickTime = Time.time + NetworkData.GetTickDelta();

			OnTick?.Invoke();
			OnLateTick?.Invoke();
		}
	}
}