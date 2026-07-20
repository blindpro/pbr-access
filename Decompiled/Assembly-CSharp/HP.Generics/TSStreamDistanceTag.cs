using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class TSStreamDistanceTag : MonoBehaviour
{
	[HideInInspector]
	public bool isInitDone;

	public float DistanceMin = 100f;

	public float refDistancePerSecond = 20f;

	[HideInInspector]
	public bool isAlreadyEnable;

	public List<Vector3> positionToCheckList = new List<Vector3>();

	public UnityEvent ActionOnDisable;

	public UnityEvent ActionOnEnable;

	public bool isAddingObjectToOptiGridSone;

	private void Start()
	{
		if ((bool)TSOptiGrid.instance)
		{
			StartCoroutine(InitRoutine(newObjState: false, onEnableInit: true));
		}
	}

	private void OnEnable()
	{
		if ((bool)TSOptiGrid.instance)
		{
			StartCoroutine(InitRoutine(newObjState: false, onEnableInit: true));
		}
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private float CalculateDistanceToTarget()
	{
		float num = -1f;
		float num2 = -1f;
		for (int i = 0; i < TSOptiGrid.instance.targetsList.Count; i++)
		{
			if (positionToCheckList.Count > 0)
			{
				for (int j = 0; j < positionToCheckList.Count; j++)
				{
					Vector3 position = TSOptiGrid.instance.targetsList[i].target.position;
					float num3 = Vector3.Distance(positionToCheckList[j] + base.transform.position, position);
					if (num2 == -1f || num2 > num3)
					{
						num2 = num3;
					}
				}
				num = ((num2 == -1f) ? (-1f) : num2);
			}
			else
			{
				Vector3 position2 = TSOptiGrid.instance.targetsList[i].target.position;
				float num4 = Vector3.Distance(base.transform.position, position2);
				if (num == -1f || num > num4)
				{
					num = num4;
				}
			}
		}
		return num;
	}

	public IEnumerator InitRoutine(bool newObjState, bool onEnableInit = false, bool waitUntilOptiGridInitDone = true)
	{
		isInitDone = false;
		if (waitUntilOptiGridInitDone)
		{
			yield return new WaitUntil(() => TSOptiGrid.instance.isInitDone);
		}
		isAddingObjectToOptiGridSone = false;
		float num = CalculateDistanceToTarget();
		if (num < DistanceMin)
		{
			newObjState = true;
		}
		if (isAlreadyEnable != newObjState || onEnableInit)
		{
			if (newObjState)
			{
				ActionOnEnable.Invoke();
			}
			else
			{
				ActionOnDisable.Invoke();
			}
			for (int num2 = 0; num2 < base.transform.childCount; num2++)
			{
				TSOptiGrid.instance.AddObjToList(base.transform.GetChild(num2).gameObject, newObjState);
			}
		}
		isAddingObjectToOptiGridSone = true;
		isAlreadyEnable = newObjState;
		float waitDurationUntilNextCheck = num - DistanceMin;
		waitDurationUntilNextCheck /= refDistancePerSecond;
		if (waitDurationUntilNextCheck < 0f)
		{
			waitDurationUntilNextCheck = 5f;
		}
		float timer = 0f;
		while (timer < waitDurationUntilNextCheck)
		{
			timer += Time.deltaTime;
			yield return null;
		}
		isInitDone = true;
		StartCoroutine(InitRoutine(newObjState: false));
	}

	public void ForceReset()
	{
		StopAllCoroutines();
		StartCoroutine(InitRoutine(newObjState: false, onEnableInit: true, waitUntilOptiGridInitDone: false));
	}
}
