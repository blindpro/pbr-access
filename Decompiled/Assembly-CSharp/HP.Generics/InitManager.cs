using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class InitManager : MonoBehaviour
{
	public bool autoStart = true;

	public List<UnityEvent> ActionWhenSceneStarts = new List<UnityEvent>();

	[Space]
	public List<GameObject> waitUntilList = new List<GameObject>();

	[Space]
	public List<UnityEvent> ActionWhenInitProcessDone = new List<UnityEvent>();

	private List<IInitable> interfaceList = new List<IInitable>();

	private void Start()
	{
		if (autoStart)
		{
			SceneStarts();
		}
	}

	public void SceneStarts()
	{
		for (int i = 0; i < waitUntilList.Count; i++)
		{
			if ((bool)waitUntilList[i])
			{
				interfaceList.Add(waitUntilList[i].GetComponent<IInitable>());
			}
		}
		for (int j = 0; j < ActionWhenSceneStarts.Count; j++)
		{
			ActionWhenSceneStarts[j].Invoke();
		}
		StartCoroutine(WaitUntilSceneIsInitialized());
	}

	private IEnumerator WaitUntilSceneIsInitialized()
	{
		bool allConditionsTrue = false;
		while (!allConditionsTrue)
		{
			allConditionsTrue = true;
			for (int i = 0; i < interfaceList.Count; i++)
			{
				if (!interfaceList[i].IsInitDone())
				{
					allConditionsTrue = false;
					break;
				}
			}
			yield return null;
		}
		StartCoroutine(InitProcessDoneRoutine());
		yield return null;
	}

	private IEnumerator InitProcessDoneRoutine()
	{
		for (int i = 0; i < ActionWhenInitProcessDone.Count; i++)
		{
			ActionWhenInitProcessDone[i].Invoke();
		}
		yield return null;
	}
}
