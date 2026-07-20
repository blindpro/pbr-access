using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class BlinkingLightLPFP : MonoBehaviour
{
	[Header("Light Component")]
	public Light blinkingLight;

	[Header("Timers")]
	[Tooltip("How long the light is enabled")]
	public float blinkTimer = 0.03f;

	[Tooltip("How much time there is inbetween blinks")]
	public float blinkDuration = 2.5f;

	private void Start()
	{
		blinkingLight.enabled = false;
		StartCoroutine(BlinkTimer());
	}

	private IEnumerator BlinkTimer()
	{
		yield return new WaitForSeconds(blinkDuration);
		StartCoroutine(BlinkOnce());
	}

	private IEnumerator BlinkOnce()
	{
		blinkingLight.enabled = true;
		yield return new WaitForSeconds(blinkTimer);
		blinkingLight.enabled = false;
		StartCoroutine(BlinkTimer());
	}
}
