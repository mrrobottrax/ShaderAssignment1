using System;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceProperties : ScriptableObject
{
	#region Singleton
	static SurfaceProperties s_instance;
	public static SurfaceProperties Instance
	{
		get
		{
			if (s_instance == null)
			{
				Load();
			}
			return s_instance;
		}
	}

	static void Load()
	{
		SurfaceProperties[] data = Resources.LoadAll<SurfaceProperties>("");

		if (data == null || data.Length == 0)
		{
			Debug.LogError("No SurfaceProperties found");
			return;
		}

		s_instance = data[0];

		// Copy inspector data into dictionary
		s_instance.m_SurfaceSoundsDict = new();
		foreach (var entry in s_instance.m_SurfaceSounds)
		{
			s_instance.m_SurfaceSoundsDict.Add(entry.SurfaceType, entry.SurfaceSounds);
		}
	}
	#endregion

	[Serializable]
	struct InspectorSurfaceData
	{
		public SurfaceType SurfaceType;
		public SurfaceSounds SurfaceSounds;
	}
	[SerializeField] InspectorSurfaceData[] m_SurfaceSounds;

	Dictionary<SurfaceType, SurfaceSounds> m_SurfaceSoundsDict;

	public static AudioClip GetStepSound(SurfaceType surface)
	{
		SurfaceSounds sounds = Instance.m_SurfaceSoundsDict[surface];
		return sounds.Step[UnityEngine.Random.Range(0, sounds.Step.Length)];
	}

	public static int GetStepSoundCount(SurfaceType surface)
	{
		SurfaceSounds sounds = Instance.m_SurfaceSoundsDict[surface];
		return sounds.Step.Length;
	}
}