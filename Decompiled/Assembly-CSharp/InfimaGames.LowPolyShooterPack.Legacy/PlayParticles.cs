using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class PlayParticles : MonoBehaviour
{
	[Header("Delay Settings")]
	public float initialDelay = 1f;

	public float waitBetweenPlaying = 5f;

	[Header("Particle Settings")]
	public ParticleSystem particles;

	[Range(0f, 1f)]
	public float particleScale = 1f;

	private void Start()
	{
		StartCoroutine(WaitBeforePlaying());
		particles.transform.localScale = new Vector3(particleScale, particleScale, particleScale);
	}

	private IEnumerator WaitBeforePlaying()
	{
		yield return new WaitForSeconds(initialDelay);
		StartCoroutine(PlayEffect());
	}

	private IEnumerator PlayEffect()
	{
		yield return new WaitForSeconds(waitBetweenPlaying);
		particles.Play();
		StartCoroutine(PlayEffect());
	}
}
