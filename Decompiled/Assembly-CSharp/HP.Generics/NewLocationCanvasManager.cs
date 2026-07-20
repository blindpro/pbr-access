using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics;

public class NewLocationCanvasManager : MonoBehaviour
{
	public static NewLocationCanvasManager instance;

	public SpawnSystem spawnSystem;

	[HideInInspector]
	public string lastLocation;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	private void Start()
	{
		StartCoroutine(InitRoutine());
	}

	private IEnumerator InitRoutine()
	{
		base.transform.GetChild(0).gameObject.SetActive(value: false);
		base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
			.alpha = 0f;
		yield return null;
	}

	public void FadeTheText(GameObject obj)
	{
		StopAllCoroutines();
		StartCoroutine(FadeTheTextRoutine(obj));
	}

	private IEnumerator FadeTheTextRoutine(GameObject obj)
	{
		base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
			.alpha = 0f;
		base.transform.GetChild(0).gameObject.SetActive(value: true);
		float t = 0f;
		float duration = 1f;
		while (t < duration)
		{
			t += Time.deltaTime;
			base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
				.alpha = t;
			yield return null;
		}
		t = 0f;
		duration = 2f;
		while (t < duration)
		{
			t += Time.deltaTime;
			yield return null;
		}
		t = 0f;
		duration = 1f;
		while (t < duration)
		{
			t += Time.deltaTime;
			base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
				.alpha = 1f - t;
			yield return null;
		}
		if ((bool)obj)
		{
			ActionProcessDone(obj);
		}
		yield return null;
	}

	public void ActionProcessDone(GameObject obj)
	{
		obj.GetComponent<IValidateAction<bool>>().ValidateAction(actionState: true);
	}

	public void TextFromNewLocationTrigger(string newtext, GameObject objNewLocation)
	{
		StopAllCoroutines();
		StartCoroutine(TextFromNewLocationTriggerRoutine(newtext));
		if ((bool)objNewLocation)
		{
			objNewLocation.SetActive(value: false);
		}
	}

	private IEnumerator TextFromNewLocationTriggerRoutine(string newtext)
	{
		base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
			.alpha = 0f;
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(1)
			.GetComponent<Text>()
			.text = newtext;
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(2)
			.gameObject.SetActive(value: true);
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(3)
			.gameObject.SetActive(value: true);
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(4)
			.gameObject.SetActive(value: false);
		lastLocation = newtext;
		base.transform.GetChild(0).gameObject.SetActive(value: true);
		float t = 0f;
		float duration = 1f;
		while (t < duration)
		{
			t += Time.deltaTime;
			base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
				.alpha = t;
			yield return null;
		}
		t = 0f;
		duration = 4f;
		while (t < duration)
		{
			t += Time.deltaTime;
			yield return null;
		}
		t = 0f;
		duration = 1f;
		while (t < duration)
		{
			t += Time.deltaTime;
			base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
				.alpha = 1f - t;
			yield return null;
		}
		yield return null;
	}

	public void TextFromZoneLimitTriggerEnter(string newtext)
	{
		StopAllCoroutines();
		StartCoroutine(TextFromZoneLimitTriggerEnterRoutine(newtext));
	}

	private IEnumerator TextFromZoneLimitTriggerEnterRoutine(string newtext)
	{
		base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
			.alpha = 0f;
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(1)
			.GetComponent<Text>()
			.text = newtext;
		lastLocation = newtext;
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(2)
			.gameObject.SetActive(value: false);
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(3)
			.gameObject.SetActive(value: false);
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(4)
			.gameObject.SetActive(value: true);
		base.transform.GetChild(0).gameObject.SetActive(value: true);
		float t = 0f;
		float duration = 1f;
		while (t < duration)
		{
			t += Time.deltaTime;
			base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
				.alpha = t;
			yield return null;
		}
		yield return null;
	}

	public void TextFromZoneLimitTriggerExit(string newtext)
	{
		StopAllCoroutines();
		StartCoroutine(TextFromZoneLimitTriggerExitRoutine(newtext));
	}

	private IEnumerator TextFromZoneLimitTriggerExitRoutine(string newtext)
	{
		base.transform.GetChild(0).GetChild(0).GetChild(1)
			.GetChild(1)
			.GetComponent<Text>()
			.text = newtext;
		lastLocation = newtext;
		base.transform.GetChild(0).gameObject.SetActive(value: true);
		float t = 1f - base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
			.alpha;
		float duration = 1f;
		while (t < duration)
		{
			t += Time.deltaTime;
			base.transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>()
				.alpha = 1f - t;
			yield return null;
		}
		yield return null;
	}
}
