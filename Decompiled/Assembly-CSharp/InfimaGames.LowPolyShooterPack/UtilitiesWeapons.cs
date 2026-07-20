using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public static class UtilitiesWeapons
{
	public static T SelectAndSetActive<T>(this T[] array, int index) where T : MonoBehaviour
	{
		if (!array.IsValid())
		{
			return null;
		}
		array.ForEach(delegate(T obj)
		{
			obj.gameObject.SetActive(value: false);
		});
		if (!array.IsValidIndex(index))
		{
			return null;
		}
		T val = array[index];
		if (val != null)
		{
			val.gameObject.SetActive(value: true);
		}
		return val;
	}
}
