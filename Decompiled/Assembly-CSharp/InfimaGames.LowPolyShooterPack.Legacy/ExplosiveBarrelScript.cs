using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class ExplosiveBarrelScript : MonoBehaviour
{
	private float randomTime;

	private bool routineStarted;

	public bool explode;

	[Header("Prefabs")]
	public Transform explosionPrefab;

	public Transform destroyedBarrelPrefab;

	[Header("Customizable Options")]
	public float minTime = 0.05f;

	public float maxTime = 0.25f;

	[Header("Explosion Options")]
	public float explosionRadius = 12.5f;

	public float explosionForce = 4000f;

	private void Update()
	{
		randomTime = Random.Range(minTime, maxTime);
		if (explode && !routineStarted)
		{
			StartCoroutine(Explode());
			routineStarted = true;
		}
	}

	private IEnumerator Explode()
	{
		yield return new WaitForSeconds(randomTime);
		Object.Instantiate(destroyedBarrelPrefab, base.transform.position, base.transform.rotation);
		Vector3 position = base.transform.position;
		Collider[] array = Physics.OverlapSphere(position, explosionRadius);
		foreach (Collider collider in array)
		{
			Rigidbody component = collider.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.AddExplosionForce(explosionForce * 50f, position, explosionRadius);
			}
			if (collider.transform.tag == "ExplosiveBarrel")
			{
				collider.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
			}
			if (collider.transform.tag == "Target")
			{
				collider.transform.gameObject.GetComponent<TargetScript>().isHit = true;
			}
			if (collider.GetComponent<Collider>().tag == "GasTank")
			{
				collider.gameObject.GetComponent<GasTankScript>().isHit = true;
				collider.gameObject.GetComponent<GasTankScript>().explosionTimer = 0.05f;
			}
		}
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 50f))
		{
			Object.Instantiate(explosionPrefab, hitInfo.point, Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
		}
		Object.Destroy(base.gameObject);
	}
}
