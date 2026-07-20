using System.Collections;
using UnityEngine;

namespace Synty.Interface.Samples;

public class SampleLoopAnimator : MonoBehaviour
{
	[Header("References")]
	public Animator animator;

	[Header("Parameters")]
	public string parameterName = "Health";

	public float inSpeed = 5f;

	public float outSpeed = 5f;

	public float startDelay;

	public float inDelay = 2.5f;

	public float outDelay = 2.5f;

	private void Awake()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
	}

	private void Reset()
	{
		animator = GetComponent<Animator>();
	}

	private void OnEnable()
	{
		if (!(animator == null))
		{
			StartCoroutine(C_TweenBackAndForth());
		}
	}

	private IEnumerator C_TweenBackAndForth()
	{
		yield return new WaitForSeconds(startDelay);
		while (true)
		{
			yield return C_TweenFloat(0f, 1f, inSpeed);
			yield return new WaitForSeconds(outDelay);
			yield return C_TweenFloat(1f, 0f, outSpeed);
			yield return new WaitForSeconds(inDelay);
		}
	}

	private IEnumerator C_TweenFloat(float startValue, float endValue, float duration)
	{
		float time = 0f;
		while (time < 1f)
		{
			time += Time.deltaTime / duration;
			float value = Mathf.Lerp(startValue, endValue, time);
			animator.SetFloat(parameterName, value);
			yield return null;
		}
	}
}
