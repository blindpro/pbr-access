using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class LoweredMotion : Motion
{
	[Tooltip("The LowerWeapon component that determines whether the character is lowering their weapon, or not at any given time.")]
	[SerializeField]
	private LowerWeapon lowerWeapon;

	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

	private readonly Spring loweredSpringLocation = new Spring();

	private readonly Spring loweredSpringRotation = new Spring();

	private LowerData lowerData;

	public override void Tick()
	{
		if (lowerWeapon == null || characterBehaviour == null || inventoryBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		ItemAnimationDataBehaviour component = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
		if (!(component == null))
		{
			lowerData = component.GetLowerData();
			if (!(lowerData == null))
			{
				loweredSpringLocation.UpdateEndValue(lowerWeapon.IsLowered() ? lowerData.LocationOffset : default(Vector3));
				loweredSpringRotation.UpdateEndValue(lowerWeapon.IsLowered() ? lowerData.RotationOffset : default(Vector3));
			}
		}
	}

	public override Vector3 GetLocation()
	{
		if (lowerData == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return default(Vector3);
		}
		return loweredSpringLocation.Evaluate(lowerData.Interpolation);
	}

	public override Vector3 GetEulerAngles()
	{
		if (lowerData == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return default(Vector3);
		}
		return loweredSpringRotation.Evaluate(lowerData.Interpolation);
	}
}
