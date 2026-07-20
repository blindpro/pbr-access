using System;
using System.Collections.Generic;
using System.Linq;

namespace InfimaGames.LowPolyShooterPack;

public static class RLoops
{
	public static void ForrEach<T>(this IEnumerable<T> enumerable, Action<T> action)
	{
		if (enumerable.IsFilled())
		{
			for (int num = enumerable.Count() - 1; num >= 0; num--)
			{
				T obj = enumerable.ElementAt(num);
				action(obj);
			}
		}
	}

	public static void ForrEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
	{
		if (enumerable.IsFilled())
		{
			for (int num = enumerable.Count() - 1; num >= 0; num--)
			{
				T arg = enumerable.ElementAt(num);
				action(arg, num);
			}
		}
	}

	public static void ForrEach<T>(this IEnumerable<T> enumerable, Action<T, int, IEnumerable<T>> action)
	{
		if (enumerable.IsFilled())
		{
			for (int num = enumerable.Count() - 1; num >= 0; num--)
			{
				T arg = enumerable.ElementAt(num);
				action(arg, num, enumerable);
			}
		}
	}

	public static void ForrEach<T1, T2>(this IEnumerable<KeyValuePair<T1, T2>> enumerable, Action<T1, T2> action)
	{
		if (enumerable != null)
		{
			for (int num = enumerable.Count() - 1; num >= 0; num--)
			{
				KeyValuePair<T1, T2> keyValuePair = enumerable.ElementAt(num);
				action(keyValuePair.Key, keyValuePair.Value);
			}
		}
	}
}
