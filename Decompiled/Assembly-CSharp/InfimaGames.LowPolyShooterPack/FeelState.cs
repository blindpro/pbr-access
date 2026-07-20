using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[Serializable]
public struct FeelState
{
	[Tooltip("Offset.")]
	[SerializeField]
	public FeelStateOffset offset;

	[Tooltip("Settings relating to sway.")]
	[SerializeField]
	public SwayData swayData;

	[Tooltip("Animation curves played when the character jumps.")]
	[SerializeField]
	public ACurves jumpingCurves;

	[Tooltip("Animation curves played when the character falls.")]
	[SerializeField]
	public ACurves fallingCurves;

	[Tooltip("Animation curves played when the character lands.")]
	[SerializeField]
	public ACurves landingCurves;

	public FeelStateOffset Offset => offset;

	public SwayData SwayData => swayData;

	public ACurves JumpingCurves => jumpingCurves;

	public ACurves FallingCurves => fallingCurves;

	public ACurves LandingCurves => landingCurves;
}
