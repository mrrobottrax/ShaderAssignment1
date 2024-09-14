using Steamworks;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class VoiceManager : MonoBehaviour
{
	[RuntimeInitializeOnLoadMethod()]
	static void Init()
	{
		new GameObject("Voice Manager").AddComponent<VoiceManager>();
		NetworkManager.OnModeChange += OnModeChange;
	}

	static bool m_recording = false;
	static uint m_sampleRate;

	static readonly byte[] m_playBackBuffer = new byte[20 * 1024];
	static uint m_bufferFill = 0;

	static void OnModeChange(ENetworkMode mode)
	{
		if (mode == ENetworkMode.None)
		{
			StopRecording();
		}
		else
		{
			StartRecording();
		}
	}

	public static void StartRecording()
	{
		SteamUser.StartVoiceRecording();
		m_sampleRate = SteamUser.GetVoiceOptimalSampleRate();
		m_recording = true;
	}

	public static void StopRecording()
	{
		SteamUser.StopVoiceRecording();
		m_recording = false;
	}

	private void LateUpdate()
	{
		if (!m_recording) return;
		if (!TickManager.ShouldTick()) return;

		EVoiceResult result = SteamUser.GetAvailableVoice(out uint cbCompressed);

		if (result == EVoiceResult.k_EVoiceResultOK)
		{
			byte[] voiceBuffer = new byte[cbCompressed];
			SteamUser.GetVoice(true, voiceBuffer, (uint)voiceBuffer.Length, out uint nBytesWritted);

			if (nBytesWritted == 0) return;

			// send voice data
			GCHandle handle = GCHandle.Alloc(voiceBuffer, GCHandleType.Pinned);

			try
			{
				IntPtr pData = handle.AddrOfPinnedObject();

				NetworkManager.SendMessageAll(ESnapshotMessageType.VoiceData, pData, (int)nBytesWritted, ESteamNetworkingSend.k_nSteamNetworkingSend_Unreliable);
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

	public static void ReceiveVoice(SteamNetworkingMessage_t message)
	{
		byte[] buffer = new byte[message.m_cbSize - 1];

		Marshal.Copy(message.m_pData + 1, buffer, 0, buffer.Length);

		EVoiceResult result = SteamUser.DecompressVoice(buffer, (uint)buffer.Length, m_playBackBuffer, (uint)m_playBackBuffer.Length, out uint nBytesWritten, m_sampleRate);

		if (result == EVoiceResult.k_EVoiceResultOK)
		{
			m_bufferFill = nBytesWritten;
		}
	}
}
