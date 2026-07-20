using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfimaGames.LowPolyShooterPack;

public class PortalTeleport : MonoBehaviour
{
	[Tooltip("Display name of the scene.")]
	[SerializeField]
	private string displayName;

	[Tooltip("Name of the scene to load.")]
	[SerializeField]
	private string sceneToLoad;

	[Tooltip("Loading Screen Object.")]
	[SerializeField]
	private GameObject loadingScreen;

	[Tooltip("Canvas Group.")]
	[SerializeField]
	private CanvasGroup canvasGroup;

	[Tooltip("Scene Text.")]
	[SerializeField]
	private TMP_Text sceneText;

	[Tooltip("Duration of the fade.")]
	[SerializeField]
	public float fadeDuration = 1f;

	private void Start()
	{
		canvasGroup.alpha = 0f;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			StartCoroutine(LoadScene());
		}
	}

	private IEnumerator LoadScene()
	{
		loadingScreen.SetActive(value: true);
		sceneText.text = displayName;
		yield return StartCoroutine(FadeLoadingScreen(1f, fadeDuration));
		AsyncOperation operation = null;
		operation = SceneManager.LoadSceneAsync(sceneToLoad, new LoadSceneParameters(LoadSceneMode.Single));
		yield return new WaitWhile(() => !operation.isDone);
		yield return StartCoroutine(FadeLoadingScreen(0f, fadeDuration));
		base.gameObject.SetActive(value: false);
	}

	private IEnumerator FadeLoadingScreen(float targetValue, float duration)
	{
		float startValue = canvasGroup.alpha;
		float time = 0f;
		while (time < duration)
		{
			canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time / duration);
			time += Time.deltaTime;
			yield return null;
		}
		canvasGroup.alpha = targetValue;
	}
}
