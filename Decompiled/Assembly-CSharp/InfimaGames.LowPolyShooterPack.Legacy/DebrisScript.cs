using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class DebrisScript : MonoBehaviour
{
	[Header("Audio")]
	public AudioClip[] debrisSounds;

	public AudioSource audioSource;

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.relativeVelocity.magnitude > 50f)
		{
			audioSource.clip = debrisSounds[Random.Range(0, debrisSounds.Length)];
			audioSource.Play();
		}
	}
}
