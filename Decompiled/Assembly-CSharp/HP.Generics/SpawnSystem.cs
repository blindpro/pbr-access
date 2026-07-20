using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class SpawnSystem : MonoBehaviour, IValidateAction<bool>
{
	[Serializable]
	public class SpawnPosParam
	{
		public string spawnName = "Name";

		public Transform spawnPos;

		public KeyCode key;

		public bool alreadyVisited;
	}

	public bool isInitializedWhenSceneStarts = true;

	public bool checkInput = true;

	public Transform Chara;

	[HideInInspector]
	public bool isInitDone;

	public KeyCode nextDestinationKey = KeyCode.N;

	public List<SpawnPosParam> spawnList = new List<SpawnPosParam>();

	[HideInInspector]
	public bool isNewSpawnPosInProgress;

	public List<UnityEvent> ActionsWhenProcessStarts = new List<UnityEvent>();

	public List<UnityEvent> ActionsWhenProcessEnded = new List<UnityEvent>();

	[HideInInspector]
	public bool isActionProcessDone;

	[HideInInspector]
	public int currentSpawnID;

	private void Start()
	{
		if (isInitializedWhenSceneStarts)
		{
			StartCoroutine(InitSpawnSystemRoutine());
		}
	}

	public void InitSpawnSystem()
	{
		StartCoroutine(InitSpawnSystemRoutine());
	}

	public IEnumerator InitSpawnSystemRoutine()
	{
		if (Chara == null)
		{
			TSCharacterTag target = UnityEngine.Object.FindObjectOfType<TSCharacterTag>();
			yield return new WaitUntil(() => target);
			Chara = target.transform;
		}
		StartCoroutine(SpawnRoutine(0));
		yield return null;
	}

	private void Update()
	{
		if (checkInput)
		{
			CheckInput();
		}
	}

	public void CheckInput()
	{
		if (isNewSpawnPosInProgress || !isInitDone)
		{
			return;
		}
		for (int i = 0; i < spawnList.Count; i++)
		{
			if (Input.GetKeyDown(spawnList[i].key) && i != currentSpawnID && spawnList[i].key != KeyCode.None)
			{
				GoToNewSpawnPosition(i);
				break;
			}
		}
		if (Input.GetKeyDown(nextDestinationKey))
		{
			GoToNextDestination();
		}
	}

	public void GoToNewSpawnPosition(int spawnID)
	{
		if (!isNewSpawnPosInProgress && isInitDone)
		{
			StartCoroutine(SpawnRoutine(spawnID));
		}
	}

	private IEnumerator SpawnRoutine(int spawnID)
	{
		isNewSpawnPosInProgress = true;
		currentSpawnID = spawnID;
		spawnList[currentSpawnID].alreadyVisited = true;
		TSOptiGrid.instance.isInitDone = false;
		for (int i = 0; i < ActionsWhenProcessStarts.Count; i++)
		{
			isActionProcessDone = false;
			ActionsWhenProcessStarts[i].Invoke();
			yield return new WaitUntil(() => isActionProcessDone);
		}
		yield return new WaitUntil(() => Chara.transform.position == spawnList[currentSpawnID].spawnPos.position);
		yield return new WaitUntil(() => Chara.transform.rotation == spawnList[currentSpawnID].spawnPos.rotation);
		yield return new WaitUntil(() => TSOptiGrid.instance.ForceOptimizationGridUpdate());
		yield return new WaitUntil(() => TSOptiGrid.instance.objsDistanceList.Count == 0);
		for (int i = 0; i < ActionsWhenProcessEnded.Count; i++)
		{
			isActionProcessDone = false;
			ActionsWhenProcessEnded[i].Invoke();
			yield return new WaitUntil(() => isActionProcessDone);
		}
		isNewSpawnPosInProgress = false;
		isInitDone = true;
		yield return null;
	}

	public void ValidateAction(bool actionState)
	{
		isActionProcessDone = true;
	}

	public void GoToNextDestination()
	{
		bool flag = false;
		while (!flag)
		{
			currentSpawnID++;
			currentSpawnID %= spawnList.Count;
			if (currentSpawnID == 0)
			{
				for (int i = 0; i < spawnList.Count; i++)
				{
					spawnList[i].alreadyVisited = false;
				}
			}
			if (!spawnList[currentSpawnID].alreadyVisited)
			{
				flag = true;
			}
		}
		GoToNewSpawnPosition(currentSpawnID);
	}
}
