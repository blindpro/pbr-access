using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class GasTankScript : MonoBehaviour
{
	private float randomRotationValue;

	private float randomValue;

	private bool routineStarted;

	public bool isHit;

	[Header("Prefabs")]
	public Transform explosionPrefab;

	public Transform destroyedGasTankPrefab;

	[Header("Customizable Options")]
	public float explosionTimer;

	public float rotationSpeed;

	public float maxRotationSpeed;

	public float moveSpeed;

	public float audioPitchIncrease = 0.5f;

	[Header("Explosion Options")]
	public float explosionRadius = 12.5f;

	public float explosionForce = 4000f;

	[Header("Light")]
	public Light lightObject;

	[Header("Particle Systems")]
	public ParticleSystem flameParticles;

	public ParticleSystem smokeParticles;

	[Header("Audio")]
	public AudioSource flameSound;

	public AudioSource impactSound;

	private bool audioHasPlayed;

	private void Start()
	{
		lightObject.intensity = 0f;
		randomValue = Random.Range(-50, 50);
	}

	private void Update()
	{
		if (isHit)
		{
			randomRotationValue += 1f * Time.deltaTime;
			if (randomRotationValue > maxRotationSpeed)
			{
				randomRotationValue = maxRotationSpeed;
			}
			base.gameObject.GetComponent<Rigidbody>().AddRelativeForce(Vector3.down * moveSpeed * 50f * Time.deltaTime);
			base.transform.Rotate(randomRotationValue, 0f, randomValue * rotationSpeed * Time.deltaTime);
			flameParticles.Play();
			smokeParticles.Play();
			flameSound.pitch += audioPitchIncrease * Time.deltaTime;
			if (!audioHasPlayed)
			{
				flameSound.Play();
				audioHasPlayed = true;
			}
			if (!routineStarted)
			{
				StartCoroutine(Explode());
				routineStarted = true;
				lightObject.intensity = 3f;
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		impactSound.Play();
	}

	private IEnumerator Explode()
	{
		yield return new WaitForSeconds(explosionTimer);
		Object.Instantiate(destroyedGasTankPrefab, base.transform.position, base.transform.rotation);
		Vector3 position = base.transform.position;
		Collider[] array = Physics.OverlapSphere(position, explosionRadius);
		foreach (Collider collider in array)
		{
			Rigidbody component = collider.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.AddExplosionForce(explosionForce * 50f, position, explosionRadius);
			}
			if (collider.transform.tag == "GasTank")
			{
				collider.transform.gameObject.GetComponent<GasTankScript>().isHit = true;
			}
			if (collider.transform.tag == "ExplosiveBarrel")
			{
				collider.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
			}
		}
		Object.Instantiate(explosionPrefab, base.transform.position, base.transform.rotation);
		Object.Destroy(base.gameObject);
	}
}
