using UnityEngine;
using UnityEngine.UI;

namespace Synty.Interface.Samples;

public class SampleScrollUV : MonoBehaviour
{
	[Header("References")]
	public RawImage rawImage;

	[Header("Parameters")]
	public Vector2 speed = new Vector2(1f, 0f);

	public Vector2 size = new Vector2(256f, 256f);

	private void Awake()
	{
		if (rawImage == null)
		{
			rawImage = GetComponent<RawImage>();
		}
	}

	private void Reset()
	{
		rawImage = GetComponent<RawImage>();
	}

	private void Update()
	{
		Vector2 vector = new Vector2(rawImage.rectTransform.rect.width / size.x, rawImage.rectTransform.rect.height / size.y);
		rawImage.uvRect = new Rect(rawImage.uvRect.position + speed * Time.deltaTime, vector);
	}
}
