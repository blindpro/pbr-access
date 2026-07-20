using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class CharacterBehaviour : MonoBehaviour
{
	public Vector3 lookat_cursor_raycasted;

	public Vector3 lookat_cursor_raycasted_normal;

	public Collider lookat_cursor_raycasted_collider;

	protected virtual void Awake()
	{
	}

	protected virtual void Start()
	{
	}

	protected virtual void Update()
	{
	}

	protected virtual void LateUpdate()
	{
	}

	public abstract int GetShotsFired();

	public abstract bool IsLowered();

	public abstract Camera GetCameraWorld();

	public abstract Camera GetCameraDepth();

	public abstract InventoryBehaviour GetInventory();

	public abstract int GetGrenadesCurrent();

	public abstract int GetGrenadesTotal();

	public abstract bool IsRunning();

	public abstract bool IsHolstered();

	public abstract bool IsCrouching();

	public abstract bool IsReloading();

	public abstract bool IsThrowingGrenade();

	public abstract bool IsMeleeing();

	public abstract bool IsAiming();

	public abstract float GetAimingAlpha();

	public abstract bool IsCursorLocked();

	public abstract void ShowCursor(bool show);

	public abstract bool IsTutorialTextVisible();

	public abstract Vector2 GetInputMovement();

	public abstract Vector2 GetInputLook();

	public abstract AudioClip[] GetAudioClipsGrenadeThrow();

	public abstract AudioClip[] GetAudioClipsMelee();

	public abstract bool IsInspecting();

	public abstract bool IsHoldingButtonFire();

	public abstract Animator GetCharacterAnimator();

	public abstract void EjectCasing();

	public abstract void FillAmmunition(int amount);

	public abstract void Grenade();

	public abstract void SetActiveMagazine(int active);

	public abstract void AnimationEndedBolt();

	public abstract void AnimationEndedReload();

	public abstract void AnimationEndedGrenadeThrow();

	public abstract void AnimationEndedMelee();

	public abstract void AnimationEndedInspect();

	public abstract void AnimationEndedHolster();

	public abstract void SetSlideBack(int back);

	public abstract void SetActiveKnife(int active);

	public abstract void Restart();
}
