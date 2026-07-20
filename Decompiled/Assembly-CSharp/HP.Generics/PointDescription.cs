using System;
using UnityEngine;

namespace HP.Generics;

[Serializable]
public class PointDescription
{
	public Vector3 points;

	public Quaternion rotation = Quaternion.identity;

	public PointDescription(Vector3 _Point, Quaternion _Rotation)
	{
		points = _Point;
		rotation = _Rotation;
	}
}
