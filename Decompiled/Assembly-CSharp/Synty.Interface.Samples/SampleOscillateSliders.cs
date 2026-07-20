using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Synty.Interface.Samples;

public class SampleOscillateSliders : MonoBehaviour
{
	[Header("References")]
	public List<Slider> sliders;

	[Header("Parameters")]
	public bool autoGetSliders = true;

	public float speed = 1f;

	public float offset = 0.5f;

	private void Reset()
	{
		sliders = Object.FindObjectsOfType<Slider>().ToList();
	}

	private void Start()
	{
		if (autoGetSliders)
		{
			sliders = Object.FindObjectsOfType<Slider>().ToList();
		}
	}

	private void Update()
	{
		for (int i = 0; i < sliders.Count; i++)
		{
			sliders[i].value = Mathf.Sin(Time.time * speed + (float)i * offset) * 0.5f + 0.5f;
		}
	}
}
