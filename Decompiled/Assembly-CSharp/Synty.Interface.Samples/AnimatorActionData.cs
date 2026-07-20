using System;
using UnityEngine;

namespace Synty.Interface.Samples;

[Serializable]
public class AnimatorActionData
{
	public enum AnimatorActionType
	{
		Trigger,
		Bool,
		Float,
		Int
	}

	[Header("References")]
	public Animator animator;

	public AnimatorActionType type;

	[Header("Parameters")]
	public string parameterName;

	public bool boolToggle;

	public bool boolValue;

	public float floatValue;

	public int intValue;

	public void Execute()
	{
		if (!animator)
		{
			return;
		}
		switch (type)
		{
		case AnimatorActionType.Trigger:
			animator.SetTrigger(parameterName);
			break;
		case AnimatorActionType.Bool:
			if (boolToggle)
			{
				bool flag = animator.GetBool(parameterName);
				animator.SetBool(parameterName, !flag);
			}
			else
			{
				animator.SetBool(parameterName, boolValue);
			}
			break;
		case AnimatorActionType.Float:
			animator.SetFloat(parameterName, floatValue);
			break;
		case AnimatorActionType.Int:
			animator.SetInteger(parameterName, intValue);
			break;
		}
	}
}
