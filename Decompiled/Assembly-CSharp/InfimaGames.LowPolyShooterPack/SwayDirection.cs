using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[Serializable]
public struct SwayDirection
{
	[Range(0f, 10f)]
	[Tooltip("Multiplier applied to the location curves.")]
	[SerializeField]
	public float locationMultiplier;

	[Tooltip("Animated location curves.")]
	[SerializeField]
	public AnimationCurve[] locationCurves;

	[Range(0f, 10f)]
	[Tooltip("Multiplier applied to the rotation curves.")]
	[SerializeField]
	public float rotationMultiplier;

	[Tooltip("Animated rotation curves.")]
	[SerializeField]
	public AnimationCurve[] rotationCurves;
}
