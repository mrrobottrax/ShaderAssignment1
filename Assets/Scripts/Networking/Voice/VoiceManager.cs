using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

internal class VoiceManager : MonoBehaviour
{
	const int k_bufferSize = 200 * 1024; // Must be even
	[SerializeField] float k_seperateMessageThreshold = 0.1f; // Messages this far apart are considered seperate
	[SerializeField] int k_bufferCooldown = 32768;

	[SerializeField] float m_gain = 8;

	static VoiceManager s_instance;
	public static VoiceManager Instance { get { return s_instance; } }

	[RuntimeInitializeOnLoadMethod]
	static void Init()
	{
		s_instance = new GameObject("Voice Manager").AddComponent<VoiceManager>();
		DontDestroyOnLoad(s_instance);
	}

	class PlayerBuffer
	{
		public byte[] m_buffer = new byte[k_bufferSize];
		public int m_end = 0;
		public int m_position = 0;
		public int m_winding = 0;
		public int m_bufferCooldown = 0;
		public AudioSource m_audioSource;
	}


	bool m_recording = false;
	uint m_sampleRate;
	float m_lastMessageTime = -100.0f;

	readonly Dictionary<SteamNetworkingIdentity, PlayerBuffer> m_playerBuffers = new();


	private void Awake()
	{
		NetworkManager.OnModeChange += OnModeChange;
		TickManager.OnTick += Tick;
	}

	private void OnDestroy()
	{
		NetworkManager.OnModeChange -= OnModeChange;
		TickManager.OnTick -= Tick;
	}

	void OnModeChange(ENetworkMode mode)
	{
		Debug.Log($"Mode chage: {mode}");

		if (mode == ENetworkMode.None)
		{
			StopRecording();
		}
		else
		{
			StartRecording();
		}
	}

	public void StartRecording()
	{
		Debug.Log("Start recording");

		SteamUser.StartVoiceRecording();
		m_sampleRate = SteamUser.GetVoiceOptimalSampleRate();

		Debug.Log($"Sample rate: {m_sampleRate}");

		m_recording = true;

		SteamFriends.SetInGameVoiceSpeaking(new CSteamID(), true);
	}

	public void StopRecording()
	{
		Debug.Log("Stop recording");

		SteamUser.StopVoiceRecording();
		m_recording = false;

		m_playerBuffers.Clear();
	}

	private void Tick()
	{
		if (!m_recording) return;

		EVoiceResult result = SteamUser.GetAvailableVoice(out uint cbCompressed);

		if (result == EVoiceResult.k_EVoiceResultOK)
		{
			byte[] voiceBuffer = new byte[cbCompressed];
			SteamUser.GetVoice(true, voiceBuffer, (uint)voiceBuffer.Length, out uint nBytesWritten);

			if (nBytesWritten == 0) return;

			// Check if this message is seperate from the last one
			VoiceDataMessage voiceDataMessage = new();
			if (Time.time - m_lastMessageTime >= k_seperateMessageThreshold)
			{
				voiceDataMessage.m_isSeperate = true;
				//Debug.Log("New message");
			}
			else
			{
				voiceDataMessage.m_isSeperate = false;
			}
			m_lastMessageTime = Time.time;

			byte[] data = new byte[nBytesWritten + Marshal.SizeOf<VoiceDataMessage>()];

			GCHandle voiceBufferHandle = GCHandle.Alloc(voiceBuffer, GCHandleType.Pinned);
			GCHandle messageHandle = GCHandle.Alloc(voiceDataMessage, GCHandleType.Pinned);
			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			try
			{
				IntPtr pVoiceData = voiceBufferHandle.AddrOfPinnedObject();
				IntPtr pMessage = messageHandle.AddrOfPinnedObject();
				IntPtr pData = dataHandle.AddrOfPinnedObject();

				// Copy into one big array
				Marshal.Copy(pMessage, data, 0, Marshal.SizeOf<VoiceDataMessage>());
				Marshal.Copy(pVoiceData, data, Marshal.SizeOf<VoiceDataMessage>(), (int)nBytesWritten);

				NetworkManager.SendMessageAll(EMessageType.VoiceData, pData, data.Length, ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable);

				// loopback
				//{
				//	SteamNetworkingMessage_t message1 = new()
				//	{
				//		m_pData = pData - 1,
				//		m_cbSize = data.Length + 1
				//	};

				//	SteamNetworkingSockets.GetIdentity(out SteamNetworkingIdentity identity);
				//	Peer peer = new(new HSteamNetConnection(), identity, NetworkManager.GetLocalPlayer());
				//	ReceiveVoice(message1, peer);
				//}
				//Debug.Log("Send");
			}
			finally
			{
				voiceBufferHandle.Free();
				messageHandle.Free();
				dataHandle.Free();
			}
		}
		else if (result != EVoiceResult.k_EVoiceResultNoData)
		{
			Debug.LogWarning(result);
		}
	}

	public void ReceiveVoice(SteamNetworkingMessage_t message, Peer sender)
	{
		//Debug.Log("Receive");

		// Check if receiving before player spawns
		if (sender.m_player == null)
		{
			Debug.LogWarning("Receiving mic data before player spawned");
			return;
		}

		// Check if peer needs a buffer
		if (!m_playerBuffers.TryGetValue(sender.m_identity, out PlayerBuffer playBuffer))
		{
			playBuffer = new()
			{
				m_audioSource = sender.m_player.gameObject.GetComponent<AudioSource>(),
				m_bufferCooldown = k_bufferCooldown,
			};

			m_playerBuffers.Add(sender.m_identity, playBuffer);

			AudioClip audioClip = AudioClip.Create(
				$"{sender.m_identity}",
				(int)m_sampleRate * 2,
				1,
				(int)m_sampleRate,
				true,
				(float[] data) => OnPCMReader(data, playBuffer),
				(int pos) => OnPCMSetPos(pos, playBuffer)
			);

			playBuffer.m_audioSource.loop = true;
			playBuffer.m_audioSource.clip = audioClip;
			playBuffer.m_audioSource.volume = 1;
			playBuffer.m_audioSource.spatialBlend = 1;
			playBuffer.m_audioSource.spatialize = true;
			playBuffer.m_audioSource.Play();
		}

		// Check if this message is seperate
		VoiceDataMessage voiceData = Marshal.PtrToStructure<VoiceDataMessage>(message.m_pData + sizeof(EMessageType));
		if (voiceData.m_isSeperate)
		{
			playBuffer.m_bufferCooldown = k_bufferCooldown;
		}

		// Copy into byte array
		byte[] compressedBuffer = new byte[message.m_cbSize - sizeof(EMessageType) - Marshal.SizeOf<VoiceDataMessage>()];
		Marshal.Copy(message.m_pData + sizeof(EMessageType) + Marshal.SizeOf<VoiceDataMessage>(), compressedBuffer, 0, compressedBuffer.Length);

		// Decompress into buffer
		byte[] rawBuffer = new byte[k_bufferSize];

		EVoiceResult result = SteamUser.DecompressVoice(
			compressedBuffer, (uint)compressedBuffer.Length,
			rawBuffer, (uint)rawBuffer.Length,
			out uint nBytesWritten, m_sampleRate
		);


		if (result != EVoiceResult.k_EVoiceResultOK)
		{
			Debug.LogWarning(result);
			return;
		}

		// Copy into play buffer
		for (int i = 0; i < nBytesWritten; ++i)
		{
			playBuffer.m_buffer[playBuffer.m_end] = rawBuffer[i];

			playBuffer.m_end++;
			// Loop around
			if (playBuffer.m_end >= k_bufferSize)
			{
				playBuffer.m_end = 0;
				playBuffer.m_winding -= 1;
			}
		}

		if (playBuffer.m_winding < -2 || playBuffer.m_winding > 2)
		{
			Debug.LogWarning($"Voice buffer winding at {playBuffer.m_winding}");
		}

		//Debug.Log("Enqueue " + playBuffer.m_buffers.Count);
	}

	void OnPCMReader(float[] data, PlayerBuffer playBuffer)
	{
		// Convert to floats
		int i;
		for (i = 0; i < data.Length; i++)
		{
			// Add a little cooldown to messages to give time to buffer
			if (playBuffer.m_bufferCooldown > 0)
			{
				data[i] = 0;
				playBuffer.m_bufferCooldown -= 1;
				continue;
			}

			// Check if we've read the whole buffer
			if (playBuffer.m_position == playBuffer.m_end)
			{
				break;
			}

			short value = BitConverter.ToInt16(playBuffer.m_buffer, playBuffer.m_position);
			data[i] = value / 32768.0f;

			// Gain
			data[i] *= m_gain;

			playBuffer.m_position += 2;
			// Loop around
			if (playBuffer.m_position >= k_bufferSize)
			{
				playBuffer.m_position = 0;
				playBuffer.m_winding += 1;
			}
		}

		if (i == data.Length) return; // Sucessfully set all data

		// Set the rest to 0
		for (; i < data.Length; i++)
		{
			data[i] = 0;
		}
	}

	void OnPCMSetPos(int pos, PlayerBuffer playBuffer)
	{
		_ = pos;
		_ = playBuffer;
	}
}
