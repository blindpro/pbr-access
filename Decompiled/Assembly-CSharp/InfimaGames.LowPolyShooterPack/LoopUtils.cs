using System.Collections.Generic;
using System.Linq;

namespace InfimaGames.LowPolyShooterPack;

public static class LoopUtils
{
	public static bool IsFilled<T>(this IEnumerable<T> enumerable)
	{
		if (enumerable == null)
		{
			return false;
		}
		if (enumerable.Count() == 0)
		{
			return false;
		}
		return true;
	}
}
