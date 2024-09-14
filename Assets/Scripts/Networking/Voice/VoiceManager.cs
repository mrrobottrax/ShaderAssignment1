using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

internal class VoiceManager : MonoBehaviour
{
	const uint k_bufferSize = 20 * 1024;


	static VoiceManager s_instance;
	public static VoiceManager Instance { get { return s_instance; } }

	[RuntimeInitializeOnLoadMethod]
	static void Init()
	{
		s_instance = new GameObject("Voice Manager").AddComponent<VoiceManager>();
		DontDestroyOnLoad(s_instance);
	}


	struct PlayerBuffer
	{
		public byte[] m_buffer;
		public int m_end;
		public AudioSource m_audioSource;
	}


	bool m_recording = false;
	uint m_sampleRate;


	readonly Dictionary<SteamNetworkingIdentity, PlayerBuffer> m_playerBuffers = new();


	private void Awake()
	{
		NetworkManager.OnModeChange += OnModeChange;
		TickManager.OnTick += OnTick;
	}

	private void OnDestroy()
	{
		NetworkManager.OnModeChange -= OnModeChange;
		TickManager.OnTick -= OnTick;
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

	private void OnTick()
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

				NetworkManager.SendMessageAll(EMessageType.VoiceData, pData, (int)nBytesWritten, ESteamNetworkingSend.k_nSteamNetworkingSend_Unreliable);
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
		// Check if receiving before player spawns
		if (sender.m_player == null)
		{
			Debug.LogWarning("Receiving mic data before player spawned");
			return;
		}

		// Check if peer needs a buffer
		if (!m_playerBuffers.TryGetValue(sender.m_identity, out PlayerBuffer bufferStruct))
		{
			bufferStruct = new()
			{
				m_buffer = new byte[k_bufferSize],
				m_end = 0,
				m_audioSource = sender.m_player.gameObject.AddComponent<AudioSource>()
			};

			m_playerBuffers.Add(sender.m_identity, bufferStruct);
		}

		// Copy into byte array
		byte[] buffer = new byte[message.m_cbSize - 1];
		Marshal.Copy(message.m_pData + 1, buffer, 0, buffer.Length);

		byte[] pPlayBuffer = bufferStruct.m_buffer;

		// Decompress
		EVoiceResult result = SteamUser.DecompressVoice(
			buffer, (uint)buffer.Length,
			pPlayBuffer, (uint)pPlayBuffer.Length,
			out uint nBytesWritten, m_sampleRate
		);

		if (result != EVoiceResult.k_EVoiceResultOK)
		{
			Debug.LogWarning(result);
			return;
		}

		AudioClip audioClip = AudioClip.Create("Test", (int)nBytesWritten, 1, (int)m_sampleRate, true);
		bufferStruct.m_audioSource.PlayOneShot(audioClip, 1);
	}
}
