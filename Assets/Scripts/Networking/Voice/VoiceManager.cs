using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor.VersionControl;
using UnityEngine;

internal class VoiceManager : MonoBehaviour
{
	const int k_bufferSize = 20 * 1024; // Must be even

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
		public int m_end;
		public int m_position;
		public AudioSource m_audioSource;
	}


	bool m_recording = false;
	uint m_sampleRate;


	readonly Dictionary<SteamNetworkingIdentity, PlayerBuffer> m_playerBuffers = new();


	private void Awake()
	{
		NetworkManager.OnModeChange += OnModeChange;
		//TickManager.OnTick += OnTick;
	}

	private void OnDestroy()
	{
		NetworkManager.OnModeChange -= OnModeChange;
		//TickManager.OnTick -= OnTick;
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

	private void Update()
	{
		if (!m_recording) return;

		EVoiceResult result = SteamUser.GetAvailableVoice(out uint cbCompressed);

		if (result == EVoiceResult.k_EVoiceResultOK)
		{
			byte[] voiceBuffer = new byte[cbCompressed];
			SteamUser.GetVoice(true, voiceBuffer, (uint)voiceBuffer.Length, out uint nBytesWritten);

			if (nBytesWritten == 0) return;

			// send voice data
			GCHandle handle = GCHandle.Alloc(voiceBuffer, GCHandleType.Pinned);

			try
			{
				IntPtr pData = handle.AddrOfPinnedObject();

				NetworkManager.SendMessageAll(EMessageType.VoiceData, pData, (int)nBytesWritten, ESteamNetworkingSend.k_nSteamNetworkingSend_Reliable);

				//Debug.Log("Send");
			}
			finally
			{
				handle.Free();
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
				m_audioSource = sender.m_player.gameObject.GetComponent<AudioSource>()
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

		// Copy into byte array
		byte[] compressedBuffer = new byte[message.m_cbSize - 1];
		Marshal.Copy(message.m_pData + 1, compressedBuffer, 0, compressedBuffer.Length);

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
			}
		}


		//Debug.Log("Enqueue " + playBuffer.m_buffers.Count);
	}

	void OnPCMReader(float[] data, PlayerBuffer playBuffer)
	{
		// Convert to floats
		int i;
		for (i = 0; i < data.Length; i++)
		{
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
	}
}
