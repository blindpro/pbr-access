using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Synty.Interface.Samples;

public class SampleRadialFillBar : MonoBehaviour
{
	[Header("References")]
	public Image image;

	public TMP_Text text;

	[Header("Parameters")]
	public float fillAmountFull = 1f;

	public float inSpeed = 5f;

	public float outSpeed = 5f;

	public float startDelay;

	public float inDelay = 2.5f;

	public float outDelay = 2.5f;

	public string labelText = "{0}%";

	public string LabelText => string.Format(labelText, (image.fillAmount / fillAmountFull * 100f).ToString("0"));

	private void Awake()
	{
		if (image == null)
		{
			image = GetComponentInChildren<Image>();
		}
		if (text == null)
		{
			text = GetComponentInChildren<TMP_Text>();
		}
	}

	private void Reset()
	{
		image = GetComponentInChildren<Image>();
		text = GetComponentInChildren<TMP_Text>();
	}

	private void Start()
	{
		StartCoroutine(C_TweenBackAndForth());
	}

	private IEnumerator C_TweenBackAndForth()
	{
		yield return new WaitForSeconds(startDelay);
		while (true)
		{
			yield return C_TweenFloat(0f, 1f, inSpeed);
			yield return new WaitForSeconds(outDelay);
			yield return C_TweenFloat(1f, 0f, outSpeed);
			yield return new WaitForSeconds(inDelay);
		}
	}

	private IEnumerator C_TweenFloat(float startValue, float endValue, float duration)
	{
		float time = 0f;
		while (time < 1f)
		{
			time += Time.deltaTime / duration;
			float num = Mathf.Lerp(startValue, endValue, time);
			image.fillAmount = num * fillAmountFull;
			text?.SetText(LabelText);
			yield return null;
		}
	}
}
