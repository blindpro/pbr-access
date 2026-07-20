using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class LeaningMotion : Motion
{
	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("The type of motion we want this component to apply.")]
	[SerializeField]
	private MotionType motionType;

	private readonly Spring springLocation = new Spring();

	private readonly Spring springRotation = new Spring();

	private ACurves leaningCurves;

	public override void Tick()
	{
		if (inventoryBehaviour == null || characterBehaviour == null || characterAnimator == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		ItemAnimationDataBehaviour component = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
		if (component == null)
		{
			return;
		}
		LeaningData leaningData = component.GetLeaningData();
		if (!(leaningData == null))
		{
			leaningCurves = leaningData.GetCurves(motionType, characterBehaviour.IsAiming());
			if (leaningCurves == null)
			{
				springLocation.UpdateEndValue(default(Vector3));
				springRotation.UpdateEndValue(default(Vector3));
			}
			else
			{
				float time = characterAnimator.GetFloat(AHashes.LeaningInput);
				springLocation.UpdateEndValue(leaningCurves.LocationCurves.EvaluateCurves(time) * leaningCurves.LocationMultiplier);
				springRotation.UpdateEndValue(leaningCurves.RotationCurves.EvaluateCurves(time) * leaningCurves.RotationMultiplier);
			}
		}
	}

	public override Vector3 GetLocation()
	{
		if (leaningCurves == null)
		{
			return default(Vector3);
		}
		return springLocation.Evaluate(leaningCurves.LocationSpring);
	}

	public override Vector3 GetEulerAngles()
	{
		if (leaningCurves == null)
		{
			return default(Vector3);
		}
		return springRotation.Evaluate(leaningCurves.RotationSpring);
	}
}
