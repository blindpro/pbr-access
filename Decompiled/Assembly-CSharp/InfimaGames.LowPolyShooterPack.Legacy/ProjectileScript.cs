using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class ProjectileScript : MonoBehaviour
{
	public byte projectilDamage = 32;

	public Character character;

	private bool explodeSelf;

	[Tooltip("Enable to use constant force, instead of force at launch only")]
	public bool useConstantForce;

	[Tooltip("How fast the projectile moves")]
	public float constantForceSpeed;

	[Tooltip("How long after spawning that the projectile self destructs")]
	public float explodeAfter;

	private bool hasStartedExplode;

	private bool hasCollided;

	[Header("Explosion Prefabs")]
	public Transform explosionPrefab;

	[Header("Customizable Options")]
	[Tooltip("Initial launch force")]
	public float force = 5000f;

	[Tooltip("How long after spawning should the projectile object destroy")]
	public float despawnTime = 30f;

	[Header("Explosion Options")]
	[Tooltip("Explosion radius")]
	public float radius = 50f;

	[Tooltip("Explosion intensity")]
	public float power = 250f;

	[Header("Rocket Launcher Projectile")]
	[Tooltip("Enabled if the projectile has particle effects")]
	public bool usesParticles;

	public ParticleSystem smokeParticles;

	public ParticleSystem flameParticles;

	[Tooltip("Added delay to let particle effects finish playing, before destroying object")]
	public float destroyDelay;

	private void Start()
	{
		if (!useConstantForce)
		{
			GetComponent<Rigidbody>().AddForce(base.gameObject.transform.forward * force);
		}
		StartCoroutine(DestroyTimer());
	}

	private void FixedUpdate()
	{
		if (GetComponent<Rigidbody>().velocity != Vector3.zero)
		{
			GetComponent<Rigidbody>().rotation = Quaternion.LookRotation(GetComponent<Rigidbody>().velocity);
		}
		if (useConstantForce && !hasStartedExplode)
		{
			GetComponent<Rigidbody>().AddForce(base.gameObject.transform.forward * constantForceSpeed);
			StartCoroutine(ExplodeSelf());
			hasStartedExplode = true;
		}
	}

	private IEnumerator ExplodeSelf()
	{
		yield return new WaitForSeconds(explodeAfter);
		if (!hasCollided)
		{
			Object.Instantiate(explosionPrefab, base.transform.position, base.transform.rotation);
		}
		base.gameObject.GetComponent<MeshRenderer>().enabled = false;
		base.gameObject.GetComponent<Rigidbody>().isKinematic = true;
		base.gameObject.GetComponent<BoxCollider>().isTrigger = true;
		if (usesParticles)
		{
			flameParticles.GetComponent<ParticleSystem>().Stop();
			smokeParticles.GetComponent<ParticleSystem>().Stop();
		}
		KillPlayers(base.transform.position);
		yield return new WaitForSeconds(destroyDelay);
		Object.Destroy(base.gameObject);
	}

	private IEnumerator DestroyTimer()
	{
		yield return new WaitForSeconds(despawnTime);
		Object.Destroy(base.gameObject);
	}

	private IEnumerator DestroyTimerAfterCollision()
	{
		yield return new WaitForSeconds(destroyDelay);
		Object.Destroy(base.gameObject);
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
					byte damage = projectilDamage;
					character.RPC_Damage(damage, (byte)component.ActorNumber);
				}
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.transform.CompareTag("Player"))
		{
			return;
		}
		CharacterBone component = collision.gameObject.GetComponent<CharacterBone>();
		if (!component || !(component.character == character))
		{
			hasCollided = true;
			Debug.Log("grenade launcher collision " + collision.collider.name);
			base.gameObject.GetComponent<MeshRenderer>().enabled = false;
			base.gameObject.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			base.gameObject.GetComponent<Rigidbody>().isKinematic = true;
			base.gameObject.GetComponent<BoxCollider>().isTrigger = true;
			if (usesParticles)
			{
				flameParticles.GetComponent<ParticleSystem>().Stop();
				smokeParticles.GetComponent<ParticleSystem>().Stop();
			}
			StartCoroutine(DestroyTimerAfterCollision());
			Object.Instantiate(explosionPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
			if ((bool)component)
			{
				PoolsManager.Instance.bulletsImpactBlood.CreatePrefab(collision.contacts[0].point, Quaternion.identity, collision.contacts[0].normal, useNormal: true);
			}
			KillPlayers(base.transform.position);
		}
	}
}
