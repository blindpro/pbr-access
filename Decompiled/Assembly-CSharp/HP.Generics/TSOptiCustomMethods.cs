using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics;

public class TSOptiCustomMethods : MonoBehaviour
{
	public GameObject loadingScreen;

	public void DisableCharacterMovement(GameObject obj)
	{
		ActionProcessDone(obj);
	}

	public void MoveCharacter(GameObject obj)
	{
		StartCoroutine(MoveCharacterRoutine(obj));
	}

	private IEnumerator MoveCharacterRoutine(GameObject obj)
	{
		ActionProcessDone(obj);
		yield return null;
	}

	public void UpdateCameraPosition(GameObject obj)
	{
		ActionProcessDone(obj);
	}

	public void EnableCharacterMovement(GameObject obj)
	{
		ActionProcessDone(obj);
	}

	public void DisableLoadingScreen(GameObject obj)
	{
		StartCoroutine(DisableLoadingScreenRoutine(obj));
	}

	public IEnumerator DisableLoadingScreenRoutine(GameObject obj)
	{
		_ = (bool)loadingScreen;
		if ((bool)loadingScreen)
		{
			Image imLoadingScreen = loadingScreen.transform.GetChild(0).GetComponent<Image>();
			loadingScreen.SetActive(value: true);
			Color loadingScreenColor = imLoadingScreen.color;
			while (imLoadingScreen.color.a > 0f)
			{
				float a = imLoadingScreen.color.a;
				a = Mathf.MoveTowards(a, 0f, Time.deltaTime * 2f);
				imLoadingScreen.color = new Color(loadingScreenColor.r, loadingScreenColor.g, loadingScreenColor.b, a);
				yield return null;
			}
			loadingScreen.SetActive(value: false);
		}
		if ((bool)obj)
		{
			ActionProcessDone(obj);
		}
		yield return null;
	}

	public void EnableLoadingScreen(GameObject obj)
	{
		if ((bool)loadingScreen)
		{
			loadingScreen.SetActive(value: true);
		}
		StartCoroutine(EnableLoadingScreenRoutine(obj));
	}

	public IEnumerator EnableLoadingScreenRoutine(GameObject obj)
	{
		if ((bool)loadingScreen)
		{
			Image imLoadingScreen = loadingScreen.transform.GetChild(0).GetComponent<Image>();
			if (imLoadingScreen.color.a != 1f)
			{
				loadingScreen.SetActive(value: true);
				Color loadingScreenColor = imLoadingScreen.color;
				while (imLoadingScreen.color.a < 1f)
				{
					float a = imLoadingScreen.color.a;
					a = Mathf.MoveTowards(a, 1f, Time.deltaTime * 4f);
					imLoadingScreen.color = new Color(loadingScreenColor.r, loadingScreenColor.g, loadingScreenColor.b, a);
					yield return null;
				}
			}
		}
		ActionProcessDone(obj);
		yield return null;
	}

	public void ActionProcessDone(GameObject obj)
	{
		obj.GetComponent<IValidateAction<bool>>().ValidateAction(actionState: true);
	}

	public void ResetLocalPostFx(GameObject obj)
	{
		EyeAdaptationTrigger[] array = Object.FindObjectsOfType<EyeAdaptationTrigger>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SpawnForceInsideEyeAdaptationTransition();
		}
		ActionProcessDone(obj);
	}

	public void DisableNewLocation(GameObject obj)
	{
		StartCoroutine(DisableNewLocationRoutine(obj));
	}

	private IEnumerator DisableNewLocationRoutine(GameObject obj)
	{
		GroupLocationTag groupLocationTag = Object.FindObjectOfType<GroupLocationTag>();
		if ((bool)groupLocationTag)
		{
			groupLocationTag.transform.GetChild(0).gameObject.SetActive(value: false);
			yield return new WaitUntil(() => !groupLocationTag.transform.GetChild(0).gameObject.activeSelf);
			NewLocationCanvasManager.instance.TextFromNewLocationTrigger("", null);
		}
		ActionProcessDone(obj);
		yield return null;
	}

	public void EnableNewLocation(GameObject obj)
	{
		StartCoroutine(EnableNewLocationRoutine(obj));
	}

	private IEnumerator EnableNewLocationRoutine(GameObject obj)
	{
		GroupLocationTag groupLocationTag = Object.FindObjectOfType<GroupLocationTag>();
		if ((bool)groupLocationTag)
		{
			groupLocationTag.transform.GetChild(0).gameObject.SetActive(value: true);
			yield return new WaitUntil(() => groupLocationTag.transform.GetChild(0).gameObject.activeSelf);
		}
		ActionProcessDone(obj);
		yield return null;
	}
}
