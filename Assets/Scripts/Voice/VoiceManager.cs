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
		StartRecording();
	}

	public static void StartRecording()
	{
		SteamUser.StartVoiceRecording();
	}

	public static void StopRecording()
	{
		SteamUser.StopVoiceRecording();
	}

	private void Update()
	{
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
}
