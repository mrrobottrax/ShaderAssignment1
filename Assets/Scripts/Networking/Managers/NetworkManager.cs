using UnityEngine;
using Steamworks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

public enum ENetworkMode
{
	None = 0,
	Host,
	Client
}

internal struct MessageID_t
{
	public ushort value;

	public static MessageID_t Read(IntPtr p)
	{
		MessageID_t id;
		id.value = (ushort)Marshal.ReadInt16(p);
		return id;
	}
}

public static class NetworkManager
{
	static Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

	internal static Host m_host;
	internal static Client m_localClient;
	internal static ENetworkMode m_mode;
	internal static SteamNetworkingIdentity m_localIdentity;

	internal static Dictionary<Type, MessageID_t> m_messageToID = new();
	internal static Dictionary<MessageID_t, Type> m_IdToMessage = new();

	public static void Init()
	{
		m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);

		// Starts up steam relay, causing a small delay.
		// VALVe recommends running this at game start if relay use is expected.
		SteamNetworkingUtils.InitRelayNetworkAccess();

		if (!SteamNetworkingSockets.GetIdentity(out m_localIdentity))
		{
			Debug.LogError("Failed to get identity from Steam");
		}

		// Get an ID for each message type

		var types = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(p => typeof(MessageBase).IsAssignableFrom(p));

		MessageID_t id;
		id.value = 0;
		foreach (var type in types)
		{
			//Debug.Log(type.ToString());

			m_messageToID.Add(type, id);
			m_IdToMessage.Add(id, type);

			if (id.value >= 255)
			{
				Debug.LogError("Out of message IDs");
				break;
			}

			++id.value;
		}
	}

	// Destroy host and client objects
	private static void ClearHostAndClient()
	{
		if (m_localClient != null)
		{
			UnityEngine.Object.Destroy(m_localClient.gameObject);
		}

		if (m_host != null)
		{
			UnityEngine.Object.Destroy(m_host.gameObject);
		}
	}

	// Called when trying to join a game through Steam
	private static void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t pCallback)
	{
		// We want to join off a player
		Debug.Log("Attempting to join game...");

		SteamNetworkingIdentity pIdentity = new();
		pIdentity.SetSteamID(pCallback.m_steamIDFriend);

		// Attempt to connect to host
		JoinGame(pIdentity);
	}

	internal static IEnumerable<Peer> GetAllPeers()
	{
		if (m_mode == ENetworkMode.Host)
		{
			return m_host.m_clients.Values;
		}
		else if (m_mode == ENetworkMode.Client)
		{
			var iter = m_localClient.m_peers.Values;
			iter.Append(m_localClient.m_hostPeer);
			return iter;
		}

		throw new NotImplementedException();
	}

	internal static void ProcessMessage(SteamNetworkingMessage_t message, Peer sender)
	{
		MessageID_t id = MessageID_t.Read(message.m_pData);
		Type type = m_IdToMessage[id];

		// Use the MakeGenericMethod to create a generic method for PtrToStructure
		MethodInfo method = typeof(Marshal).GetMethod("PtrToStructure", new Type[] { typeof(IntPtr) });
		MethodInfo generic = method.MakeGenericMethod(type);
		MessageBase messageObj = (MessageBase)generic.Invoke(null, new object[] { message.m_pData + Marshal.SizeOf(id) });

		// Filter message

		if (messageObj.Filter == MessageBase.EMessageFilter.All ||
			messageObj.Filter == MessageBase.EMessageFilter.ClientOnly && m_mode == ENetworkMode.Client ||
			messageObj.Filter == MessageBase.EMessageFilter.HostOnly && m_mode == ENetworkMode.Host)
		{
			messageObj.Receive(sender);
		}
	}

	#region Send Functions

	public static void SendMessage<T>(T message, Peer target) where T : MessageBase
	{
		if (!SteamManager.Initialized)
			return;

		Type messageType = message.GetType();
		MessageID_t typeID = m_messageToID[messageType];

		int cbType = Marshal.SizeOf(typeID);
		int cbMessage = Marshal.SizeOf(messageType);
		int cbBuffer = cbMessage + cbType;

		IntPtr pBuffer = Marshal.AllocHGlobal(cbBuffer);
		try
		{
			IntPtr pId = pBuffer;
			IntPtr pMessage = pBuffer + cbType;

			Marshal.StructureToPtr(typeID, pId, false);
			Marshal.StructureToPtr(message, pMessage, false);

			int sendType = (int)(
				message.Reliable ?
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable :
				ESteamNetworkingSend.k_nSteamNetworkingSend_Unreliable);


			SteamNetworkingSockets.SendMessageToConnection(target.m_hConn, pBuffer, (uint)cbBuffer, sendType, out _);
		}
		finally
		{
			Marshal.FreeHGlobal(pBuffer);
		}
	}

	public static void BroadcastMessage<T>(T message) where T : MessageBase
	{
		if (!SteamManager.Initialized)
			return;

		Type messageType = message.GetType();
		MessageID_t typeID = m_messageToID[messageType];

		int cbType = Marshal.SizeOf(typeID);
		int cbMessage = Marshal.SizeOf(messageType);
		int cbBuffer = cbMessage + cbType;

		IntPtr pBuffer = Marshal.AllocHGlobal(cbBuffer);
		try
		{
			IntPtr pId = pBuffer;
			IntPtr pMessage = pBuffer + cbType;

			Marshal.StructureToPtr(typeID, pId, false);
			Marshal.StructureToPtr(message, pMessage, false);

			int sendType = (int)(
				message.Reliable ?
				ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable :
				ESteamNetworkingSend.k_nSteamNetworkingSend_Unreliable);

			if (message.Peer2Peer)
			{
				// Send to all peers
				foreach (var peer in GetAllPeers())
				{
					SteamNetworkingSockets.SendMessageToConnection(peer.m_hConn, pBuffer, (uint)cbBuffer, sendType, out _);
				}
			}
			else
			{
				// Send to host if client, send to all if host
				if (m_mode == ENetworkMode.Client)
				{
					SteamNetworkingSockets.SendMessageToConnection(m_localClient.m_hostPeer.m_hConn, pBuffer, (uint)cbBuffer, sendType, out _);
				}
				else
				{
					// Send to all peers
					foreach (var peer in GetAllPeers())
					{
						SteamNetworkingSockets.SendMessageToConnection(peer.m_hConn, pBuffer, (uint)cbBuffer, sendType, out _);
					}
				}
			}
		}
		finally
		{
			Marshal.FreeHGlobal(pBuffer);
		}
	}

	#endregion

	#region Public API

	public static ENetworkMode Mode { get { return m_mode; } }

	public delegate void ModeChange(ENetworkMode mode);
	public static ModeChange OnModeChange;

	public static SteamNetworkingIdentity LocalIdentity
	{
		get
		{
			return m_localIdentity;
		}
	}

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

	public static void StartHosting(bool spawnPlayerImmediate = false)
	{
		m_mode = ENetworkMode.Host;
		OnModeChange?.Invoke(m_mode);

		// Create host script
		ClearHostAndClient();

		m_host = new GameObject("Host Logic").AddComponent<Host>();
		UnityEngine.Object.DontDestroyOnLoad(m_host.gameObject);

		if (spawnPlayerImmediate)
			m_host.AddPlayer();
	}

	public static void JoinGame(SteamNetworkingIdentity server)
	{
		m_mode = ENetworkMode.Client;
		OnModeChange?.Invoke(m_mode);

		// Create client script
		ClearHostAndClient();

		m_localClient = new GameObject("Client Logic").AddComponent<Client>();
		UnityEngine.Object.DontDestroyOnLoad(m_localClient.gameObject);

		m_localClient.Connect(server);
	}

	public static NetworkObject GetLocalPlayer()
	{
		if (m_mode == ENetworkMode.Host)
		{
			return m_host.m_player;
		}
		else
		{
			return m_localClient.m_player;
		}
	}

	public static int GetPlayerCount()
	{
		return GetAllPeers().Count() + 1;
	}

	public static NetworkObject[] GetAllPlayers()
	{
		NetworkObject[] players = new NetworkObject[GetPlayerCount()];

		players[0] = GetLocalPlayer();

		int i = 1;
		foreach (var peer in GetAllPeers())
		{
			players[i] = peer.m_player;

			++i;
		}

		return players;
	}

	public static SteamNetworkingIdentity GetServerIdentity()
	{
		if (m_mode == ENetworkMode.Host)
		{
			return m_localIdentity;
		}
		else if (m_mode == ENetworkMode.Client)
		{
			return m_localClient.m_hostPeer.m_identity;
		}

		return new SteamNetworkingIdentity();
	}

	#endregion
}
