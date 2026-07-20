using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public static class AnimationCurveUtilities
{
	public static Vector3 EvaluateCurves(this AnimationCurve[] animationCurves, float time)
	{
		if (animationCurves == null || animationCurves.Length != 3)
		{
			return default(Vector3);
		}
		return new Vector3
		{
			x = animationCurves[0].Evaluate(time),
			y = animationCurves[1].Evaluate(time),
			z = animationCurves[2].Evaluate(time)
		};
	}
}
