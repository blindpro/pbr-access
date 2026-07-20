using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HP.Generics;

public class Intro : MonoBehaviour
{
	public string scenName;

	private void Start()
	{
		LoadAsyncScenes();
	}

	public void LoadAsyncScenes()
	{
		StartCoroutine(LoadAsyncScenesRoutine());
	}

	private IEnumerator LoadAsyncScenesRoutine()
	{
		yield return new WaitForSeconds(2f);
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenName);
		while (!asyncLoad.isDone)
		{
			yield return null;
		}
		yield return null;
	}
}
