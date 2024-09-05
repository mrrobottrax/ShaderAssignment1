using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamScript : MonoBehaviour
{
	void Start()
	{
		if (!SteamManager.Initialized)
			return;

		SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10);
	}
}
