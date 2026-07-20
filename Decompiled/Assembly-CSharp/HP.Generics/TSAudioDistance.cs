using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class TSAudioDistance : MonoBehaviour
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

	private bool isLightEnable;

	[Header("Distance in meters to:")]
	public float enableAudio = 40f;

	public float reachMaxVolume = 20f;

	public float speedToReachMaxVolume = 2f;

	private float volumeRef;

	[HideInInspector]
	public List<TargetParam> targetsList = new List<TargetParam>();

	[Header("Audio")]
	public AudioSource aSource;

	private void Start()
	{
	}

	public IEnumerator InitRoutine()
	{
		volumeRef = aSource.volume;
		TSCharacterTag[] array = UnityEngine.Object.FindObjectsOfType<TSCharacterTag>();
		foreach (TSCharacterTag tSCharacterTag in array)
		{
			targetsList.Add(new TargetParam(tSCharacterTag.transform));
		}
		yield return null;
	}

	private void Update()
	{
		if ((bool)aSource && targetsList.Count > 0)
		{
			CalculateDistanceFromAudioSourceToTarget();
			EnableOrDisableLightDependingDistance();
			UpdateAudioVolume();
		}
	}

	private void EnableOrDisableLightDependingDistance()
	{
		if (currentDistanceToTarget <= enableAudio && !isLightEnable)
		{
			isLightEnable = true;
			if ((bool)aSource)
			{
				aSource.gameObject.SetActive(value: true);
			}
		}
		if (currentDistanceToTarget > enableAudio && isLightEnable)
		{
			isLightEnable = false;
			if ((bool)aSource)
			{
				aSource.gameObject.SetActive(value: false);
				ResetAudioVolume();
			}
		}
	}

	private void ResetAudioVolume()
	{
		aSource.volume = 0f;
	}

	private void UpdateAudioVolume()
	{
		if (isLightEnable)
		{
			float value = (currentDistanceToTarget - reachMaxVolume) / (enableAudio - reachMaxVolume);
			value = Mathf.Clamp01(value);
			aSource.volume = Mathf.MoveTowards(aSource.volume, volumeRef * (1f - value), Time.deltaTime * speedToReachMaxVolume);
		}
	}

	private void CalculateDistanceFromAudioSourceToTarget()
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
		bool flag2 = ((targetsList[currentID].targetDistance < enableAudio) ? true : false);
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
