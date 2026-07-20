using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class RoadCreation
{
	public static Vector3 GetPointPosition(int id0, int id1, int id2, int id3, float t, List<PointDescription> points)
	{
		Vector3 vector = SListPoint(id0, points);
		Vector3 vector2 = SListPoint(id1, points);
		Vector3 vector3 = SListPoint(id2, points);
		Vector3 vector4 = SListPoint(id3, points);
		return (1f - t) * (1f - t) * (1f - t) * vector + 3f * (1f - t) * (1f - t) * t * vector2 + 3f * (1f - t) * t * t * vector3 + t * t * t * vector4;
	}

	public static Vector3 GetVelocity(int id0, int id1, int id2, int id3, float t, List<PointDescription> points)
	{
		Vector3 vector = SListPoint(id0, points);
		Vector3 vector2 = SListPoint(id1, points);
		Vector3 vector3 = SListPoint(id2, points);
		Vector3 vector4 = SListPoint(id3, points);
		return 3f * (1f - t) * (1f - t) * (vector2 - vector) + 6f * (1f - t) * t * (vector3 - vector2) + 3f * t * t * (vector4 - vector3);
	}

	public static Vector3 SListPoint(int index, List<PointDescription> points)
	{
		int index2 = (index + points.Count) % points.Count;
		return points[index2].points;
	}
}
