using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HP.Generics;

public class LoadAScene : MonoBehaviour
{
	public void LoadASceneAsync(string name)
	{
		StartCoroutine(LoadAsyncSceneRoutine(name));
	}

	private IEnumerator LoadAsyncSceneRoutine(string name)
	{
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name);
		while (!asyncLoad.isDone)
		{
			yield return null;
		}
	}
}
