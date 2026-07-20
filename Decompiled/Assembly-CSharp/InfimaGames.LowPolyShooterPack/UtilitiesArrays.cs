using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public static class UtilitiesArrays
{
	public static bool IsValidIndex<T>(this T[] array, int index)
	{
		if (array.Length > index)
		{
			return index >= 0;
		}
		return false;
	}

	public static bool IsValid<T>(this T[] array)
	{
		if (!array.Equals(null))
		{
			return array.Length != 0;
		}
		return false;
	}

	public static T GetRandom<T>(this T[] array)
	{
		return array[Random.Range(0, array.Length)];
	}
}
