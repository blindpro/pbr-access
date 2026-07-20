using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class RecoilMotion : Motion
{
	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The type of motion we want this component to apply.")]
	[SerializeField]
	private MotionType motionType;

	private readonly Spring recoilSpringLocation = new Spring();

	private readonly Spring recoilSpringRotation = new Spring();

	private ACurves recoilCurves;

	public override void Tick()
	{
		if (inventoryBehaviour == null || characterBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		ItemAnimationDataBehaviour component = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
		if (component == null)
		{
			return;
		}
		RecoilData recoilData = component.GetRecoilData(motionType);
		if (recoilData == null)
		{
			return;
		}
		int num = characterBehaviour.GetShotsFired();
		float num2 = recoilData.StandingStateMultiplier;
		CharacterMultiplayer component2 = characterBehaviour.GetComponent<CharacterMultiplayer>();
		if ((bool)component2 && !component2.isMainPlayer)
		{
			num = 0;
		}
		Vector3 value = default(Vector3);
		Vector3 value2 = default(Vector3);
		recoilCurves = recoilData.StandingState;
		if (characterBehaviour.IsAiming())
		{
			num2 = recoilData.AimingStateMultiplier;
			recoilCurves = recoilData.AimingState;
		}
		if (recoilCurves != null)
		{
			if (recoilCurves.LocationCurves.Length == 3)
			{
				value.x = recoilCurves.LocationCurves[0].Evaluate(num);
				value.y = recoilCurves.LocationCurves[1].Evaluate(num);
				value.z = recoilCurves.LocationCurves[2].Evaluate(num);
			}
			if (recoilCurves.RotationCurves.Length == 3)
			{
				value2.x = recoilCurves.RotationCurves[0].Evaluate(num);
				value2.y = recoilCurves.RotationCurves[1].Evaluate(num);
				value2.z = recoilCurves.RotationCurves[2].Evaluate(num);
			}
			value *= recoilCurves.LocationMultiplier * num2;
			value2 *= recoilCurves.RotationMultiplier * num2;
		}
		recoilSpringLocation.UpdateEndValue(value);
		recoilSpringRotation.UpdateEndValue(value2);
	}

	public override Vector3 GetLocation()
	{
		if (recoilCurves == null)
		{
			return default(Vector3);
		}
		return recoilSpringLocation.Evaluate(recoilCurves.LocationSpring);
	}

	public override Vector3 GetEulerAngles()
	{
		if (recoilCurves == null)
		{
			return default(Vector3);
		}
		return recoilSpringRotation.Evaluate(recoilCurves.RotationSpring);
	}
}
