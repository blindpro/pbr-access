using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class GrenadeScript : MonoBehaviour
{
	public byte projectilDamage = byte.MaxValue;

	public Character character;

	[Header("Timer")]
	[Tooltip("Time before the grenade explodes")]
	public float grenadeTimer = 5f;

	[Header("Explosion Prefabs")]
	public Transform explosionPrefab;

	[Header("Explosion Options")]
	[Tooltip("The radius of the explosion force")]
	public float radius = 25f;

	[Tooltip("The intensity of the explosion force")]
	public float power = 350f;

	[Header("Throw Force")]
	[Tooltip("Minimum throw force")]
	public float minimumForce = 1500f;

	[Tooltip("Maximum throw force")]
	public float maximumForce = 2500f;

	private float throwForce;

	[Header("Audio")]
	public AudioSource impactSound;

	private void Awake()
	{
		throwForce = Random.Range(minimumForce, maximumForce);
		GetComponent<Rigidbody>().AddRelativeTorque(Random.Range(500, 1500), Random.Range(0, 0), (float)Random.Range(0, 0) * Time.deltaTime * 5000f);
	}

	private void Start()
	{
		GetComponent<Rigidbody>().AddForce(base.gameObject.transform.forward * throwForce * 2f);
		StartCoroutine(ExplosionTimer());
	}

	private void KillPlayers(Vector3 explosionPos)
	{
		if (character == null)
		{
			return;
		}
		CharacterMultiplayer component = character.GetComponent<CharacterMultiplayer>();
		if (!component || !component.isLocal)
		{
			return;
		}
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if (!character || !(character != component) || character.IsDead() || component.IsSquadMember(character))
			{
				continue;
			}
			CharacterBot component2 = character.GetComponent<CharacterBot>();
			Vector3 characterUpperChestPos = CharacterBot.GetCharacterUpperChestPos(character.GetComponent<ThirdPerson>());
			if (Vector3.Distance(explosionPos, characterUpperChestPos) < radius && !Physics.Linecast(explosionPos, characterUpperChestPos, out var _, component2.defaultAndGroundLayer, QueryTriggerInteraction.Ignore))
			{
				if (component.isMainPlayer)
				{
					GameManager.Instance.GetComponent<HitCursorsManager>().ShowHitCursor();
				}
				if (component.isLocal)
				{
					this.character.GetEquippedWeapon();
					byte damage = projectilDamage;
					character.RPC_Damage(damage, (byte)component.ActorNumber);
				}
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		impactSound.Play();
	}

	private IEnumerator ExplosionTimer()
	{
		yield return new WaitForSeconds(grenadeTimer);
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 50f))
		{
			Object.Instantiate(explosionPrefab, hitInfo.point, Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
		}
		KillPlayers(base.transform.position);
		Object.Destroy(base.gameObject);
	}
}
