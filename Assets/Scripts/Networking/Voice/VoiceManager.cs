using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.VersionControl;
using UnityEngine;

internal class VoiceManager : MonoBehaviour
{
	const int k_bufferSize = 20 * 1024; // Must be even

	[SerializeField] float m_gain = 4;

	static VoiceManager s_instance;
	public static VoiceManager Instance { get { return s_instance; } }

	[RuntimeInitializeOnLoadMethod]
	static void Init()
	{
		s_instance = new GameObject("Voice Manager").AddComponent<VoiceManager>();
		DontDestroyOnLoad(s_instance);
	}

	class Buffer
	{
		public byte[] m_buffer = new byte[k_bufferSize];
		public int m_nBytes;
	}

	class PlayerBuffer
	{
		public Queue<Buffer> m_buffers;
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

				NetworkManager.SendMessageAll(EMessageType.VoiceData, pData, (int)nBytesWritten, ESteamNetworkingSend.k_nSteamNetworkingSend_Unreliable);

				Debug.Log("Send");
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
		Debug.Log("Receive");

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
				m_buffers = new Queue<Buffer>(),
				m_position = 0,
				m_audioSource = sender.m_player.gameObject.AddComponent<AudioSource>()
			};

			m_playerBuffers.Add(sender.m_identity, playBuffer);

			AudioClip audioClip = AudioClip.Create(
				$"{sender.m_identity}",
				(int)m_sampleRate * 1024,
				1,
				(int)m_sampleRate,
				true,
				(float[] data) => OnPCMReader(data, playBuffer),
				(int pos) => OnPCMSetPos(pos, playBuffer)
			);

			playBuffer.m_audioSource.loop = false;
			playBuffer.m_audioSource.clip = audioClip;
			playBuffer.m_audioSource.volume = 1;
			playBuffer.m_audioSource.spatialBlend = 1;
			playBuffer.m_audioSource.spatialize = true;
			playBuffer.m_audioSource.Play();
		}

		// Copy into byte array
		byte[] buffer = new byte[message.m_cbSize - 1];
		Marshal.Copy(message.m_pData + 1, buffer, 0, buffer.Length);

		// Decompress into play buffer
		Buffer queueBuffer = new();

		EVoiceResult result = SteamUser.DecompressVoice(
			buffer, (uint)buffer.Length,
			queueBuffer.m_buffer, (uint)queueBuffer.m_buffer.Length,
			out uint nBytesWritten, m_sampleRate
		);

		queueBuffer.m_nBytes = (int)nBytesWritten;

		if (result != EVoiceResult.k_EVoiceResultOK)
		{
			Debug.LogWarning(result);
			return;
		}

		playBuffer.m_buffers.Enqueue(queueBuffer);

		//Debug.Log("Enqueue " + playBuffer.m_buffers.Count);
	}

	void OnPCMReader(float[] data, PlayerBuffer playBuffer)
	{
		if (playBuffer.m_buffers.Count == 0)
		{
			//Debug.Log("Zero 1");
			for (int j = 0; j < data.Length; ++j)
			{
				data[j] = 0;
			}
			return;
		}

		Buffer buffer = playBuffer.m_buffers.Peek();

		// Convert to floats
		for (int i = 0; i < data.Length; i++)
		{
			// Check if we've read the whole buffer
			playBuffer.m_position += 2;
			if (playBuffer.m_position >= buffer.m_nBytes)
			{
				playBuffer.m_position = 0;
				playBuffer.m_buffers.Dequeue();
				//Debug.Log("Dequeue " + playBuffer.m_buffers.Count);

				if (playBuffer.m_buffers.Count == 0)
				{
					//Debug.Log("Zero 2");
					for (int j = i; j < data.Length; ++j)
					{
						data[j] = 0;
					}
					return;
				}

				buffer = playBuffer.m_buffers.Peek();
			}

			short value = BitConverter.ToInt16(buffer.m_buffer, playBuffer.m_position);
			data[i] = value / 32768.0f;

			// Compressor
			data[i] *= m_gain;
		}
	}

	void OnPCMSetPos(int pos, PlayerBuffer playBuffer)
	{
		_ = pos;
	}
}
