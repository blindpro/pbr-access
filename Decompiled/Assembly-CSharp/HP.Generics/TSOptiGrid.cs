using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class TSOptiGrid : MonoBehaviour, IValidateAction<bool>, IInitable
{
	[Serializable]
	public class TargetParam
	{
		public Transform target;

		public int objGridPosRow;

		public int objGridPosColumn;

		public int lastPlayerIndex;

		public TargetParam(Transform trans)
		{
			target = trans;
		}
	}

	[Serializable]
	public class GrpStreamParam
	{
		public bool isEnable;

		public List<GameObject> objList = new List<GameObject>();
	}

	public static TSOptiGrid instance;

	public bool isInitializedWhenSceneStarts = true;

	public bool isInitDone;

	private bool b_InitInProgress;

	private int howManyObjectUpdatedByFrameReminder;

	[Header("Set Terrain Size")]
	public int terrainX = 2100;

	public int terrainZ = 2100;

	[Header("Set Grid Size")]
	public int row = 5;

	public int column = 10;

	[HideInInspector]
	public List<TargetParam> targetsList = new List<TargetParam>();

	[HideInInspector]
	public List<int> activeZoneList = new List<int>();

	[HideInInspector]
	public List<int> lastActiveZoneList = new List<int>();

	public List<GrpStreamParam> steamList = new List<GrpStreamParam>();

	[HideInInspector]
	public bool isUpdateZoneProcessDone = true;

	[Space]
	public int howManyObjectUpdatedByFrame = 20;

	[Header("Actions During Initialization process")]
	public List<UnityEvent> ActionWhenProcessStart = new List<UnityEvent>();

	public List<UnityEvent> ActionWhenProcessEnded = new List<UnityEvent>();

	[HideInInspector]
	public bool isActionProcessDone;

	public bool showGizmo;

	public float gizmoSphereSize = 10f;

	public List<ObjDistanceParams> objsDistanceList = new List<ObjDistanceParams>();

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	private void Start()
	{
		if (isInitializedWhenSceneStarts)
		{
			Init();
		}
	}

	public void Init()
	{
		howManyObjectUpdatedByFrameReminder = howManyObjectUpdatedByFrame;
		howManyObjectUpdatedByFrame = 1000;
		StartCoroutine(InitRoutine());
	}

	public IEnumerator InitRoutine()
	{
		isInitDone = false;
		for (int i = 0; i < ActionWhenProcessStart.Count; i++)
		{
			isActionProcessDone = false;
			ActionWhenProcessStart[i].Invoke();
			yield return new WaitUntil(() => isActionProcessDone);
		}
		TSCharacterTag[] array = UnityEngine.Object.FindObjectsOfType<TSCharacterTag>();
		targetsList.Clear();
		int howManyTarget = 0;
		TSCharacterTag[] array2 = array;
		foreach (TSCharacterTag tSCharacterTag in array2)
		{
			howManyTarget++;
			targetsList.Add(new TargetParam(tSCharacterTag.transform));
		}
		yield return new WaitUntil(() => howManyTarget == targetsList.Count);
		steamList.Clear();
		for (int num2 = 0; num2 <= column; num2++)
		{
			for (int num3 = 0; num3 <= row; num3++)
			{
				steamList.Add(new GrpStreamParam());
			}
		}
		TSStreamGridTag[] array3 = UnityEngine.Object.FindObjectsOfType<TSStreamGridTag>();
		foreach (TSStreamGridTag tSStreamGridTag in array3)
		{
			int num4 = Mathf.FloorToInt(tSStreamGridTag.transform.position.x / (float)(terrainZ / column));
			int num5 = Mathf.FloorToInt(tSStreamGridTag.transform.position.z / (float)(terrainX / row)) * (column + 1) + num4;
			if (num5 >= 0 && num5 < (column + 1) * (row + 1))
			{
				steamList[num5].objList.Add(tSStreamGridTag.gameObject);
				for (int num6 = 0; num6 < tSStreamGridTag.transform.childCount; num6++)
				{
					tSStreamGridTag.transform.GetChild(num6).gameObject.SetActive(value: false);
				}
			}
		}
		yield return new WaitUntil(() => isUpdateZoneProcessDone);
		activeZoneList = ReturnTargetPositionAndActiveZones();
		StartCoroutine(UpdateActiveZoneOnMapRoutine());
		yield return new WaitUntil(() => isUpdateZoneProcessDone);
		TSStreamDistanceTag[] array4 = UnityEngine.Object.FindObjectsOfType<TSStreamDistanceTag>();
		foreach (TSStreamDistanceTag tSStreamDistanceTag in array4)
		{
			tSStreamDistanceTag.ForceReset();
			while (!tSStreamDistanceTag.isAddingObjectToOptiGridSone)
			{
			}
		}
		isInitDone = true;
		StartCoroutine(ChangeObjectStateRoutine());
		yield return new WaitUntil(() => objsDistanceList.Count == 0);
		howManyObjectUpdatedByFrame = howManyObjectUpdatedByFrameReminder;
		for (int i = 0; i < ActionWhenProcessEnded.Count; i++)
		{
			isActionProcessDone = false;
			ActionWhenProcessEnded[i].Invoke();
			yield return new WaitUntil(() => isActionProcessDone);
		}
		yield return null;
	}

	private void Update()
	{
		if (isInitDone)
		{
			UpdateActiveZone();
		}
	}

	private void UpdateActiveZone()
	{
		if (isUpdateZoneProcessDone)
		{
			activeZoneList = ReturnTargetPositionAndActiveZones();
		}
		for (int i = 0; i < targetsList.Count; i++)
		{
			if (isUpdateZoneProcessDone)
			{
				if (targetsList[i].lastPlayerIndex != CurrentPlayerGridIndex(i))
				{
					StartCoroutine(UpdateActiveZoneOnMapRoutine());
				}
				targetsList[i].lastPlayerIndex = CurrentPlayerGridIndex(i);
			}
		}
	}

	private List<int> ReturnTargetPositionAndActiveZones()
	{
		activeZoneList.Clear();
		for (int i = 0; i < targetsList.Count; i++)
		{
			targetsList[i].objGridPosRow = Mathf.FloorToInt(targetsList[i].target.position.x / (float)(terrainZ / column));
			targetsList[i].objGridPosColumn = Mathf.FloorToInt(targetsList[i].target.position.z / (float)(terrainX / row));
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					int num = targetsList[i].objGridPosColumn * (column + 1) + targetsList[i].objGridPosRow - (column + 2) + k + (column + 1) * j;
					if (num >= 0 && num < (column + 1) * (row + 1))
					{
						activeZoneList.Add(num);
					}
				}
			}
		}
		for (int num2 = activeZoneList.Count - 1; num2 >= 0; num2--)
		{
			for (int l = 0; l < activeZoneList.Count; l++)
			{
				if (num2 != l && activeZoneList[num2] == activeZoneList[l])
				{
					activeZoneList.RemoveAt(num2);
					break;
				}
			}
		}
		if (lastActiveZoneList.Count == 0)
		{
			for (int m = 0; m < activeZoneList.Count; m++)
			{
				lastActiveZoneList.Add(activeZoneList[m]);
			}
		}
		return activeZoneList;
	}

	private int CurrentPlayerGridIndex(int iD)
	{
		return targetsList[iD].objGridPosColumn * (column + 1) + targetsList[iD].objGridPosRow;
	}

	private IEnumerator UpdateActiveZoneOnMapRoutine()
	{
		isUpdateZoneProcessDone = false;
		for (int num = lastActiveZoneList.Count - 1; num >= 0; num--)
		{
			bool flag = true;
			for (int i = 0; i < activeZoneList.Count; i++)
			{
				if (lastActiveZoneList[num] == activeZoneList[i])
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				lastActiveZoneList.RemoveAt(num);
			}
		}
		int counter = 0;
		for (int j = 0; j < lastActiveZoneList.Count; j++)
		{
			for (int k = 0; k < steamList[lastActiveZoneList[j]].objList.Count; k++)
			{
				for (int l = 0; l < steamList[lastActiveZoneList[j]].objList[k].transform.childCount; l++)
				{
					steamList[lastActiveZoneList[j]].objList[k].transform.GetChild(l).gameObject.SetActive(value: false);
					counter++;
					if (counter % howManyObjectUpdatedByFrame == howManyObjectUpdatedByFrame - 1)
					{
						yield return new WaitForEndOfFrame();
					}
					while (objsDistanceList.Count != 0)
					{
						yield return null;
					}
				}
			}
			steamList[lastActiveZoneList[j]].isEnable = false;
		}
		lastActiveZoneList.Clear();
		for (int j = 0; j < activeZoneList.Count; j++)
		{
			if (steamList[activeZoneList[j]].isEnable)
			{
				continue;
			}
			for (int k = 0; k < steamList[activeZoneList[j]].objList.Count; k++)
			{
				for (int l = 0; l < steamList[activeZoneList[j]].objList[k].transform.childCount; l++)
				{
					steamList[activeZoneList[j]].objList[k].transform.GetChild(l).gameObject.SetActive(value: true);
					counter++;
					if (counter % howManyObjectUpdatedByFrame == howManyObjectUpdatedByFrame - 1)
					{
						yield return new WaitForEndOfFrame();
					}
					while (objsDistanceList.Count != 0)
					{
						yield return null;
					}
				}
			}
			steamList[activeZoneList[j]].isEnable = true;
		}
		isUpdateZoneProcessDone = true;
		yield return null;
	}

	public void ValidateAction(bool actionState)
	{
		isActionProcessDone = true;
	}

	private void OnDrawGizmos()
	{
		if (!showGizmo)
		{
			return;
		}
		for (int i = 0; i <= column; i++)
		{
			for (int j = 0; j <= row; j++)
			{
				float z = terrainZ / column * i;
				float x = terrainX / row * j;
				Vector3 center = new Vector3(x, 0f, z);
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(center, gizmoSphereSize);
			}
		}
		for (int k = 0; k < targetsList.Count; k++)
		{
			if ((bool)targetsList[k].target)
			{
				Vector3 position = targetsList[k].target.position;
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(position, gizmoSphereSize);
			}
		}
	}

	public bool bInitOptimizationGrid()
	{
		if (!b_InitInProgress)
		{
			b_InitInProgress = true;
			isInitDone = false;
			StartCoroutine(InitRoutine());
		}
		else if (isInitDone)
		{
			b_InitInProgress = false;
		}
		return isInitDone;
	}

	public void AddObjToList(GameObject obj, bool state)
	{
		objsDistanceList.Add(new ObjDistanceParams(obj, state));
	}

	private IEnumerator ChangeObjectStateRoutine()
	{
		yield return new WaitUntil(() => isInitDone);
		int counter = 0;
		while (objsDistanceList.Count > 0)
		{
			objsDistanceList[objsDistanceList.Count - 1].obj.SetActive(objsDistanceList[objsDistanceList.Count - 1].state);
			objsDistanceList.RemoveAt(objsDistanceList.Count - 1);
			counter++;
			if (counter % howManyObjectUpdatedByFrame == howManyObjectUpdatedByFrame - 1)
			{
				yield return new WaitForEndOfFrame();
			}
		}
		while (objsDistanceList.Count == 0)
		{
			yield return null;
		}
		StartCoroutine(ChangeObjectStateRoutine());
		yield return null;
	}

	public bool ForceOptimizationGridUpdate()
	{
		if (!b_InitInProgress)
		{
			b_InitInProgress = true;
			isInitDone = false;
			howManyObjectUpdatedByFrameReminder = howManyObjectUpdatedByFrame;
			howManyObjectUpdatedByFrame = 1000;
			StartCoroutine(InitRoutine());
		}
		else if (isInitDone)
		{
			b_InitInProgress = false;
			howManyObjectUpdatedByFrame = howManyObjectUpdatedByFrameReminder;
		}
		return isInitDone;
	}

	public bool IsInitDone()
	{
		if (!isInitializedWhenSceneStarts)
		{
			return true;
		}
		return isInitDone;
	}
}
