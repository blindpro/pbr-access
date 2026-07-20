using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Synty.Interface.Samples;

public class SampleTimeLabel : MonoBehaviour
{
	[Header("References")]
	public TMP_Text label;

	[Header("Parameters")]
	public bool is24Hour = true;

	public float timeToRefreshInSeconds = 1f;

	private bool beat;

	private void Awake()
	{
		if (label == null)
		{
			label = GetComponent<TMP_Text>();
		}
	}

	private void OnEnable()
	{
		StartCoroutine(C_UpdateTime());
	}

	private void OnDisable()
	{
		StopCoroutine(C_UpdateTime());
	}

	public string GetCurrentTimeString()
	{
		if (!is24Hour)
		{
			return DateTime.Now.ToString("hh:mm tt");
		}
		if (beat)
		{
			return DateTime.Now.ToString("HH<color=#AAAAAA>:</color>mm");
		}
		return DateTime.Now.ToString("HH:mm");
	}

	[ContextMenu("Update Time")]
	public void UpdateTime()
	{
		label.SetText(GetCurrentTimeString());
	}

	private IEnumerator C_UpdateTime()
	{
		while (true)
		{
			UpdateTime();
			beat = !beat;
			yield return new WaitForSecondsRealtime(timeToRefreshInSeconds);
		}
	}
}
