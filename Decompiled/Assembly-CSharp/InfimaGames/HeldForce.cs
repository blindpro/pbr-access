using System;
using UnityEngine;

namespace InfimaGames;

[Serializable]
public struct HeldForce
{
	[Tooltip("Force applied over frames.")]
	[SerializeField]
	private Vector3 force;

	[Tooltip("Frames to apply the force over.")]
	[SerializeField]
	private int frames;

	public Vector3 Force => force;

	public int Frames => frames;
}
