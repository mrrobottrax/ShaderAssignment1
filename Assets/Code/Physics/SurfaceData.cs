using System;
using UnityEngine;

public enum SurfaceType
{
	Stone,
	Sand,
	Wood,
	Metal,
	Gravel
}

[Serializable]
public struct SurfaceSounds
{
	public AudioClip[] Step;
	public AudioClip[] Jump;
	public AudioClip[] Drop;
	public AudioClip[] Impact;
}

public class SurfaceData : MonoBehaviour
{
	public SurfaceType SurfaceMaterial;
}