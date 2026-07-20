using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class ScopeBehaviour : MonoBehaviour
{
	public abstract float GetMultiplierMouseSensitivity();

	public abstract float GetMultiplierSpread();

	public abstract Vector3 GetOffsetAimingLocation();

	public abstract Vector3 GetOffsetAimingRotation();

	public abstract float GetFieldOfViewMultiplierAim();

	public abstract float GetFieldOfViewMultiplierAimWeapon();

	public abstract Sprite GetSprite();

	public abstract float GetSwayMultiplier();

	public abstract void OnAim();

	public abstract void OnAimStop();
}
