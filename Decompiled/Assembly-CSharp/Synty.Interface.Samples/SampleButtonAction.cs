using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synty.Interface.Samples;

public class SampleButtonAction : MonoBehaviour
{
	[Header("References")]
	public Button button;

	public List<GameObject> toggleObjects;

	public GameObject activateObject;

	[Header("Parameters")]
	public List<AnimatorActionData> animatorActions;

	public float activeTime = 1f;

	public bool runOnEnable;

	public bool applyRandomRotationToActivateObject;

	private void Awake()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		if (!(button == null))
		{
			button.onClick.AddListener(OnClick);
		}
	}

	private void Reset()
	{
		button = GetComponent<Button>();
	}

	private void OnEnable()
	{
		if (runOnEnable)
		{
			OnClick();
		}
	}

	private void OnClick()
	{
		if ((bool)activateObject)
		{
			StartCoroutine(C_ActivateObject());
		}
		foreach (GameObject toggleObject in toggleObjects)
		{
			toggleObject.SetActive(!toggleObject.activeSelf);
		}
		foreach (AnimatorActionData animatorAction in animatorActions)
		{
			animatorAction.Execute();
		}
	}

	private IEnumerator C_ActivateObject()
	{
		if (!(activateObject == null))
		{
			activateObject.SetActive(value: true);
			if (applyRandomRotationToActivateObject)
			{
				activateObject.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0, 360));
			}
			yield return new WaitForSeconds(activeTime);
			activateObject.SetActive(value: false);
		}
	}
}
