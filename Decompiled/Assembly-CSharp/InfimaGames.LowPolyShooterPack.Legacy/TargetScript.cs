using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class TargetScript : MonoBehaviour
{
	private float randomTime;

	private bool routineStarted;

	public bool isHit;

	[Header("Customizable Options")]
	public float minTime;

	public float maxTime;

	[Header("Audio")]
	public AudioClip upSound;

	public AudioClip downSound;

	[Header("Animations")]
	public AnimationClip targetUp;

	public AnimationClip targetDown;

	public AudioSource audioSource;

	private void Update()
	{
		randomTime = Random.Range(minTime, maxTime);
		if (isHit && !routineStarted)
		{
			base.gameObject.GetComponent<Animation>().clip = targetDown;
			base.gameObject.GetComponent<Animation>().Play();
			audioSource.GetComponent<AudioSource>().clip = downSound;
			audioSource.Play();
			StartCoroutine(DelayTimer());
			routineStarted = true;
		}
	}

	private IEnumerator DelayTimer()
	{
		yield return new WaitForSeconds(randomTime);
		base.gameObject.GetComponent<Animation>().clip = targetUp;
		base.gameObject.GetComponent<Animation>().Play();
		audioSource.GetComponent<AudioSource>().clip = upSound;
		audioSource.Play();
		isHit = false;
		routineStarted = false;
	}
}
