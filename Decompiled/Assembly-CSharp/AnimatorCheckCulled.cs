using UnityEngine;

public class AnimatorCheckCulled : MonoBehaviour
{
	public Animator animator;

	public bool isCulled;

	private AnimatorStateInfo currentState;

	private float lastNormalizedTime;

	private float unchangedTime;

	private void Start()
	{
		if (!animator)
		{
			animator = GetComponent<Animator>();
		}
		currentState = animator.GetCurrentAnimatorStateInfo(0);
		lastNormalizedTime = currentState.normalizedTime;
	}

	private void Update()
	{
		if ((bool)animator && animator.isActiveAndEnabled)
		{
			currentState = animator.GetCurrentAnimatorStateInfo(0);
			float normalizedTime = currentState.normalizedTime;
			if (Mathf.Approximately(normalizedTime, lastNormalizedTime))
			{
				unchangedTime += Time.deltaTime;
			}
			else
			{
				unchangedTime = 0f;
				lastNormalizedTime = normalizedTime;
			}
			isCulled = unchangedTime > 0.2f;
		}
	}
}
