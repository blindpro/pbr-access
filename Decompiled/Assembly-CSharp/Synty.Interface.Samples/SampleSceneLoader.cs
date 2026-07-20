using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Synty.Interface.Samples;

public class SampleSceneLoader : MonoBehaviour
{
	[Header("References")]
	public Animator animator;

	public CanvasGroup canvasGroup;

	public RectTransform titleScreen;

	public RectTransform contentsScreen;

	public TextMeshProUGUI titleText;

	public RectTransform contentParent;

	[Header("Parameters")]
	public bool showCursor;

	private List<RectTransform> contentList = new List<RectTransform>();

	private RectTransform currentContent;

	private void Awake()
	{
		contentList = (from screen in contentParent.GetComponentsInChildren<RectTransform>(includeInactive: true)
			where screen.parent == contentParent
			select screen).ToList();
		contentList.Insert(0, null);
		titleScreen.gameObject.SetActive(value: true);
		contentsScreen.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		if ((bool)animator)
		{
			animator.gameObject.SetActive(value: true);
			animator.SetBool("Active", value: false);
		}
		if (showCursor)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	public void QuitApplication()
	{
		Application.Quit();
	}

	public void ActivateContent(int index)
	{
		StartCoroutine(C_ActivateContent(index));
	}

	private IEnumerator C_ActivateContent(int index)
	{
		canvasGroup.interactable = false;
		if ((bool)animator)
		{
			animator.gameObject.SetActive(value: true);
			animator.SetBool("Active", value: true);
			yield return new WaitForSeconds(0.4f);
			animator.SetBool("Active", value: false);
		}
		foreach (RectTransform content in contentList)
		{
			if ((bool)content)
			{
				content.gameObject.SetActive(value: false);
			}
		}
		currentContent = contentList[index];
		currentContent.gameObject.SetActive(value: true);
		titleText.text = currentContent.name;
		titleScreen.gameObject.SetActive(value: false);
		contentsScreen.gameObject.SetActive(value: true);
		canvasGroup.interactable = true;
	}

	public void ActivateNextContent()
	{
		int num = contentList.IndexOf(currentContent) + 1;
		if (num >= contentList.Count)
		{
			num = 1;
		}
		ActivateContent(num);
	}

	public void ActivatePreviousContent()
	{
		int num = contentList.IndexOf(currentContent) - 1;
		if (num < 1)
		{
			num = contentList.Count - 1;
		}
		ActivateContent(num);
	}

	public void ActivateTitleScreen()
	{
		StartCoroutine(C_ActivateTitleScreen());
	}

	private IEnumerator C_ActivateTitleScreen()
	{
		canvasGroup.interactable = false;
		if ((bool)animator)
		{
			animator.gameObject.SetActive(value: true);
			animator.SetBool("Active", value: true);
			yield return new WaitForSeconds(0.4f);
			animator.SetBool("Active", value: false);
		}
		titleScreen.gameObject.SetActive(value: true);
		contentsScreen.gameObject.SetActive(value: false);
		canvasGroup.interactable = true;
	}
}
