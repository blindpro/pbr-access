using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HP.Generics;

public class LoadMultipleScenes : MonoBehaviour
{
	public bool initAuto = true;

	public List<string> scenesList = new List<string>();

	public string loadScene = "";

	public Camera camToDelete;

	public GameObject loadingObj;

	[HideInInspector]
	public bool isLoading;

	[Space]
	[Space]
	public UnityEvent doSomethingAtTheEndOfProcessEvent;

	public Text txtLoading;

	public Image imLoading;

	private void Start()
	{
		if (initAuto)
		{
			LoadAsyncScenes();
		}
		LightProbes.tetrahedralizationCompleted += OnTetrahedralization;
	}

	private void OnTetrahedralization()
	{
	}

	public void LoadAsyncScenes()
	{
		StartCoroutine(LoadAsyncScenesRoutine());
	}

	private IEnumerator LoadAsyncScenesRoutine()
	{
		isLoading = true;
		if ((bool)loadingObj)
		{
			loadingObj.SetActive(value: true);
			yield return new WaitUntil(() => loadingObj.activeSelf);
		}
		for (int i = 0; i < scenesList.Count; i++)
		{
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenesList[i], LoadSceneMode.Additive);
			while (!asyncLoad.isDone)
			{
				LoadingBar(i, asyncLoad);
				yield return null;
			}
			DetectCamera();
			yield return new WaitForEndOfFrame();
		}
		if ((bool)loadingObj)
		{
			loadingObj.SetActive(value: false);
			yield return new WaitUntil(() => !loadingObj.activeSelf);
		}
		isLoading = false;
		SceneManager.SetActiveScene(SceneManager.GetSceneByName(scenesList[scenesList.Count - 1]));
		doSomethingAtTheEndOfProcessEvent?.Invoke();
		SceneManager.UnloadSceneAsync(loadScene);
		yield return null;
	}

	private void DetectCamera()
	{
		Camera[] array = Object.FindObjectsOfType<Camera>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != camToDelete)
			{
				camToDelete.gameObject.SetActive(value: false);
				break;
			}
		}
	}

	public void DoSomethingAtTheEndOfTheProcess()
	{
		Debug.Log("Do something when the loaded process ended.");
	}

	public void LoadingBar(int i, AsyncOperation asyncLoad)
	{
		int num = 100 / scenesList.Count;
		float f = (float)(num * i) + (float)num * asyncLoad.progress;
		f = Mathf.Round(f);
		if ((bool)txtLoading)
		{
			txtLoading.text = f + "%";
		}
		if ((bool)imLoading)
		{
			imLoading.fillAmount = f * 0.01f;
		}
	}
}
