using Steamworks;
using UnityEngine;

[DisallowMultipleComponent]
public class Host : MonoBehaviour
{
	private CallResult<LobbyCreated_t> m_LobbyCreated;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void OnRuntimeStart()
	{
		// Autocreate host when in editor
		// todo: don't autocreate on menus
		if (Application.isEditor)
		{
			new GameObject("Host").AddComponent<Host>();
		}
	}

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	private void OnEnable()
	{
		if (SteamManager.Initialized)
		{
			m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
		}
	}

	private void Start()
	{
		StartHosting();
	}

	private void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure)
	{
		if (bIOFailure || pCallback.m_eResult != EResult.k_EResultOK)
		{
			Debug.LogWarning("Failed to create lobby");
			return;
		}

		CSteamID steamID = new (pCallback.m_ulSteamIDLobby);
		SteamFriends.ActivateGameOverlayInviteDialog(steamID);
	}

	public void StartHosting()
	{
		m_LobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10));
	}
}
