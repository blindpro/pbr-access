using System;
using System.Collections.Generic;
using System.Linq;

namespace InfimaGames.LowPolyShooterPack;

public static class Loops
{
	public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
	{
		if (enumerable.IsFilled())
		{
			for (int i = 0; i < enumerable.Count(); i++)
			{
				T obj = enumerable.ElementAt(i);
				action(obj);
			}
		}
	}

	public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
	{
		if (enumerable.IsFilled())
		{
			for (int i = 0; i < enumerable.Count(); i++)
			{
				T arg = enumerable.ElementAt(i);
				action(arg, i);
			}
		}
	}

	public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int, IEnumerable<T>> action)
	{
		if (enumerable.IsFilled())
		{
			for (int i = 0; i < enumerable.Count(); i++)
			{
				T arg = enumerable.ElementAt(i);
				action(arg, i, enumerable);
			}
		}
	}

	public static void ForEach<T1, T2>(this IEnumerable<KeyValuePair<T1, T2>> enumerable, Action<T1, T2> action)
	{
		if (enumerable != null)
		{
			for (int i = 0; i < enumerable.Count(); i++)
			{
				KeyValuePair<T1, T2> keyValuePair = enumerable.ElementAt(i);
				action(keyValuePair.Key, keyValuePair.Value);
			}
		}
	}
}
