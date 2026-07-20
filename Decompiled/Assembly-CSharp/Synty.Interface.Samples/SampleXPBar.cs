using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Synty.Interface.Samples;

public class SampleXPBar : MonoBehaviour
{
	[Header("References")]
	public Animator animator;

	public Slider xpSlider;

	public TMP_Text xpText;

	public TMP_Text levelText;

	[Header("Parameters")]
	public int xpPerLevelUp = 1000;

	private int currentLevel;

	private float currentXPNormalized;

	private float secondsPerLevelUp;

	private void Awake()
	{
		currentLevel = Random.Range(1, 69);
		currentXPNormalized = 0f;
		secondsPerLevelUp = Random.Range(4f, 20f);
	}

	private void Reset()
	{
		List<RectTransform> list = new List<RectTransform>();
		foreach (Transform item in base.transform)
		{
			if (item is RectTransform)
			{
				list.Add(item as RectTransform);
			}
		}
		RectTransform rectTransform = list.SingleOrDefault((RectTransform c) => c.name.ToLower().Contains("xp"));
		if ((bool)rectTransform)
		{
			xpSlider = rectTransform.GetComponentInChildren<Slider>();
			xpText = rectTransform.transform.GetComponentInChildren<TMP_Text>();
		}
		RectTransform rectTransform2 = list.SingleOrDefault((RectTransform c) => c.name.ToLower().Contains("level"));
		if ((bool)rectTransform2)
		{
			levelText = rectTransform2.GetComponentInChildren<TMP_Text>();
		}
	}

	private void Update()
	{
		if ((bool)xpSlider)
		{
			xpSlider.value = currentXPNormalized;
		}
		if ((bool)xpText)
		{
			xpText.text = $"{Mathf.RoundToInt(currentXPNormalized * (float)xpPerLevelUp)}/{xpPerLevelUp}";
		}
		if ((bool)levelText)
		{
			levelText.text = $"{currentLevel}";
		}
		if (currentXPNormalized >= 1f)
		{
			currentLevel++;
			currentXPNormalized = 0f;
			if ((bool)animator)
			{
				animator.SetTrigger("LevelUp");
			}
		}
		currentXPNormalized += Time.deltaTime / secondsPerLevelUp;
	}
}
