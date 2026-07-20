using System.Collections.Generic;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class WeaponAttachmentManager : WeaponAttachmentManagerBehaviour
{
	[Tooltip("Determines if the ironsights should be shown on the weapon model.")]
	[SerializeField]
	private bool scopeDefaultShow = true;

	[Tooltip("Default Scope!")]
	[SerializeField]
	private ScopeBehaviour scopeDefaultBehaviour;

	[Tooltip("Selected Scope Index. If you set this to a negative number, ironsights will be selected as the enabled scope.")]
	[SerializeField]
	public int scopeIndex = -1;

	[Tooltip("First scope index when using random scopes.")]
	[SerializeField]
	private int scopeIndexFirst = -1;

	[Tooltip("Should we pick a random index when starting the game?")]
	[SerializeField]
	private bool scopeIndexRandom;

	[Tooltip("All possible Scope Attachments that this Weapon can use!")]
	[SerializeField]
	private ScopeBehaviour[] scopeArray;

	[Tooltip("Selected Muzzle Index.")]
	[SerializeField]
	public int muzzleIndex;

	[Tooltip("Should we pick a random index when starting the game?")]
	[SerializeField]
	private bool muzzleIndexRandom = true;

	[Tooltip("All possible Muzzle Attachments that this Weapon can use!")]
	[SerializeField]
	private MuzzleBehaviour[] muzzleArray;

	[Tooltip("Selected Laser Index.")]
	[SerializeField]
	public int laserIndex = -1;

	[Tooltip("Should we pick a random index when starting the game?")]
	[SerializeField]
	private bool laserIndexRandom = true;

	[Tooltip("All possible Laser Attachments that this Weapon can use!")]
	[SerializeField]
	private LaserBehaviour[] laserArray;

	[Tooltip("Selected Grip Index.")]
	[SerializeField]
	public int gripIndex = -1;

	[Tooltip("Should we pick a random index when starting the game?")]
	[SerializeField]
	private bool gripIndexRandom = true;

	[Tooltip("All possible Grip Attachments that this Weapon can use!")]
	[SerializeField]
	private GripBehaviour[] gripArray;

	[Tooltip("Selected Magazine Index.")]
	[SerializeField]
	private int magazineIndex;

	[Tooltip("Should we pick a random index when starting the game?")]
	[SerializeField]
	private bool magazineIndexRandom = true;

	[Tooltip("All possible Magazine Attachments that this Weapon can use!")]
	[SerializeField]
	private Magazine[] magazineArray;

	[Tooltip("Selected Skin Index.")]
	[SerializeField]
	public int skinIndex;

	private ScopeBehaviour scopeBehaviour;

	private MuzzleBehaviour muzzleBehaviour;

	private LaserBehaviour laserBehaviour;

	private GripBehaviour gripBehaviour;

	private MagazineBehaviour magazineBehaviour;

	private Inventory inventory;

	private Character character;

	private WeaponNetworkSync WeaponNetworkSync;

	private Weapon weapon;

	private List<Renderer> skinRenderers = new List<Renderer>();

	protected override void Awake()
	{
		Restart();
	}

	public override void Restart()
	{
		Apply();
	}

	private void ApplySkin()
	{
		if (character == null)
		{
			character = GetComponentInParent<Character>();
		}
		weapon = GetComponent<Weapon>();
		if (!GameManager.Instance || !weapon)
		{
			return;
		}
		WeaponsSkinsManager component = GameManager.Instance.GetComponent<WeaponsSkinsManager>();
		if ((bool)character && character.GetComponent<CharacterMultiplayer>().IsLocalMainPlayer())
		{
			DataManager component2 = GameManager.Instance.GetComponent<DataManager>();
			skinIndex = component2.GetInt(weapon.GetWeaponName() + "_skin", -1);
			Debug.Log("skinIndex from " + weapon.GetWeaponName() + "_skin =" + skinIndex);
		}
		foreach (Renderer skinRenderer in skinRenderers)
		{
			Material[] materials = skinRenderer.materials;
			for (int i = 0; i < skinRenderer.materials.Length; i++)
			{
				string text = skinRenderer.materials[i].name.ToLower();
				if (!text.Contains("basic") && !text.Contains("carbonfibre_001") && !text.Contains("sight") && !text.Contains("scope") && !text.Contains("laser_beam") && !skinRenderer.gameObject.GetComponent<ParticleSystem>())
				{
					materials[i] = ((skinIndex >= 0 && skinIndex < component.skins.Length) ? component.skins[skinIndex].material : component.defaultSkin);
				}
			}
			skinRenderer.materials = materials;
		}
	}

	protected override void Update()
	{
		if (!Application.isEditor || inventory == null || character == null || weapon == null || !character.GetComponent<CharacterMultiplayer>().isLocal)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.K))
		{
			scopeIndex++;
			if (scopeIndex >= scopeArray.Length)
			{
				scopeIndex = -1;
			}
			UpdateWeapon();
		}
		if (Input.GetKeyDown(KeyCode.P))
		{
			muzzleIndex++;
			if (muzzleIndex >= muzzleArray.Length)
			{
				muzzleIndex = 0;
			}
			UpdateWeapon();
		}
		if (Input.GetKeyDown(KeyCode.O))
		{
			laserIndex++;
			if (laserIndex >= laserArray.Length)
			{
				laserIndex = -1;
			}
			UpdateWeapon();
		}
		if (Input.GetKeyDown(KeyCode.I))
		{
			gripIndex++;
			if (gripIndex >= gripArray.Length)
			{
				gripIndex = -1;
			}
			UpdateWeapon();
		}
		if (Input.GetKeyDown(KeyCode.U))
		{
			skinIndex++;
			if (skinIndex >= inventory.skins.Length)
			{
				skinIndex = 0;
			}
			UpdateWeapon();
		}
	}

	public void UpdateWeapon()
	{
		if ((bool)weapon)
		{
			weapon.UpdateAttachements(restart: false);
		}
		if ((bool)character)
		{
			character.RefreshWeaponSetup();
		}
	}

	public void Apply()
	{
		weapon = GetComponent<Weapon>();
		inventory = GetComponentInParent<Inventory>();
		character = GetComponentInParent<Character>();
		WeaponNetworkSync = GetComponentInParent<WeaponNetworkSync>();
		skinRenderers.Clear();
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer item in componentsInChildren)
		{
			skinRenderers.Add(item);
		}
		if (scopeIndexRandom)
		{
			scopeIndex = Random.Range(scopeIndexFirst, scopeArray.Length);
		}
		if (scopeIndex >= scopeArray.Length)
		{
			scopeIndex = -1;
		}
		scopeBehaviour = scopeArray.SelectAndSetActive(scopeIndex);
		if (scopeBehaviour == null)
		{
			scopeBehaviour = scopeDefaultBehaviour;
			scopeBehaviour.gameObject.SetActive(scopeDefaultShow);
		}
		if (muzzleIndexRandom)
		{
			muzzleIndex = Random.Range(0, muzzleArray.Length);
		}
		if (muzzleIndex >= muzzleArray.Length)
		{
			muzzleIndex = 0;
		}
		muzzleBehaviour = muzzleArray.SelectAndSetActive(muzzleIndex);
		if (laserIndexRandom)
		{
			laserIndex = Random.Range(0, laserArray.Length);
		}
		if (laserIndex >= laserArray.Length)
		{
			laserIndex = -1;
		}
		laserBehaviour = laserArray.SelectAndSetActive(laserIndex);
		if (gripIndexRandom)
		{
			gripIndex = Random.Range(0, gripArray.Length);
		}
		if (gripIndex >= gripArray.Length)
		{
			gripIndex = -1;
		}
		gripBehaviour = gripArray.SelectAndSetActive(gripIndex);
		if (magazineIndexRandom)
		{
			magazineIndex = Random.Range(0, magazineArray.Length);
		}
		magazineBehaviour = magazineArray.SelectAndSetActive(magazineIndex);
		ApplySkin();
		if ((bool)WeaponNetworkSync)
		{
			WeaponNetworkSync.mainPlayerChanged = true;
		}
	}

	public override ScopeBehaviour GetEquippedScope()
	{
		return scopeBehaviour;
	}

	public override ScopeBehaviour GetEquippedScopeDefault()
	{
		return scopeDefaultBehaviour;
	}

	public override MagazineBehaviour GetEquippedMagazine()
	{
		return magazineBehaviour;
	}

	public override MuzzleBehaviour GetEquippedMuzzle()
	{
		return muzzleBehaviour;
	}

	public override LaserBehaviour GetEquippedLaser()
	{
		return laserBehaviour;
	}

	public override GripBehaviour GetEquippedGrip()
	{
		return gripBehaviour;
	}
}
