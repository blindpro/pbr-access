using System;
using UnityEngine;

namespace InfimaGames;

[Serializable]
public struct SpringSettings
{
	[Tooltip("Determines how springy the spring is, the lower this value, the more bounce you will see.")]
	[Range(0f, 100f)]
	public float damping;

	[Tooltip("Determines how stiff the interpolation looks. The lower the value, the stiffer it becomes.")]
	[Range(0f, 200f)]
	public float stiffness;

	[Tooltip("Determines how heavy the interpolation looks.")]
	[Range(0f, 100f)]
	public float mass;

	[Tooltip("Determines the speed of the interpolation. The higher the value, the faster the speed.")]
	[Range(1f, 10f)]
	public float speed;

	public static SpringSettings Default()
	{
		return new SpringSettings
		{
			damping = 15f,
			mass = 1f,
			stiffness = 150f,
			speed = 1f
		};
	}
}
