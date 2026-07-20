using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class LightMovementScript : MonoBehaviour
{
	private Vector3 StartPos;

	private Vector3 randomPos;

	public float minIntensity = 0.25f;

	public float maxIntensity = 0.5f;

	private float random;

	private float TimeSinceRandomRefresh = 9999f;

	private void Start()
	{
		StartPos = base.transform.position;
		random = Random.Range(0f, 25000f);
	}

	private void Update()
	{
		setRandomPos(0.1f);
		RandomLerpPos(0.2f);
		float t = Mathf.PerlinNoise(random, Time.time);
		GetComponent<Light>().intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
	}

	private void RandomLerpPos(float speed)
	{
		Vector3 position = Vector3.Lerp(base.transform.position, randomPos, Time.deltaTime * speed);
		base.transform.position = position;
	}

	private void setRandomPos(float interval)
	{
		if (TimeSinceRandomRefresh > interval)
		{
			randomPos = Random.insideUnitSphere;
			randomPos += StartPos;
			TimeSinceRandomRefresh = 0f;
		}
		else
		{
			TimeSinceRandomRefresh += Time.deltaTime;
		}
	}
}
