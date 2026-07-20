using System.Collections;
using UnityEngine;

namespace HP.Generics;

public class EyeAdaptationTrigger : MonoBehaviour
{
	public enum EyeAdaptState
	{
		Inside,
		Outside
	}

	public float transitionDuration = 1f;

	public float startWeight;

	public EyeAdaptState player = EyeAdaptState.Outside;

	private void Start()
	{
		InitLocalPostFxWeight();
	}

	public void InitLocalPostFxWeight()
	{
	}

	public void EyeAdaptationTransition(bool insideOnly = false)
	{
		StopAllCoroutines();
		StartCoroutine(EyeAdaptationTransitionRoutine(insideOnly));
	}

	public void SpawnForceInsideEyeAdaptationTransition(bool insideOnly = false)
	{
		player = EyeAdaptState.Inside;
		StopAllCoroutines();
		StartCoroutine(EyeAdaptationTransitionRoutine(insideOnly));
	}

	private IEnumerator EyeAdaptationTransitionRoutine(bool insideOnly = false)
	{
		float t = 0f;
		float duration = transitionDuration;
		if (insideOnly)
		{
			player = EyeAdaptState.Inside;
		}
		else if (player == EyeAdaptState.Outside)
		{
			player = EyeAdaptState.Inside;
		}
		else
		{
			player = EyeAdaptState.Outside;
		}
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			yield return null;
		}
		yield return null;
	}
}
