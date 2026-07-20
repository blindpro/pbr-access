using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics;

public class FramesPerSecond : MonoBehaviour
{
	private Text txt;

	private float timeBetweenTwoFPSCalculation = 0.5f;

	private float timer;

	private List<int> fpsList;

	private void Start()
	{
		txt = GetComponent<Text>();
		fpsList = new List<int>();
	}

	private void Update()
	{
		CalculateFPS();
	}

	private void CalculateFPS()
	{
		if (timer > timeBetweenTwoFPSCalculation)
		{
			int num = 0;
			for (int i = 0; i < fpsList.Count; i++)
			{
				num += fpsList[i];
			}
			num /= fpsList.Count;
			txt.text = $"{num} FPS";
			timer = 0f;
			fpsList.Clear();
		}
		else
		{
			fpsList.Add((int)(1f / Time.deltaTime));
		}
		timer += Time.deltaTime;
	}
}
