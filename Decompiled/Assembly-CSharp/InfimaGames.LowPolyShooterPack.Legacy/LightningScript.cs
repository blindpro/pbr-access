using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class LightningScript : MonoBehaviour
{
	[Header("Light Intensity")]
	public float minIntensity = 1f;

	public float maxIntensity = 3f;

	[Header("Light Duration")]
	public float lightDuration = 0.025f;

	[Header("Delay Between Flashes")]
	public float minFlashDelay = 0.05f;

	public float maxFlashDelay = 2f;

	[Header("Total Delay")]
	public float minDelay = 5f;

	public float maxDelay = 15f;

	private float delay;

	private float flashDelay;

	private bool isWaiting;

	[Header("Background Color")]
	public Color mainBackgroundColor;

	public Color lightningBackgroundColor;

	[Header("Lightning Size")]
	public float minSize;

	public float maxSize;

	[Header("Components")]
	public Camera gunCamera;

	public Light lightObject;

	public AudioSource lightningSound;

	public Sprite[] lightningSprites;

	public SpriteRenderer lightningSpriteRenderer;

	private float x;

	private float y;

	private Vector3 lightningPos;

	private float lightningScale;

	private void Start()
	{
		lightObject.enabled = false;
		gunCamera.backgroundColor = mainBackgroundColor;
	}

	private void Update()
	{
		delay = Random.Range(minDelay, maxDelay);
		flashDelay = Random.Range(minFlashDelay, maxFlashDelay);
		if (!isWaiting)
		{
			StartCoroutine(LightFlashOne());
			isWaiting = true;
		}
	}

	private IEnumerator LightFlashOne()
	{
		lightObject.enabled = true;
		lightObject.intensity = Random.Range(minIntensity, maxIntensity);
		gunCamera.backgroundColor = lightningBackgroundColor;
		lightningSpriteRenderer.enabled = true;
		lightningSpriteRenderer.sprite = lightningSprites[Random.Range(0, lightningSprites.Length)];
		x = Random.Range(-100, 100);
		y = Random.Range(12, 28);
		lightningPos = new Vector3(x, y, 75f);
		lightningScale = Random.Range(minSize, maxSize);
		lightningSpriteRenderer.transform.position = lightningPos;
		lightningSpriteRenderer.transform.localScale = new Vector3(lightningScale, lightningScale, lightningScale);
		yield return new WaitForSeconds(lightDuration);
		lightObject.enabled = false;
		gunCamera.backgroundColor = mainBackgroundColor;
		lightningSpriteRenderer.enabled = false;
		StartCoroutine(FlashDelay());
	}

	private IEnumerator FlashDelay()
	{
		yield return new WaitForSeconds(flashDelay);
		StartCoroutine(LightFlashTwo());
	}

	private IEnumerator LightFlashTwo()
	{
		lightObject.enabled = true;
		lightObject.intensity = Random.Range(minIntensity, maxIntensity);
		gunCamera.backgroundColor = lightningBackgroundColor;
		lightningSpriteRenderer.enabled = true;
		lightningSpriteRenderer.sprite = lightningSprites[Random.Range(0, lightningSprites.Length)];
		x = Random.Range(-100, 100);
		y = Random.Range(12, 28);
		lightningPos = new Vector3(x, y, 75f);
		lightningScale = Random.Range(minSize, maxSize);
		lightningSpriteRenderer.transform.position = lightningPos;
		lightningSpriteRenderer.transform.localScale = new Vector3(lightningScale, lightningScale, lightningScale);
		lightningSound.Play();
		yield return new WaitForSeconds(lightDuration);
		lightObject.enabled = false;
		gunCamera.backgroundColor = mainBackgroundColor;
		lightningSpriteRenderer.enabled = false;
		StartCoroutine(Timer());
	}

	private IEnumerator Timer()
	{
		yield return new WaitForSeconds(delay);
		isWaiting = false;
	}
}
