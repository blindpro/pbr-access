using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Muzzle : MuzzleBehaviour
{
	[Tooltip("Socket at the tip of the Muzzle. Commonly used as a firing point.")]
	[SerializeField]
	private Transform socket;

	[Tooltip("Sprite. Displayed on the player's interface.")]
	[SerializeField]
	private Sprite sprite;

	[Tooltip("Audio clip played when firing through this muzzle.")]
	[SerializeField]
	private AudioClip audioClipFire;

	[Tooltip("Firing Particles Smoke.")]
	[SerializeField]
	private GameObject prefabFlashSmokeParticles;

	[Tooltip("Firing Particles.")]
	[SerializeField]
	private GameObject prefabFlashParticles;

	[Tooltip("Number of particles to emit when firing.")]
	[SerializeField]
	private int flashParticlesCount = 5;

	[Tooltip("Muzzle Flash Prefab. A small light we use when firing.")]
	[SerializeField]
	private GameObject prefabFlashLight;

	[Tooltip("Time that the light flashed stays active. After this time, it is disabled.")]
	[SerializeField]
	private float flashLightDuration;

	[Tooltip("Local offset applied to the light.")]
	[SerializeField]
	private Vector3 flashLightOffset;

	private ParticleSystem particles;

	private ParticleSystem particlesSmoke;

	private Light flashLight;

	private void Awake()
	{
		if (prefabFlashParticles != null)
		{
			GameObject gameObject = Object.Instantiate(prefabFlashParticles, socket);
			gameObject.transform.localPosition = default(Vector3);
			gameObject.transform.localEulerAngles = default(Vector3);
			particles = gameObject.GetComponent<ParticleSystem>();
		}
		if (prefabFlashSmokeParticles != null)
		{
			GameObject gameObject2 = Object.Instantiate(prefabFlashSmokeParticles, socket);
			gameObject2.transform.localPosition = default(Vector3);
			gameObject2.transform.localEulerAngles = default(Vector3);
			particlesSmoke = gameObject2.GetComponent<ParticleSystem>();
		}
		if ((bool)prefabFlashLight)
		{
			GameObject gameObject3 = Object.Instantiate(prefabFlashLight, socket);
			gameObject3.transform.localPosition = flashLightOffset;
			gameObject3.transform.localEulerAngles = default(Vector3);
			flashLight = gameObject3.GetComponent<Light>();
			flashLight.enabled = false;
		}
	}

	public override void Effect()
	{
		if (particles != null)
		{
			particles.Play(withChildren: true);
		}
		if (particlesSmoke != null)
		{
			particlesSmoke.Emit(12);
		}
		if (flashLight != null)
		{
			flashLight.enabled = true;
			StartCoroutine("DisableLight");
		}
	}

	public override Transform GetSocket()
	{
		return socket;
	}

	public override Sprite GetSprite()
	{
		return sprite;
	}

	public override AudioClip GetAudioClipFire()
	{
		return audioClipFire;
	}

	public override ParticleSystem GetParticlesFire()
	{
		return particles;
	}

	public override int GetParticlesFireCount()
	{
		return flashParticlesCount;
	}

	public override Light GetFlashLight()
	{
		return flashLight;
	}

	public override float GetFlashLightDuration()
	{
		return flashLightDuration;
	}

	private IEnumerator DisableLight()
	{
		yield return new WaitForSeconds(flashLightDuration);
		flashLight.enabled = false;
		if (particles != null)
		{
			particles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}
	}
}
