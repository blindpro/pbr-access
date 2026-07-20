using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class PoolsManager : MonoBehaviour
{
	public static PoolsManager Instance;

	public PrefabsInstancer bullets;

	public GameObject bulletPrefab;

	public int bulletsCount = 100;

	public PrefabsInstancer bulletsImpact;

	public GameObject bulletImpactPrefab;

	public int bulletsImpactCount = 100;

	public PrefabsInstancer bulletsImpactBlood;

	public GameObject bulletImpactBloodPrefab;

	public int bulletsImpactBloodCount = 50;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		bullets = new PrefabsInstancer();
		bullets.Prefab = bulletPrefab;
		bullets.PrefabsCount = bulletsCount;
		bullets.CreatePrefabs(base.transform);
		bulletsImpact = new PrefabsInstancer();
		bulletsImpact.Prefab = bulletImpactPrefab;
		bulletsImpact.PrefabsCount = bulletsImpactCount;
		bulletsImpact.CreatePrefabs(base.transform);
		bulletsImpactBlood = new PrefabsInstancer();
		bulletsImpactBlood.Prefab = bulletImpactBloodPrefab;
		bulletsImpactBlood.PrefabsCount = bulletsImpactBloodCount;
		bulletsImpactBlood.CreatePrefabs(base.transform);
	}

	private void Update()
	{
	}
}
