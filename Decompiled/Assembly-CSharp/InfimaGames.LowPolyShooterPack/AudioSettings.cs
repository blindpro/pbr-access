using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[Serializable]
public struct AudioSettings
{
	[Header("Settings")]
	[Tooltip("If true, any AudioSource created will be removed after it has finished playing its clip.")]
	[SerializeField]
	private bool automaticCleanup;

	[Tooltip("Volume.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float volume;

	[Tooltip("Spatial Blend.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float spatialBlend;

	public bool AutomaticCleanup => automaticCleanup;

	public float Volume => volume;

	public float SpatialBlend => spatialBlend;

	public AudioSettings(float volume = 1f, float spatialBlend = 0f, bool automaticCleanup = true)
	{
		this.volume = volume;
		this.spatialBlend = spatialBlend;
		this.automaticCleanup = automaticCleanup;
	}
}
