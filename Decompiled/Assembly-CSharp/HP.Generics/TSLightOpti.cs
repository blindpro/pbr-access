using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class TSLightOpti : MonoBehaviour
{
	[Serializable]
	public class TargetParam
	{
		public Transform target;

		public float targetDistance;

		public TargetParam(Transform trans)
		{
			target = trans;
		}
	}

	private float currentDistanceToTarget = -1f;

	private Light lightC;

	private bool isLightEnable;

	[Header("Distance in meters to:")]
	public float enableLight = 20f;

	public float reachMaxIntensity = 10f;

	public float SpeedLightIntensity = 2f;

	private bool isLightShadowEnable;

	public float enableShadow = 10f;

	public float reachMaxStrength = 5f;

	public LightShadows lightShadows = LightShadows.Hard;

	private float intensityRef;

	private float strengthRef;

	public bool bypassShadow;

	[HideInInspector]
	public List<TargetParam> targetsList = new List<TargetParam>();

	private void Start()
	{
	}

	public IEnumerator InitRoutine()
	{
		lightC = GetComponent<Light>();
		intensityRef = lightC.intensity;
		strengthRef = lightC.shadowStrength;
		isLightEnable = lightC.enabled;
		if (bypassShadow)
		{
			lightC.shadows = LightShadows.None;
		}
		if (isLightEnable)
		{
			ResetLightIntensity();
		}
		TSCharacterTag[] array = UnityEngine.Object.FindObjectsOfType<TSCharacterTag>();
		foreach (TSCharacterTag tSCharacterTag in array)
		{
			targetsList.Add(new TargetParam(tSCharacterTag.transform));
		}
		yield return null;
	}

	private void Update()
	{
		if ((bool)lightC && targetsList.Count > 0)
		{
			CalculateDistanceFromLightToTarget();
			EnableOrDisableLightDependingDistance();
			EnableOrDisableLightShadowDependingDistance();
			UpdateLightIntensity();
			UpdateLightShadow();
		}
	}

	private void EnableOrDisableLightDependingDistance()
	{
		if (currentDistanceToTarget <= enableLight && !isLightEnable)
		{
			isLightEnable = true;
			if ((bool)lightC)
			{
				lightC.enabled = true;
			}
		}
		if (currentDistanceToTarget > enableLight && isLightEnable)
		{
			isLightEnable = false;
			if ((bool)lightC)
			{
				lightC.enabled = false;
				ResetLightIntensity();
			}
		}
	}

	private void ResetLightIntensity()
	{
		lightC.intensity = 0f;
	}

	private void UpdateLightIntensity()
	{
		if (isLightEnable)
		{
			float value = (currentDistanceToTarget - reachMaxIntensity) / (enableLight - reachMaxIntensity);
			value = Mathf.Clamp01(value);
			lightC.intensity = Mathf.MoveTowards(lightC.intensity, intensityRef * (1f - value), Time.deltaTime * SpeedLightIntensity);
		}
	}

	private void EnableOrDisableLightShadowDependingDistance()
	{
		if (!isLightEnable || bypassShadow)
		{
			return;
		}
		if (currentDistanceToTarget <= enableShadow && !isLightShadowEnable)
		{
			isLightShadowEnable = true;
			if ((bool)lightC)
			{
				lightC.shadows = lightShadows;
			}
		}
		if (currentDistanceToTarget > enableShadow && isLightShadowEnable)
		{
			isLightShadowEnable = false;
			if ((bool)lightC)
			{
				lightC.shadows = LightShadows.None;
				ResetLightShadow();
			}
		}
	}

	private void ResetLightShadow()
	{
		lightC.shadowStrength = 0f;
	}

	private void UpdateLightShadow()
	{
		if (isLightEnable && isLightShadowEnable)
		{
			float value = (currentDistanceToTarget - reachMaxStrength) / (enableShadow - reachMaxStrength);
			value = Mathf.Clamp01(value);
			lightC.shadowStrength = Mathf.MoveTowards(lightC.shadowStrength, strengthRef * (1f - value), Time.deltaTime * 2f);
		}
	}

	private void CalculateDistanceFromLightToTarget()
	{
		for (int i = 0; i < targetsList.Count; i++)
		{
			if ((bool)targetsList[i].target)
			{
				targetsList[i].targetDistance = Vector3.Distance(targetsList[i].target.position, base.transform.position);
			}
		}
		for (int j = 0; j < targetsList.Count; j++)
		{
			if (j > 0 && (bool)targetsList[j].target && (bool)targetsList[j - 1].target)
			{
				if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(j, j - 1))
				{
					currentDistanceToTarget = targetsList[j].targetDistance;
				}
				else if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(j - 1))
				{
					currentDistanceToTarget = targetsList[j - 1].targetDistance;
				}
				else
				{
					currentDistanceToTarget = 100000f;
				}
			}
			if (targetsList.Count == 1)
			{
				if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(j))
				{
					currentDistanceToTarget = targetsList[j].targetDistance;
				}
				else
				{
					currentDistanceToTarget = 1000000f;
				}
			}
		}
	}

	private bool IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(int currentID, int lastID = -1)
	{
		bool flag = (flag = true);
		if (lastID != -1)
		{
			flag = ((targetsList[currentID].targetDistance < targetsList[lastID].targetDistance) ? true : false);
		}
		bool flag2 = ((targetsList[currentID].targetDistance < enableLight) ? true : false);
		if (flag && flag2)
		{
			return true;
		}
		return false;
	}

	private bool IsLampInFrontOfTheTarget(Transform target)
	{
		if (Vector3.Dot(base.transform.position - target.position, target.forward) >= 0f)
		{
			return true;
		}
		return false;
	}
}
