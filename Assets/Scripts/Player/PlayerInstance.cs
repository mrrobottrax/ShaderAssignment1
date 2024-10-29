using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInstance : NetworkBehaviour
{
	[SerializeField] private PlayerUIInstance UIPrefab;

	[SerializeField] GameObject[] m_firstPersonObjects;
	[SerializeField] GameObject[] m_thirdPersonObjects;

	[Header("System")]
	private PlayerUIInstance playerUI;
	PlayerController m_controller;

	#region Initialization Methods

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

			playerUI = Instantiate(UIPrefab, transform);

			TrySpawn();
		}
		else
		{
			SetLocalOnlyStuffEnabled(false);
		}
	}
	#endregion

	#region Unity Callbacks

	private void OnDestroy()
	{
		if (IsOwner)
		{
			SceneManager.activeSceneChanged -= OnSceneLoad;
		}
	}
	#endregion

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
		TrySpawn();
	}

	void TrySpawn()
	{
		GameObject spawn = GameObject.FindGameObjectWithTag("Player Spawn");
		if (spawn != null)
		{
			m_controller.Teleport(spawn.transform.position + Vector3.up * 0.05f);
		}
		else
		{
			Debug.LogWarning("No spawn found in level");
		}
	}

	#region Utility Methods

	public PlayerUIInstance GetPlayersUI()
	{
		return playerUI;
	}

    #endregion
}