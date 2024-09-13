using UnityEngine;
using Steamworks;
using System.Runtime.InteropServices;

public enum ENetworkMode
{
	None = 0,
	Host,
	Client
}

public static class NetworkManager
{
	#region API
	public static ENetworkMode Mode { get { return m_mode; } }

	public static void SetGameJoinable(bool joinable)
	{
		if (m_mode == ENetworkMode.Client)
		{
			Debug.LogWarning("Joining from clients is not implemented yet!");
			return;
		}

		// Enable/disable joining games
		if (joinable)
		{
			SteamFriends.SetRichPresence("connect", "+connect " + SteamUser.GetSteamID().m_SteamID);
		}
		else
		{
			SteamFriends.SetRichPresence("connect", null);
		}
	}

	public static void StartHosting()
	{
		m_mode = ENetworkMode.Host;

		// Create host script
		ClearHostAndClient();

		m_host = new GameObject("Host Logic").AddComponent<Host>();
		Object.DontDestroyOnLoad(m_host.gameObject);
	}

	public static int GetPlayerNetID()
	{
		if (m_mode == ENetworkMode.Host)
		{
			return Host.m_player.m_netID;
		}
		else
		{
			return LocalClient.m_playerObjectID;
		}
	}

	public static NetworkObject GetLocalPlayer()
	{
		if (m_mode == ENetworkMode.Host)
		{
			return Host.m_player;
		}
		else
		{
			return NetworkObjectManager.GetNetworkObject(GetPlayerNetID());
		}
	}
	#endregion


	static Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

	internal static Host m_host;
	internal static LocalClient m_localClient;
	internal static ENetworkMode m_mode;

	internal static bool m_tickFrame = false;

	[RuntimeInitializeOnLoadMethod]
	static void Initialize()
	{
		m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);

		// Starts up steam relay, causing a small delay.
		// VALVe recommends running this at game start if relay use is expected.
		SteamNetworkingUtils.InitRelayNetworkAccess();
	}

	private static void ClearHostAndClient()
	{
		if (m_localClient != null)
		{
			Object.Destroy(m_localClient.gameObject);
		}

		if (m_host != null)
		{
			Object.Destroy(m_host.gameObject);
		}
	}

	private static void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t pCallback)
	{
		// We want to join off a player
		Debug.Log("Attempting to join game...");

		SteamNetworkingIdentity pIdentity = new();
		pIdentity.SetSteamID(pCallback.m_steamIDFriend);

		// Attempt to connect to host
		JoinGame(pIdentity);
	}

	internal static void JoinGame(SteamNetworkingIdentity server)
	{
		m_mode = ENetworkMode.Client;

		// Create client script
		ClearHostAndClient();

		m_localClient = new GameObject("Client Logic").AddComponent<LocalClient>();
		Object.DontDestroyOnLoad(m_localClient.gameObject);

		m_localClient.Init(server);
	}

	public static void SendAll(ESnapshotMessageType messageType, System.IntPtr pData, int cbLength, ESteamNetworkingSend sendType)
	{
		if (!SteamManager.Initialized)
			return;

		// Allocate a new buffer to hold the messageType byte and the original data
		byte[] buffer = new byte[cbLength + 1];
		buffer[0] = (byte)messageType;

		// Copy the original data to the new buffer, starting at index 1
		Marshal.Copy(pData, buffer, 1, cbLength);

		// Pin the buffer in memory and get a pointer to it
		GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			System.IntPtr pBuffer = handle.AddrOfPinnedObject();

			// Send the message
			if (m_mode == ENetworkMode.Host)
			{
				foreach (var client in Host.m_clients.Values)
				{
					SteamNetworkingSockets.SendMessageToConnection(client.m_hConn, pBuffer, (uint)buffer.Length, (int)sendType, out _);
				}
			}
			else
			{
				SteamNetworkingSockets.SendMessageToConnection(LocalClient.m_hConn, pBuffer, (uint)buffer.Length, (int)sendType, out _);
			}
		}
		finally
		{
			handle.Free();
		}
	}

	internal static void SendMessage(ESnapshotMessageType messageType, string message, ESteamNetworkingSend sendType, HSteamNetConnection hConn)
	{
		// Get the pointer to the message object
		System.IntPtr pMessage = Marshal.StringToHGlobalAnsi(message);

		try
		{
			// Send the message
			SendMessage(messageType, pMessage, message.Length, sendType, hConn);
		}
		finally
		{
			Marshal.FreeHGlobal(pMessage);
		}
	}

	internal static void SendMessage<T>(ESnapshotMessageType messageType, T message, ESteamNetworkingSend sendType, HSteamNetConnection hConn) where T : struct
	{
		// Pin the message object in memory
		GCHandle handle = GCHandle.Alloc(message, GCHandleType.Pinned);
		try
		{
			// Get the pointer to the message object
			System.IntPtr pMessage = handle.AddrOfPinnedObject();

			// Send the message
			SendMessage(messageType, pMessage, Marshal.SizeOf(message), sendType, hConn);
		}
		finally
		{
			// Free the pinned object
			handle.Free();
		}
	}

	internal static void SendMessage(ESnapshotMessageType messageType, System.IntPtr pData, int cbLength, ESteamNetworkingSend sendType, HSteamNetConnection hConn)
	{
		if (!SteamManager.Initialized)
			return;

		// Allocate a new buffer to hold the messageType byte and the original data
		byte[] buffer = new byte[cbLength + 1];
		buffer[0] = (byte)messageType;

		// Copy the original data to the new buffer, starting at index 1
		Marshal.Copy(pData, buffer, 1, cbLength);

		// Pin the buffer in memory and get a pointer to it
		GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			System.IntPtr pBuffer = handle.AddrOfPinnedObject();

			// Send the message
			SteamNetworkingSockets.SendMessageToConnection(hConn, pBuffer, (uint)buffer.Length, (int)sendType, out _);
		}
		finally
		{
			handle.Free();
		}
	}
}
