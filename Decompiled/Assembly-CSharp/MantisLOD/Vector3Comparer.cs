using System.Collections.Generic;
using UnityEngine;

namespace MantisLOD;

internal class Vector3Comparer : IEqualityComparer<Vector3>
{
	public bool Equals(Vector3 vec1, Vector3 vec2)
	{
		if (vec1.x + 1E-07f >= vec2.x && vec1.x <= vec2.x + 1E-07f && vec1.y + 1E-07f >= vec2.y && vec1.y <= vec2.y + 1E-07f && vec1.z + 1E-07f >= vec2.z)
		{
			return vec1.z <= vec2.z + 1E-07f;
		}
		return false;
	}

	public int GetHashCode(Vector3 vec)
	{
		return vec.x.GetHashCode() ^ vec.y.GetHashCode() ^ vec.z.GetHashCode();
	}
}
