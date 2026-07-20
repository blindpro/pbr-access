using UnityEngine;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
	private Sharpen _sharpen;

	private void Update()
	{
		_sharpen = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Sharpen>();
	}

	public void MoveSlider(Slider a)
	{
		_sharpen.Sharpness = a.value;
	}
}
