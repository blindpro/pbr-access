using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Synty.Interface.Samples;

public class SampleCountdownLabel : MonoBehaviour
{
	[Header("References")]
	public Animator animator;

	public TMP_Text text;

	[Header("Parameters")]
	public float countdownTime = 30f;

	public float updateInterval = 0.1f;

	public float timeUpDuration = 2.5f;

	public UnityEvent onCountdownComplete;

	private float currentTime;

	private void Reset()
	{
		text = GetComponentInChildren<TMP_Text>();
	}

	private void OnEnable()
	{
		BeginTimer();
	}

	private void BeginTimer()
	{
		currentTime = countdownTime;
		RefreshUI();
		StartCoroutine(C_TickDown());
	}

	private IEnumerator C_TickDown()
	{
		while (currentTime > 0f)
		{
			yield return new WaitForSeconds(updateInterval);
			currentTime -= updateInterval;
			if (currentTime <= 0f)
			{
				currentTime = 0f;
			}
			RefreshUI();
		}
		animator?.gameObject.SetActive(value: true);
		animator?.SetBool("Active", value: true);
		yield return new WaitForSeconds(timeUpDuration);
		animator?.SetBool("Active", value: false);
		yield return new WaitForSeconds(1f);
		animator?.gameObject.SetActive(value: false);
		onCountdownComplete?.Invoke();
		BeginTimer();
	}

	private void RefreshUI()
	{
		text.SetText(currentTime.ToString("F1"));
	}
}
