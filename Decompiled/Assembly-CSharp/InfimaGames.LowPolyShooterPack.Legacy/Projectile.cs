using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class Projectile : MonoBehaviour
{
	public CharacterBehaviour character;

	public bool isBullet;

	public float bulletSpeed = 100f;

	public Vector3 bulletEndPoint;

	public Vector3 bulletStartPoint;

	public Vector3 bulletEndNormal;

	public Collider bulletEndCollider;

	public bool isBulletImpacted;

	[Range(5f, 100f)]
	[Tooltip("After how long time should the bullet prefab be destroyed?")]
	public float destroyAfter;

	[Tooltip("If enabled the bullet destroys on impact")]
	public bool destroyOnImpact;

	[Tooltip("Minimum time after impact that the bullet is destroyed")]
	public float minDestroyTime;

	[Tooltip("Maximum time after impact that the bullet is destroyed")]
	public float maxDestroyTime;

	[Header("Impact Effect Prefabs")]
	public Transform[] bloodImpactPrefabs;

	public Transform[] metalImpactPrefabs;

	public Transform[] dirtImpactPrefabs;

	public Transform[] concreteImpactPrefabs;

	private void Start()
	{
		ServiceLocator.Current.Get<IGameModeService>();
		if (!isBullet)
		{
			Physics.IgnoreCollision(character.GetComponent<Collider>(), GetComponent<Collider>());
			StartCoroutine(DestroyAfter());
		}
	}

	private void Update()
	{
		if (!isBullet || isBulletImpacted)
		{
			return;
		}
		base.transform.position = base.transform.position + base.transform.forward * bulletSpeed * Time.deltaTime;
		if (!(Vector3.Distance(bulletStartPoint, base.transform.position) > Vector3.Distance(bulletStartPoint, bulletEndPoint)))
		{
			return;
		}
		base.transform.position = bulletStartPoint + base.transform.forward * Vector3.Distance(bulletStartPoint, bulletEndPoint);
		isBulletImpacted = true;
		if (!bulletEndCollider)
		{
			return;
		}
		CharacterBone component = bulletEndCollider.GetComponent<CharacterBone>();
		if ((bool)component && (bool)component.character && (bool)character)
		{
			CharacterMultiplayer component2 = character.GetComponent<CharacterMultiplayer>();
			CharacterMultiplayer component3 = component.character.GetComponent<CharacterMultiplayer>();
			if (!(component3 != component2) || component3.IsDead() || component2.IsSquadMember(component3))
			{
				return;
			}
			PoolsManager.Instance.bulletsImpactBlood.CreatePrefab(bulletEndPoint, Quaternion.identity, bulletEndNormal, useNormal: true);
			if (component2.isMainPlayer)
			{
				GameManager.Instance.GetComponent<HitCursorsManager>().ShowHitCursor();
			}
			if (component2.isLocal)
			{
				Weapon equippedWeapon = ((Character)character).GetEquippedWeapon();
				byte damage = 64;
				if ((bool)equippedWeapon)
				{
					damage = equippedWeapon.weaponDamage;
				}
				component3.RPC_Damage(damage, (byte)component2.ActorNumber);
			}
		}
		else
		{
			PoolsManager.Instance.bulletsImpact.CreatePrefab(bulletEndPoint, Quaternion.identity, bulletEndNormal, useNormal: true);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!isBullet && !(collision.gameObject.GetComponent<Projectile>() != null))
		{
			if (!destroyOnImpact)
			{
				StartCoroutine(DestroyTimer());
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "Blood")
			{
				Object.Instantiate(bloodImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)], base.transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "Metal")
			{
				Object.Instantiate(metalImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)], base.transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "Dirt")
			{
				Object.Instantiate(dirtImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)], base.transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "Concrete")
			{
				Object.Instantiate(concreteImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)], base.transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "Target")
			{
				collision.transform.gameObject.GetComponent<TargetScript>().isHit = true;
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "ExplosiveBarrel")
			{
				collision.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
				Object.Destroy(base.gameObject);
			}
			if (collision.transform.tag == "GasTank")
			{
				collision.transform.gameObject.GetComponent<GasTankScript>().isHit = true;
				Object.Destroy(base.gameObject);
			}
		}
	}

	private IEnumerator DestroyTimer()
	{
		yield return new WaitForSeconds(Random.Range(minDestroyTime, maxDestroyTime));
		Object.Destroy(base.gameObject);
	}

	private IEnumerator DestroyAfter()
	{
		yield return new WaitForSeconds(destroyAfter);
		Object.Destroy(base.gameObject);
	}
}
