using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Scope : ScopeBehaviour
{
	[Tooltip("Amount to multiply the mouse sensitivity by while aiming through this scope.")]
	[SerializeField]
	private float multiplierMouseSensitivity = 0.8f;

	[Tooltip("Value multiplied by the weapon's spread while aiming through this scope.")]
	[SerializeField]
	private float multiplierSpread = 0.1f;

	[Tooltip("Interface Sprite.")]
	[SerializeField]
	private Sprite sprite;

	[Tooltip("The value to multiply the weapon sway by while aiming through this scope.")]
	[SerializeField]
	private float swayMultiplier = 1f;

	[Tooltip("Weapon bone location offset while aiming.")]
	[SerializeField]
	private Vector3 offsetAimingLocation;

	[Tooltip("Weapon bone rotation offset while aiming.")]
	[SerializeField]
	private Vector3 offsetAimingRotation;

	[Tooltip("Field Of View Multiplier Aim.")]
	[SerializeField]
	private float fieldOfViewMultiplierAim = 0.9f;

	[Tooltip("Field Of View Multiplier Aim Weapon.")]
	[SerializeField]
	private float fieldOfViewMultiplierAimWeapon = 0.7f;

	[Tooltip("The index of the scope material that gets hidden when we don't aim.")]
	[SerializeField]
	private int materialIndex = 3;

	[Tooltip("Material to block the scope while not aiming through it.")]
	[SerializeField]
	private Material materialHidden;

	private MeshRenderer meshRenderer;

	private Material materialDefault;

	private CharacterMultiplayer characterMultiplayer;

	private Camera renderCam;

	private void Awake()
	{
		renderCam = GetComponentInChildren<Camera>();
		characterMultiplayer = GetComponentInParent<CharacterMultiplayer>();
		meshRenderer = GetComponentInChildren<MeshRenderer>();
		if (HasMaterialIndex())
		{
			materialDefault = meshRenderer.materials[materialIndex];
		}
	}

	private void OnEnable()
	{
	}

	private void Start()
	{
		OnAimStop();
	}

	private void Update()
	{
		if (!renderCam || !characterMultiplayer)
		{
			return;
		}
		if (!characterMultiplayer.isMainPlayer && !characterMultiplayer.isSpectating)
		{
			if (renderCam.enabled)
			{
				renderCam.enabled = false;
			}
		}
		else if (!renderCam.enabled)
		{
			renderCam.enabled = true;
		}
	}

	public override float GetMultiplierMouseSensitivity()
	{
		return multiplierMouseSensitivity;
	}

	public override float GetMultiplierSpread()
	{
		return multiplierSpread;
	}

	public override Vector3 GetOffsetAimingLocation()
	{
		return offsetAimingLocation;
	}

	public override Vector3 GetOffsetAimingRotation()
	{
		return offsetAimingRotation;
	}

	public override float GetFieldOfViewMultiplierAim()
	{
		return fieldOfViewMultiplierAim;
	}

	public override float GetFieldOfViewMultiplierAimWeapon()
	{
		return fieldOfViewMultiplierAimWeapon;
	}

	public override Sprite GetSprite()
	{
		return sprite;
	}

	public override float GetSwayMultiplier()
	{
		return swayMultiplier;
	}

	private bool HasMaterialIndex()
	{
		if (meshRenderer == null)
		{
			return false;
		}
		if (materialIndex < meshRenderer.materials.Length)
		{
			return materialIndex >= 0;
		}
		return false;
	}

	public override void OnAim()
	{
		if (HasMaterialIndex())
		{
			Material[] materials = meshRenderer.materials;
			materials[materialIndex] = materialDefault;
			meshRenderer.materials = materials;
		}
	}

	public override void OnAimStop()
	{
		if (HasMaterialIndex())
		{
			Material[] materials = meshRenderer.materials;
			materials[materialIndex] = materialHidden;
			meshRenderer.materials = materials;
		}
	}
}
