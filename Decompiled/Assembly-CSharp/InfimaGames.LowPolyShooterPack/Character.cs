using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

[RequireComponent(typeof(CharacterKinematics))]
public sealed class Character : CharacterBehaviour
{
	public Vector2 mouseAxisLook;

	[Tooltip("The character's LowerWeapon component.")]
	[SerializeField]
	private LowerWeapon lowerWeapon;

	[Tooltip("Determines the index of the weapon to equip when the game starts.")]
	[SerializeField]
	private int weaponIndexEquippedAtStart;

	[Tooltip("Inventory.")]
	[SerializeField]
	private InventoryBehaviour inventory;

	[Tooltip("If true, the character's grenades will never run out.")]
	[SerializeField]
	private bool grenadesUnlimited;

	[Tooltip("Total amount of grenades at start.")]
	[SerializeField]
	private int grenadeTotal = 10;

	[Tooltip("Grenade spawn offset from the character's camera.")]
	[SerializeField]
	private float grenadeSpawnOffset = 1f;

	[Tooltip("Grenade Prefab. Spawned when throwing a grenade.")]
	[SerializeField]
	private GameObject grenadePrefab;

	[Tooltip("Knife GameObject.")]
	[SerializeField]
	private GameObject knife;

	[Tooltip("Normal Camera.")]
	[SerializeField]
	private Camera cameraWorld;

	[Tooltip("Weapon-Only Camera. Depth.")]
	[SerializeField]
	private Camera cameraDepth;

	[Tooltip("Determines how smooth the turning animation is.")]
	[SerializeField]
	private float dampTimeTurning = 0.4f;

	[Tooltip("Determines how smooth the locomotion blendspace is.")]
	[SerializeField]
	private float dampTimeLocomotion = 0.15f;

	[Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
	[SerializeField]
	private float dampTimeAiming = 0.3f;

	[Tooltip("Interpolation speed for the running offsets.")]
	[SerializeField]
	private float runningInterpolationSpeed = 12f;

	[Tooltip("Determines how fast the character's weapons are aimed.")]
	[SerializeField]
	private float aimingSpeedMultiplier = 1f;

	[Tooltip("Character Animator.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("Normal world field of view.")]
	[SerializeField]
	private float fieldOfView = 100f;

	[Tooltip("Multiplier for the field of view while running.")]
	[SerializeField]
	private float fieldOfViewRunningMultiplier = 1.05f;

	[Tooltip("Weapon-specific field of view.")]
	[SerializeField]
	private float fieldOfViewWeapon = 55f;

	[Tooltip("Melee Audio Clips.")]
	[SerializeField]
	private AudioClip[] audioClipsMelee;

	[Tooltip("Grenade Throw Audio Clips.")]
	[SerializeField]
	private AudioClip[] audioClipsGrenadeThrow;

	[Tooltip("If true, the running input has to be held to be active.")]
	[SerializeField]
	private bool holdToRun = true;

	[Tooltip("If true, the aiming input has to be held to be active.")]
	[SerializeField]
	private bool holdToAim = true;

	private bool aiming;

	private bool wasAiming;

	private bool running;

	private bool holstered;

	private float lastShotTime;

	private int layerOverlay;

	private int layerHolster;

	private int layerActions;

	private MovementBehaviour movementBehaviour;

	private WeaponBehaviour equippedWeapon;

	private WeaponAttachmentManagerBehaviour weaponAttachmentManager;

	private ScopeBehaviour equippedWeaponScope;

	private MagazineBehaviour equippedWeaponMagazine;

	private bool reloading;

	private bool inspecting;

	private bool throwingGrenade;

	private bool meleeing;

	private bool holstering;

	private float aimingAlpha;

	private float crouchingAlpha;

	private float runningAlpha;

	private Vector2 axisLook;

	private Vector2 axisMovement;

	private bool bolting;

	private int grenadeCount;

	private bool holdingButtonAim;

	private bool holdingButtonRun;

	private bool holdingButtonFire;

	private bool tutorialTextVisible;

	private bool cursorLocked;

	private int shotsFired;

	private CharacterMultiplayer characterMultiplayer;

	private InputSimulator inputSimulator;

	private ThirdPerson thirdPerson;

	private bool canReduceMag;

	protected override void Awake()
	{
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
		inputSimulator = GetComponent<InputSimulator>();
		thirdPerson = GetComponent<ThirdPerson>();
		cursorLocked = true;
		movementBehaviour = GetComponent<MovementBehaviour>();
		inventory.Init(weaponIndexEquippedAtStart);
		RefreshWeaponSetup();
	}

	protected override void Start()
	{
		grenadeCount = grenadeTotal;
		if (knife != null)
		{
			knife.SetActive(value: false);
		}
		layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
		layerActions = characterAnimator.GetLayerIndex("Layer Actions");
		layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
		Restart();
	}

	public override void Restart()
	{
		grenadeCount = grenadeTotal;
		if (knife != null)
		{
			knife.SetActive(value: false);
		}
		cursorLocked = true;
		GetComponent<CharacterInventory>().Restart();
		inventory.Restart(weaponIndexEquippedAtStart);
		RefreshWeaponSetup();
		inputSimulator.Restart();
		characterMultiplayer.Restart();
		Weapon weapon = GetEquippedWeapon();
		if ((bool)weapon)
		{
			weapon.EmptyAmmunition();
		}
		grenadeCount = 0;
		holdingButtonAim = false;
		holdingButtonFire = false;
		holdingButtonRun = false;
		canReduceMag = false;
	}

	public void AddGrenades(int g)
	{
		grenadeCount += g;
	}

	protected override void Update()
	{
		_ = characterMultiplayer.isLocal;
		if (!characterMultiplayer.isMainPlayer)
		{
			cursorLocked = true;
		}
		aiming = holdingButtonAim && CanAim();
		running = holdingButtonRun && CanRun();
		if (aiming)
		{
			if (!wasAiming)
			{
				equippedWeaponScope.OnAim();
			}
		}
		else if (wasAiming)
		{
			equippedWeaponScope.OnAimStop();
		}
		if (holdingButtonFire)
		{
			if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
			{
				if (Time.time - lastShotTime > 60f / equippedWeapon.GetRateOfFire())
				{
					Fire();
				}
			}
			else
			{
				shotsFired = 0;
			}
		}
		UpdateAnimator();
		aimingAlpha = characterAnimator.GetFloat(AHashes.AimingAlpha);
		crouchingAlpha = Mathf.Lerp(crouchingAlpha, movementBehaviour.IsCrouching() ? 1f : 0f, Time.deltaTime * 12f);
		runningAlpha = Mathf.Lerp(runningAlpha, running ? 1f : 0f, Time.deltaTime * runningInterpolationSpeed);
		float num = Mathf.Lerp(1f, fieldOfViewRunningMultiplier, runningAlpha);
		cameraWorld.fieldOfView = Mathf.Lerp(fieldOfView, fieldOfView * equippedWeapon.GetFieldOfViewMultiplierAim(), aimingAlpha) * num;
		cameraDepth.fieldOfView = Mathf.Lerp(fieldOfViewWeapon, fieldOfViewWeapon * equippedWeapon.GetFieldOfViewMultiplierAimWeapon(), aimingAlpha);
		wasAiming = aiming;
	}

	public Weapon GetEquippedWeapon()
	{
		return (Weapon)equippedWeapon;
	}

	public override Animator GetCharacterAnimator()
	{
		return characterAnimator;
	}

	public override int GetShotsFired()
	{
		return shotsFired;
	}

	public override bool IsLowered()
	{
		if (lowerWeapon == null)
		{
			return false;
		}
		return lowerWeapon.IsLowered();
	}

	public override Camera GetCameraWorld()
	{
		return cameraWorld;
	}

	public override Camera GetCameraDepth()
	{
		return cameraDepth;
	}

	public override InventoryBehaviour GetInventory()
	{
		return inventory;
	}

	public override int GetGrenadesCurrent()
	{
		return grenadeCount;
	}

	public override int GetGrenadesTotal()
	{
		return grenadeTotal;
	}

	public override bool IsRunning()
	{
		return running;
	}

	public override bool IsHolstered()
	{
		return holstered;
	}

	public override bool IsCrouching()
	{
		return movementBehaviour.IsCrouching();
	}

	public override bool IsReloading()
	{
		return reloading;
	}

	public override bool IsThrowingGrenade()
	{
		return throwingGrenade;
	}

	public override bool IsMeleeing()
	{
		return meleeing;
	}

	public override bool IsAiming()
	{
		return aiming;
	}

	public override float GetAimingAlpha()
	{
		return aimingAlpha;
	}

	public override bool IsCursorLocked()
	{
		return cursorLocked;
	}

	public override bool IsTutorialTextVisible()
	{
		return tutorialTextVisible;
	}

	public override Vector2 GetInputMovement()
	{
		if (characterMultiplayer.IsDead())
		{
			return Vector2.zero;
		}
		return axisMovement;
	}

	public override Vector2 GetInputLook()
	{
		return axisLook;
	}

	public override AudioClip[] GetAudioClipsGrenadeThrow()
	{
		return audioClipsGrenadeThrow;
	}

	public override AudioClip[] GetAudioClipsMelee()
	{
		return audioClipsMelee;
	}

	public override bool IsInspecting()
	{
		return inspecting;
	}

	public override bool IsHoldingButtonFire()
	{
		return holdingButtonFire;
	}

	private void UpdateAnimator()
	{
		if (GetComponent<CharacterMultiplayer>().isBot)
		{
			Vector3 world_velocity = GetComponent<NetworkTransformSynch>().world_velocity;
			world_velocity.y = 0f;
			Vector3 vector = base.transform.InverseTransformDirection(world_velocity);
			Vector2 vector2 = new Vector2(vector.x, vector.z);
			axisMovement = Vector2.ClampMagnitude(vector2, 1f);
		}
		if (characterAnimator.GetBool("Reloading") && equippedWeapon.GetAmmunitionTotal() - equippedWeapon.GetAmmunitionCurrent() < 1)
		{
			characterAnimator.SetBool("Reloading", value: false);
			equippedWeapon.GetAnimator().SetBool("Reloading", value: false);
		}
		float value = Mathf.Clamp01(axisMovement.y);
		characterAnimator.SetFloat(AHashes.LeaningForward, value, 0.5f, Time.deltaTime);
		float value2 = Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y));
		characterAnimator.SetFloat(AHashes.Movement, value2, dampTimeLocomotion, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.AimingSpeedMultiplier, aimingSpeedMultiplier);
		characterAnimator.SetFloat(AHashes.Turning, Mathf.Abs(axisLook.x), dampTimeTurning, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.Horizontal, axisMovement.x, dampTimeLocomotion, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.Vertical, axisMovement.y, dampTimeLocomotion, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.AimingAlpha, Convert.ToSingle(aiming), dampTimeAiming, Time.deltaTime);
		characterAnimator.SetFloat("Play Rate Locomotion", movementBehaviour.IsGroundedApproximate() ? 1f : 0f, 0.2f, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.PlayRateLocomotionForward, movementBehaviour.GetMultiplierForward(), 0.2f, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.PlayRateLocomotionSideways, movementBehaviour.GetMultiplierSideways(), 0.2f, Time.deltaTime);
		characterAnimator.SetFloat(AHashes.PlayRateLocomotionBackwards, movementBehaviour.GetMultiplierBackwards(), 0.2f, Time.deltaTime);
		characterAnimator.SetBool(AHashes.Aim, aiming);
		characterAnimator.SetBool(AHashes.Running, running);
		characterAnimator.SetBool(AHashes.Crouching, movementBehaviour.IsCrouching());
	}

	private void Inspect()
	{
		inspecting = true;
		characterAnimator.CrossFade("Inspect", 0f, layerActions, 0f);
	}

	private void Fire()
	{
		canReduceMag = true;
		shotsFired++;
		lastShotTime = Time.time;
		equippedWeapon.Fire(aiming ? equippedWeaponScope.GetMultiplierSpread() : 1f);
		characterAnimator.CrossFade("Fire", 0.05f, layerOverlay, 0f);
		if (equippedWeapon.IsBoltAction() && equippedWeapon.HasAmmunition())
		{
			UpdateBolt(value: true);
		}
		if (!equippedWeapon.HasAmmunition() && equippedWeapon.GetAutomaticallyReloadOnEmpty())
		{
			StartCoroutine("TryReloadAutomatic");
		}
	}

	private void PlayReloadAnimation()
	{
		string stateName = (equippedWeapon.HasCycledReload() ? "Reload Open" : (equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty"));
		characterAnimator.Play(stateName, layerActions, 0f);
		characterAnimator.SetBool(AHashes.Reloading, reloading = true);
		equippedWeapon.Reload();
	}

	private IEnumerator TryReloadAutomatic()
	{
		yield return new WaitForSeconds(equippedWeapon.GetAutomaticallyReloadOnEmptyDelay());
		if (CanPlayAnimationReload())
		{
			PlayReloadAnimation();
		}
	}

	private IEnumerator Equip(int index = 0)
	{
		if (!holstered)
		{
			Character character = this;
			Character character2 = this;
			bool flag = true;
			character2.holstering = true;
			character.SetHolstered(flag);
			yield return new WaitUntil(() => !holstering);
		}
		SetHolstered(value: false);
		characterAnimator.Play("Unholster", layerHolster, 0f);
		_ = (Weapon)inventory.Equip(index);
		RefreshWeaponSetup();
	}

	public void RefreshWeaponSetup()
	{
		if (!((equippedWeapon = inventory.GetEquipped()) == null))
		{
			characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();
			weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
			if (!(weaponAttachmentManager == null))
			{
				equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
				equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
			}
		}
	}

	private void FireEmpty()
	{
		OnTryPlayReloadDirect();
		lastShotTime = Time.time;
		characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0f);
	}

	private void UpdateCursorState()
	{
		if (characterMultiplayer.isMainPlayer)
		{
			Debug.Log(cursorLocked);
			Cursor.visible = !cursorLocked;
			Cursor.lockState = (cursorLocked ? CursorLockMode.Locked : CursorLockMode.None);
		}
	}

	private void PlayGrenadeThrow()
	{
		throwingGrenade = true;
		characterAnimator.CrossFade("Grenade Throw", 0.15f, characterAnimator.GetLayerIndex("Layer Actions Arm Left"), 0f);
		characterAnimator.CrossFade("Grenade Throw", 0.05f, characterAnimator.GetLayerIndex("Layer Actions Arm Right"), 0f);
	}

	private void PlayMelee()
	{
		meleeing = true;
		characterAnimator.CrossFade("Knife Attack", 0.05f, characterAnimator.GetLayerIndex("Layer Actions Arm Left"), 0f);
		characterAnimator.CrossFade("Knife Attack", 0.05f, characterAnimator.GetLayerIndex("Layer Actions Arm Right"), 0f);
	}

	private void UpdateBolt(bool value)
	{
		characterAnimator.SetBool(AHashes.Bolt, bolting = value);
	}

	private void SetHolstered(bool value = true)
	{
		holstered = value;
		characterAnimator.SetBool("Holstered", holstered);
	}

	private bool CanPlayAnimationFire()
	{
		if (holstered || holstering)
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || bolting)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		return true;
	}

	private bool CanPlayAnimationReload()
	{
		if (reloading)
		{
			return false;
		}
		if (meleeing)
		{
			return false;
		}
		if (bolting)
		{
			return false;
		}
		if (throwingGrenade)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		if (!equippedWeapon.CanReloadWhenFull() && equippedWeapon.IsFull())
		{
			return false;
		}
		if (((Weapon)equippedWeapon).GetCurrentMags() <= 0)
		{
			return false;
		}
		return true;
	}

	private bool CanPlayAnimationGrenadeThrow()
	{
		if (holstered || holstering)
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || bolting)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		if (!grenadesUnlimited && grenadeCount == 0)
		{
			return false;
		}
		return true;
	}

	private bool CanPlayAnimationMelee()
	{
		if (holstered || holstering)
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || bolting)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		return true;
	}

	private bool CanPlayAnimationHolster()
	{
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || bolting)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		return true;
	}

	private bool CanChangeWeapon()
	{
		if (holstering)
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || bolting)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		return true;
	}

	private bool CanPlayAnimationInspect()
	{
		if (holstered || holstering)
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || bolting)
		{
			return false;
		}
		if (inspecting)
		{
			return false;
		}
		return true;
	}

	private bool CanAim()
	{
		if (holstered || inspecting)
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if ((!equippedWeapon.CanReloadAimed() && reloading) || holstering)
		{
			return false;
		}
		return true;
	}

	private bool CanRun()
	{
		if (inspecting || bolting)
		{
			return false;
		}
		if (movementBehaviour.IsCrouching())
		{
			return false;
		}
		if (meleeing || throwingGrenade)
		{
			return false;
		}
		if (reloading || aiming)
		{
			return false;
		}
		if (holdingButtonFire && equippedWeapon.HasAmmunition())
		{
			return false;
		}
		if ((axisMovement.y <= 0f || Math.Abs(Mathf.Abs(axisMovement.x) - 1f) < 0.01f) && characterMultiplayer.isLocal)
		{
			return false;
		}
		return true;
	}

	public void OnTryFire(InputAction.CallbackContext context)
	{
		if (!characterMultiplayer.isMainPlayer || !cursorLocked || !thirdPerson.isActive)
		{
			return;
		}
		switch (context.phase)
		{
		case InputActionPhase.Started:
			holdingButtonFire = true;
			shotsFired = 0;
			break;
		case InputActionPhase.Performed:
			if (!CanPlayAnimationFire())
			{
				break;
			}
			if (equippedWeapon.HasAmmunition())
			{
				if (equippedWeapon.IsAutomatic())
				{
					shotsFired = 0;
				}
				else if (Time.time - lastShotTime > 60f / equippedWeapon.GetRateOfFire())
				{
					Fire();
				}
			}
			else
			{
				FireEmpty();
			}
			break;
		case InputActionPhase.Canceled:
			holdingButtonFire = false;
			shotsFired = 0;
			break;
		}
	}

	public void OnTryFireSimulator(InputSimulator.Action context)
	{
		if (!cursorLocked || !thirdPerson.isActive || context == null)
		{
			return;
		}
		switch (context.phase)
		{
		case InputActionPhase.Started:
			holdingButtonFire = true;
			shotsFired = 0;
			break;
		case InputActionPhase.Performed:
			if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition())
			{
				if (equippedWeapon.IsAutomatic())
				{
					shotsFired = 0;
				}
				else if (Time.time - lastShotTime > 60f / equippedWeapon.GetRateOfFire())
				{
					Fire();
				}
			}
			break;
		case InputActionPhase.Canceled:
			holdingButtonFire = false;
			shotsFired = 0;
			break;
		}
	}

	public void OnTryPlayReloadDirect()
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive)
		{
			if ((bool)GameManager.Instance)
			{
				GameManager.Instance.GetComponent<PickupsManager>().UseAmmoAuto();
			}
			if (CanPlayAnimationReload())
			{
				PlayReloadAnimation();
				inputSimulator.RPC_Action(2);
			}
		}
	}

	public void OnTryPlayReload(InputAction.CallbackContext context)
	{
		if (!characterMultiplayer.isMainPlayer || !cursorLocked || !thirdPerson.isActive)
		{
			return;
		}
		if ((bool)GameManager.Instance)
		{
			PickupsManager component = GameManager.Instance.GetComponent<PickupsManager>();
			if (!Application.isEditor)
			{
				component.UseAmmoAuto();
			}
		}
		if (CanPlayAnimationReload() && context.phase == InputActionPhase.Performed)
		{
			PlayReloadAnimation();
			inputSimulator.RPC_Action(2);
		}
	}

	public void OnTryPlayReloadSimulator(InputSimulator.Action context)
	{
		if (cursorLocked && thirdPerson.isActive && CanPlayAnimationReload() && context != null && context.phase == InputActionPhase.Performed)
		{
			PlayReloadAnimation();
		}
	}

	public void OnTryInspect(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && CanPlayAnimationInspect() && context.phase == InputActionPhase.Performed)
		{
			Inspect();
			inputSimulator.RPC_Action(6);
		}
	}

	public void OnTryInspectSimulator(InputSimulator.Action context)
	{
		if (cursorLocked && thirdPerson.isActive && CanPlayAnimationInspect() && context != null && context.phase == InputActionPhase.Performed)
		{
			Inspect();
		}
	}

	public void OnTryAiming(InputAction.CallbackContext context)
	{
		if (!characterMultiplayer.isMainPlayer || !cursorLocked || !thirdPerson.isActive)
		{
			return;
		}
		switch (context.phase)
		{
		case InputActionPhase.Started:
			if (holdToAim)
			{
				holdingButtonAim = true;
			}
			break;
		case InputActionPhase.Performed:
			if (!holdToAim)
			{
				holdingButtonAim = !holdingButtonAim;
			}
			break;
		case InputActionPhase.Canceled:
			if (holdToAim)
			{
				holdingButtonAim = false;
			}
			break;
		}
	}

	public void OnTryAimingSimulator(InputSimulator.Action context)
	{
		if (!cursorLocked || !thirdPerson.isActive)
		{
			return;
		}
		switch (context.phase)
		{
		case InputActionPhase.Started:
			if (holdToAim)
			{
				holdingButtonAim = true;
			}
			break;
		case InputActionPhase.Performed:
			if (!holdToAim)
			{
				holdingButtonAim = !holdingButtonAim;
			}
			break;
		case InputActionPhase.Canceled:
			if (holdToAim)
			{
				holdingButtonAim = false;
			}
			break;
		}
	}

	public void OnTryHolster(InputAction.CallbackContext context)
	{
	}

	public void OnTryHolsterSimulator(InputSimulator.Action context)
	{
	}

	public void OnTryThrowGrenade(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed && CanPlayAnimationGrenadeThrow())
		{
			PlayGrenadeThrow();
			inputSimulator.RPC_Action(3);
		}
	}

	public void OnTryThrowGrenadeSimulator(InputSimulator.Action context)
	{
		if (cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed && CanPlayAnimationGrenadeThrow())
		{
			PlayGrenadeThrow();
		}
	}

	public void OnTryMelee(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed && CanPlayAnimationMelee())
		{
			PlayMelee();
			inputSimulator.RPC_Action(4);
		}
	}

	public void OnTryMeleeSimulator(InputSimulator.Action context)
	{
		if (cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed && CanPlayAnimationMelee())
		{
			PlayMelee();
		}
	}

	public void OnTryRun(InputAction.CallbackContext context)
	{
		if (!characterMultiplayer.isMainPlayer || !cursorLocked)
		{
			return;
		}
		switch (context.phase)
		{
		case InputActionPhase.Performed:
			if (!holdToRun)
			{
				holdingButtonRun = !holdingButtonRun;
			}
			break;
		case InputActionPhase.Started:
			if (holdToRun)
			{
				holdingButtonRun = true;
			}
			break;
		case InputActionPhase.Canceled:
			if (holdToRun)
			{
				holdingButtonRun = false;
			}
			break;
		}
	}

	public void OnTryRunSimulator(InputSimulator.Action context)
	{
		if (!cursorLocked || !thirdPerson.isActive)
		{
			return;
		}
		switch (context.phase)
		{
		case InputActionPhase.Performed:
			if (!holdToRun)
			{
				holdingButtonRun = !holdingButtonRun;
			}
			break;
		case InputActionPhase.Started:
			if (holdToRun)
			{
				holdingButtonRun = true;
			}
			break;
		case InputActionPhase.Canceled:
			if (holdToRun)
			{
				holdingButtonRun = false;
			}
			break;
		}
	}

	public void OnTryJump(InputAction.CallbackContext context)
	{
		if (!characterMultiplayer.isMainPlayer || !cursorLocked || context.phase != InputActionPhase.Performed)
		{
			return;
		}
		CharacterParachute component = GetComponent<CharacterParachute>();
		if (component.isParachuting)
		{
			if (component.isOnAirplane)
			{
				component.JumpFromPlane();
			}
			else
			{
				component.OpenParachute();
			}
		}
		else if (thirdPerson.isActive)
		{
			movementBehaviour.Jump();
		}
	}

	public void OnTryJumpSimulator(InputSimulator.Action context)
	{
		if (!cursorLocked || context.phase != InputActionPhase.Performed)
		{
			return;
		}
		CharacterParachute component = GetComponent<CharacterParachute>();
		if (component.isParachuting)
		{
			if (component.isOnAirplane)
			{
				component.JumpFromPlane();
			}
			else
			{
				component.OpenParachute();
			}
		}
		else if (thirdPerson.isActive)
		{
			movementBehaviour.Jump();
		}
	}

	public void OnTryInventoryNext(InputAction.CallbackContext context)
	{
		if (Application.isEditor && characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && !(inventory == null) && context.phase == InputActionPhase.Performed)
		{
			int num = (((context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1f) > 0f) ? inventory.GetNextIndex() : inventory.GetLastIndex());
			int equippedIndex = inventory.GetEquippedIndex();
			if (CanChangeWeapon() && equippedIndex != num)
			{
				StartCoroutine("Equip", num);
				inputSimulator.RPC_Action(5);
			}
		}
	}

	public void OnTryInventoryNextSimulator(InputSimulator.Action context)
	{
		if (Application.isEditor && cursorLocked && thirdPerson.isActive && !(inventory == null) && context != null && context.phase == InputActionPhase.Performed)
		{
			int nextIndex = inventory.GetNextIndex();
			int equippedIndex = inventory.GetEquippedIndex();
			if (CanChangeWeapon() && equippedIndex != nextIndex)
			{
				StartCoroutine("Equip", nextIndex);
			}
		}
	}

	public void OnSetInventoryWeapon(int weaponId, bool cursorLockedInclude = false)
	{
		if ((cursorLocked || cursorLockedInclude) && thirdPerson.isActive && !(inventory == null))
		{
			int equippedIndex = inventory.GetEquippedIndex();
			if (CanChangeWeapon() && equippedIndex != weaponId)
			{
				StartCoroutine("Equip", weaponId);
			}
		}
	}

	public void OnSetInventoryWeapon(string weaponName, bool cursorLockedInclude = false)
	{
		if (!(inventory == null))
		{
			int weaponId = ((Inventory)inventory).GetWeaponId(weaponName);
			if (weaponId >= 0)
			{
				OnSetInventoryWeapon(weaponId, cursorLockedInclude);
			}
		}
	}

	public void OnLockCursor(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && context.phase == InputActionPhase.Performed)
		{
			cursorLocked = !cursorLocked;
			UpdateCursorState();
		}
	}

	public override void ShowCursor(bool show)
	{
		cursorLocked = !show;
		UpdateCursorState();
	}

	public void OnMove(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer)
		{
			axisMovement = ((cursorLocked && thirdPerson.isActive) ? context.ReadValue<Vector2>() : Vector2.zero);
		}
	}

	public void OnMoveSimulator(Vector2 _axisMovement)
	{
		if (!characterMultiplayer.isMainPlayer)
		{
			axisMovement = ((cursorLocked && thirdPerson.isActive) ? _axisMovement : Vector2.zero);
		}
	}

	public void OnLook(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer)
		{
			mouseAxisLook = context.ReadValue<Vector2>();
			axisLook = ((cursorLocked && thirdPerson.isActive) ? context.ReadValue<Vector2>() : Vector2.zero);
			if (!(equippedWeapon == null) && !(equippedWeaponScope == null))
			{
				axisLook *= (aiming ? equippedWeaponScope.GetMultiplierMouseSensitivity() : 1f);
			}
		}
	}

	public void OnLookSimulator(Vector2 _axisLook)
	{
		if (!characterMultiplayer.isMainPlayer)
		{
			axisLook = ((cursorLocked && thirdPerson.isActive) ? _axisLook : Vector2.zero);
			if (!(equippedWeapon == null) && !(equippedWeaponScope == null))
			{
				axisLook *= (aiming ? equippedWeaponScope.GetMultiplierMouseSensitivity() : 1f);
			}
		}
	}

	public void OnUpdateTutorial(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer)
		{
			tutorialTextVisible = context.phase switch
			{
				InputActionPhase.Started => true, 
				InputActionPhase.Canceled => false, 
				_ => tutorialTextVisible, 
			};
		}
	}

	public void OnTryShowMap(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed)
		{
			Debug.Log("Show Hide Map");
		}
	}

	public void OnTryShowInventory(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed)
		{
			Debug.Log("Show Hide Inventory");
		}
	}

	public void OnTrySelectWeapon1(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed)
		{
			Debug.Log("Select weapon 1");
			GetComponent<CharacterInventory>().SetCurrentWeapon(0);
		}
	}

	public void OnTrySelectWeapon2(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed)
		{
			Debug.Log("Select weapon 2");
			GetComponent<CharacterInventory>().SetCurrentWeapon(1);
		}
	}

	public void OnTryInteract(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed)
		{
			Debug.Log("Interact");
		}
	}

	public void OnTryToggleCameraMode(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && cursorLocked && thirdPerson.isActive && context.phase == InputActionPhase.Performed)
		{
			GetComponent<ThirdPerson>().ToggleCameraMode();
		}
	}

	public override void EjectCasing()
	{
		if (equippedWeapon != null)
		{
			equippedWeapon.EjectCasing();
		}
	}

	public override void FillAmmunition(int amount)
	{
		if (equippedWeapon != null)
		{
			equippedWeapon.FillAmmunition(amount);
		}
	}

	public override void Grenade()
	{
		if (!(grenadePrefab == null) && !(cameraWorld == null))
		{
			if (!grenadesUnlimited)
			{
				grenadeCount--;
			}
			Transform transform = cameraWorld.transform;
			Vector3 position = transform.position;
			position += transform.forward * grenadeSpawnOffset;
			GameObject gameObject = UnityEngine.Object.Instantiate(grenadePrefab, position, transform.rotation);
			if ((bool)gameObject && (bool)gameObject.GetComponent<GrenadeScript>())
			{
				gameObject.GetComponent<GrenadeScript>().character = this;
			}
		}
	}

	public override void SetActiveMagazine(int active)
	{
		equippedWeaponMagazine.gameObject.SetActive(active != 0);
	}

	public override void AnimationEndedBolt()
	{
		Debug.Log("AnimationEndedBolt");
		UpdateBolt(value: false);
	}

	public override void AnimationEndedReload()
	{
		reloading = false;
		Weapon weapon = GetEquippedWeapon();
		if ((bool)weapon && (bool)characterMultiplayer && characterMultiplayer.IsLocalMainPlayer() && canReduceMag)
		{
			weapon.ReloadReduceOneMag();
			canReduceMag = false;
		}
		Debug.Log("AnimationEndedReload");
	}

	public override void AnimationEndedGrenadeThrow()
	{
		throwingGrenade = false;
	}

	public override void AnimationEndedMelee()
	{
		meleeing = false;
	}

	public override void AnimationEndedInspect()
	{
		inspecting = false;
	}

	public override void AnimationEndedHolster()
	{
		holstering = false;
	}

	public override void SetSlideBack(int back)
	{
		if (equippedWeapon != null)
		{
			equippedWeapon.SetSlideBack(back);
		}
	}

	public override void SetActiveKnife(int active)
	{
		knife.SetActive(active != 0);
	}

	public void OnDead()
	{
		holdingButtonAim = false;
		holdingButtonFire = false;
		holdingButtonRun = false;
	}
}
