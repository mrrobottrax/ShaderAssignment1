using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
	[SerializeField] GameObject[] m_firstPersonObjects;
	[SerializeField] GameObject[] m_thirdPersonObjects;

	PlayerController m_controller;

	private void Awake()
	{
		m_controller = GetComponent<PlayerController>();
	}

	private void Start()
	{
		if (IsOwner)
		{
			SetLocalOnlyStuffEnabled(true);
			SceneManager.activeSceneChanged += OnSceneLoad;
		}
		else
		{
			SetLocalOnlyStuffEnabled(false);
		}
	}

	private void OnDestroy()
	{
		if (IsOwner)
		{
			SceneManager.activeSceneChanged -= OnSceneLoad;
		}
	}

	private void SetLocalOnlyStuffEnabled(bool enabled)
	{
		foreach (var obj in m_firstPersonObjects)
		{
			obj.SetActive(enabled);
		}

		foreach (var obj in m_thirdPersonObjects)
		{
			obj.SetActive(!enabled);
		}
	}

	void OnSceneLoad(Scene old, Scene newScene)
	{
		GameObject spawn = GameObject.FindGameObjectWithTag("Player Spawn");
		if (spawn != null)
			m_controller.Teleport(spawn.transform.position);
	}
}