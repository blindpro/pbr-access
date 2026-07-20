using InfimaGames.LowPolyShooterPack.Legacy;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Weapon : WeaponBehaviour
{
	[Tooltip("Weapon Damage.")]
	public byte weaponDamage = 64;

	[Tooltip("Weapon Name. Currently not used for anything, but in the future, we will use this for pickups!")]
	[SerializeField]
	private string weaponName;

	[Tooltip("How much the character's movement speed is multiplied by when wielding this weapon.")]
	[SerializeField]
	private float multiplierMovementSpeed = 1f;

	[Tooltip("Is this weapon automatic? If yes, then holding down the firing button will continuously fire.")]
	[SerializeField]
	private bool automatic;

	[Tooltip("Is this weapon bolt-action? If yes, then a bolt-action animation will play after every shot.")]
	[SerializeField]
	private bool boltAction;

	[Tooltip("Amount of shots fired at once. Helpful for things like shotguns, where there are multiple projectiles fired at once.")]
	[SerializeField]
	private int shotCount = 1;

	[Tooltip("How far the weapon can fire from the center of the screen.")]
	[SerializeField]
	private float spread = 0.25f;

	[Tooltip("How fast the projectiles are.")]
	[SerializeField]
	private float projectileImpulse = 400f;

	[Tooltip("Amount of shots this weapon can shoot in a minute. It determines how fast the weapon shoots.")]
	[SerializeField]
	private int roundsPerMinutes = 200;

	[Tooltip("Determines if this weapon reloads in cycles, meaning that it inserts one bullet at a time, or not.")]
	[SerializeField]
	private bool cycledReload;

	[Tooltip("Determines if the player can reload this weapon when it is full of ammunition.")]
	[SerializeField]
	private bool canReloadWhenFull = true;

	[Tooltip("Should this weapon be reloaded automatically after firing its last shot?")]
	[SerializeField]
	private bool automaticReloadOnEmpty;

	[Tooltip("Time after the last shot at which a reload will automatically start.")]
	[SerializeField]
	private float automaticReloadOnEmptyDelay = 0.25f;

	[Tooltip("Transform that represents the weapon's ejection port, meaning the part of the weapon that casings shoot from.")]
	[SerializeField]
	private Transform socketEjection;

	[Tooltip("Settings this to false will stop the weapon from being reloaded while the character is aiming it.")]
	[SerializeField]
	private bool canReloadAimed = true;

	[Tooltip("Casing Prefab.")]
	[SerializeField]
	private GameObject prefabCasing;

	[Tooltip("Projectile Prefab. This is the prefab spawned when the weapon shoots.")]
	[SerializeField]
	private GameObject prefabProjectile;

	[Tooltip("The AnimatorController a player character needs to use while wielding this weapon.")]
	[SerializeField]
	public RuntimeAnimatorController controller;

	[Tooltip("Weapon Body Texture.")]
	[SerializeField]
	private Sprite spriteBody;

	[Tooltip("Holster Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipHolster;

	[Tooltip("Unholster Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipUnholster;

	[Tooltip("Reload Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipReload;

	[Tooltip("Reload Empty Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipReloadEmpty;

	[Tooltip("Reload Open Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipReloadOpen;

	[Tooltip("Reload Insert Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipReloadInsert;

	[Tooltip("Reload Close Audio Clip.")]
	[SerializeField]
	private AudioClip audioClipReloadClose;

	[Tooltip("AudioClip played when this weapon is fired without any ammunition.")]
	[SerializeField]
	private AudioClip audioClipFireEmpty;

	[Tooltip("")]
	[SerializeField]
	private AudioClip audioClipBoltAction;

	private Animator animator;

	private WeaponAttachmentManagerBehaviour attachmentManager;

	private int ammunitionCurrent;

	private ScopeBehaviour scopeBehaviour;

	private MagazineBehaviour magazineBehaviour;

	private MuzzleBehaviour muzzleBehaviour;

	private LaserBehaviour laserBehaviour;

	private GripBehaviour gripBehaviour;

	private IGameModeService gameModeService;

	private CharacterBehaviour characterBehaviour;

	private CharacterMultiplayer characterMultiplayer;

	private Transform playerCamera;

	private bool useBulletProjectile;

	private ParticleSystem casingParticle;

	public Sprite weaponIcon;

	private PickupsManager pickupsManager;

	private void Init()
	{
		animator = GetComponent<Animator>();
		attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();
		if ((bool)attachmentManager)
		{
			attachmentManager.Restart();
		}
		else
		{
			Debug.LogWarning("Awake attachmentManager null " + base.gameObject.name);
		}
		gameModeService = ServiceLocator.Current.Get<IGameModeService>();
		characterBehaviour = GetComponentInParent<Character>();
		if ((bool)characterBehaviour)
		{
			characterMultiplayer = characterBehaviour.GetComponent<CharacterMultiplayer>();
		}
		playerCamera = characterBehaviour?.GetCameraWorld()?.transform;
		useBulletProjectile = (bool)prefabProjectile && (bool)prefabProjectile.GetComponent<Projectile>() && prefabProjectile.GetComponent<Projectile>().isBullet;
		if (!(casingParticle == null) || !(prefabCasing != null) || !(socketEjection != null))
		{
			return;
		}
		GameObject gameObject = Object.Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation, socketEjection);
		if ((bool)gameObject)
		{
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = Vector3.one;
			casingParticle = gameObject.GetComponentInChildren<ParticleSystem>();
			if ((bool)casingParticle)
			{
				ParticleSystem.MainModule main = casingParticle.main;
				main.loop = false;
				casingParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}
		}
	}

	protected override void Awake()
	{
		Init();
	}

	protected override void Start()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		if ((bool)pickupsManager)
		{
			PickupsManager.Item item = pickupsManager.GetItem(weaponName);
			if (item != null)
			{
				weaponIcon = item.image;
			}
		}
		Restart();
	}

	public override void Restart()
	{
		UpdateAttachements(restart: true);
		if ((bool)GetMagazine())
		{
			GetMagazine().Restart();
		}
		ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
	}

	public Magazine GetMagazine()
	{
		if (magazineBehaviour == null)
		{
			return null;
		}
		return (Magazine)magazineBehaviour;
	}

	public void EmptyAmmunition()
	{
		Magazine magazine = GetMagazine();
		if ((bool)magazine)
		{
			ammunitionCurrent = 0;
			magazine.SetMags(0);
		}
	}

	public bool HasGrip()
	{
		WeaponAttachmentManager weaponAttachmentManager = (WeaponAttachmentManager)attachmentManager;
		if ((bool)weaponAttachmentManager)
		{
			return weaponAttachmentManager.gripIndex != -1;
		}
		return false;
	}

	public void AddMags(int mags)
	{
		Magazine magazine = GetMagazine();
		if ((bool)magazine)
		{
			magazine.SetMags(magazine.GetCurrentMags() + mags);
		}
	}

	public int GetCurrentMags()
	{
		Magazine magazine = GetMagazine();
		if ((bool)magazine)
		{
			return magazine.GetCurrentMags();
		}
		return 0;
	}

	public override float GetFieldOfViewMultiplierAim()
	{
		if (scopeBehaviour != null)
		{
			return scopeBehaviour.GetFieldOfViewMultiplierAim();
		}
		Debug.LogError("Weapon has no scope equipped!");
		return 1f;
	}

	public override float GetFieldOfViewMultiplierAimWeapon()
	{
		if (scopeBehaviour != null)
		{
			return scopeBehaviour.GetFieldOfViewMultiplierAimWeapon();
		}
		Debug.LogError("Weapon has no scope equipped!");
		return 1f;
	}

	public override Animator GetAnimator()
	{
		return animator;
	}

	public override bool CanReloadAimed()
	{
		return canReloadAimed;
	}

	public override Sprite GetSpriteBody()
	{
		return spriteBody;
	}

	public override float GetMultiplierMovementSpeed()
	{
		return multiplierMovementSpeed;
	}

	public override AudioClip GetAudioClipHolster()
	{
		return audioClipHolster;
	}

	public override AudioClip GetAudioClipUnholster()
	{
		return audioClipUnholster;
	}

	public override AudioClip GetAudioClipReload()
	{
		return audioClipReload;
	}

	public override AudioClip GetAudioClipReloadEmpty()
	{
		return audioClipReloadEmpty;
	}

	public override AudioClip GetAudioClipReloadOpen()
	{
		return audioClipReloadOpen;
	}

	public override AudioClip GetAudioClipReloadInsert()
	{
		return audioClipReloadInsert;
	}

	public override AudioClip GetAudioClipReloadClose()
	{
		return audioClipReloadClose;
	}

	public override AudioClip GetAudioClipFireEmpty()
	{
		return audioClipFireEmpty;
	}

	public override AudioClip GetAudioClipBoltAction()
	{
		return audioClipBoltAction;
	}

	public override AudioClip GetAudioClipFire()
	{
		return muzzleBehaviour.GetAudioClipFire();
	}

	public override int GetAmmunitionCurrent()
	{
		return ammunitionCurrent;
	}

	public override int GetAmmunitionTotal()
	{
		return magazineBehaviour.GetAmmunitionTotal();
	}

	public override bool HasCycledReload()
	{
		return cycledReload;
	}

	public override bool IsAutomatic()
	{
		return automatic;
	}

	public override bool IsBoltAction()
	{
		return boltAction;
	}

	public override bool GetAutomaticallyReloadOnEmpty()
	{
		return automaticReloadOnEmpty;
	}

	public override float GetAutomaticallyReloadOnEmptyDelay()
	{
		return automaticReloadOnEmptyDelay;
	}

	public override bool CanReloadWhenFull()
	{
		return canReloadWhenFull;
	}

	public override float GetRateOfFire()
	{
		return roundsPerMinutes;
	}

	public override bool IsFull()
	{
		return ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
	}

	public override bool HasAmmunition()
	{
		return ammunitionCurrent > 0;
	}

	public override RuntimeAnimatorController GetAnimatorController()
	{
		return controller;
	}

	public override WeaponAttachmentManagerBehaviour GetAttachmentManager()
	{
		return attachmentManager;
	}

	public string GetWeaponName()
	{
		return weaponName;
	}

	public override void Reload()
	{
		animator.SetBool("Reloading", value: true);
		ServiceLocator.Current.Get<IAudioManagerService>().PlayOneShot3D(HasAmmunition() ? audioClipReload : audioClipReloadEmpty, new AudioSettings(1f, 0f, automaticCleanup: false), base.gameObject.transform);
		animator.Play(cycledReload ? "Reload Open" : (HasAmmunition() ? "Reload" : "Reload Empty"), 0, 0f);
	}

	public override void Fire(float spreadMultiplier = 1f)
	{
		if (muzzleBehaviour == null || playerCamera == null || characterMultiplayer == null)
		{
			return;
		}
		animator.Play("Fire", 0, 0f);
		ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());
		if (ammunitionCurrent == 0)
		{
			SetSlideBack(1);
		}
		muzzleBehaviour.Effect();
		if ((bool)characterBehaviour && (bool)characterBehaviour.GetInventory())
		{
			_ = characterBehaviour.GetInventory().tps_mode;
		}
		Vector3 vector = characterBehaviour.gameObject.GetComponent<NetworkTransformSynch>().world_velocity * Time.deltaTime * 2f;
		vector *= 0.5f;
		vector.y = 0f;
		for (int i = 0; i < shotCount; i++)
		{
			Vector3 direction = Random.insideUnitSphere * (spread * spreadMultiplier);
			direction.z = 0f;
			direction = playerCamera.TransformDirection(direction);
			GameObject gameObject = null;
			gameObject = ((!useBulletProjectile) ? Object.Instantiate(prefabProjectile, muzzleBehaviour.transform.position + vector, Quaternion.Euler(playerCamera.eulerAngles + direction)) : PoolsManager.Instance.bullets.CreatePrefab(muzzleBehaviour.transform.position + vector, Quaternion.Euler(playerCamera.eulerAngles + direction)));
			if ((bool)gameObject && (bool)gameObject.GetComponent<Projectile>())
			{
				gameObject.GetComponent<Projectile>().character = characterBehaviour;
			}
			if ((bool)gameObject && (bool)gameObject.GetComponent<ProjectileScript>())
			{
				gameObject.GetComponent<ProjectileScript>().character = (Character)characterBehaviour;
			}
			if ((bool)gameObject && !useBulletProjectile)
			{
				gameObject.GetComponent<Rigidbody>().velocity = gameObject.transform.forward * projectileImpulse;
			}
			if ((bool)gameObject && useBulletProjectile)
			{
				characterBehaviour.GetComponent<ThirdPerson>().ComputeLookAtRaycast();
				Projectile component = gameObject.GetComponent<Projectile>();
				component.bulletStartPoint = muzzleBehaviour.transform.position + vector;
				component.bulletEndPoint = characterBehaviour.lookat_cursor_raycasted;
				component.bulletEndNormal = characterBehaviour.lookat_cursor_raycasted_normal;
				component.bulletEndCollider = characterBehaviour.lookat_cursor_raycasted_collider;
				component.isBulletImpacted = false;
				component.GetComponent<TrailRenderer>().emitting = !characterMultiplayer.IsLocalMainPlayer() && !characterMultiplayer.isSpectating;
			}
		}
	}

	public override void FillAmmunition(int amount)
	{
		ammunitionCurrent = ((amount != 0) ? Mathf.Clamp(ammunitionCurrent + amount, 0, GetAmmunitionTotal()) : magazineBehaviour.GetAmmunitionTotal());
	}

	public void ReloadReduceOneMag()
	{
		if (!characterBehaviour || !characterBehaviour.GetComponent<CharacterMultiplayer>().isBot)
		{
			Magazine magazine = GetMagazine();
			if ((bool)magazine)
			{
				magazine.ReduceMag();
			}
		}
	}

	public override void SetSlideBack(int back)
	{
		animator.SetBool("Slide Back", back != 0);
	}

	public override void EjectCasing()
	{
		if (prefabCasing != null && socketEjection != null && (bool)casingParticle)
		{
			casingParticle.Emit(1);
		}
	}

	public override void UpdateAttachements(bool restart)
	{
		if (attachmentManager == null)
		{
			Init();
		}
		else if (restart)
		{
			attachmentManager.Restart();
		}
		else
		{
			((WeaponAttachmentManager)attachmentManager).Apply();
		}
		scopeBehaviour = attachmentManager.GetEquippedScope();
		magazineBehaviour = attachmentManager.GetEquippedMagazine();
		muzzleBehaviour = attachmentManager.GetEquippedMuzzle();
		laserBehaviour = attachmentManager.GetEquippedLaser();
		gripBehaviour = attachmentManager.GetEquippedGrip();
		if ((bool)characterBehaviour)
		{
			characterBehaviour.GetComponent<CharacterMultiplayer>().SetRecoil(gripBehaviour != null, this);
		}
	}
}
