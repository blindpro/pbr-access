using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class SwayMotion : Motion
{
	[Tooltip("The character's FeelManager component.")]
	[SerializeField]
	private FeelManager feelManager;

	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The type of motion we want this component to apply.")]
	[SerializeField]
	private MotionType motionType;

	private readonly Spring springLocation = new Spring();

	private readonly Spring springRotation = new Spring();

	private FeelState feelState;

	public override void Tick()
	{
		if (feelManager == null || characterBehaviour == null || inventoryBehaviour == null || characterAnimator == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		Vector2 vector = Vector2.ClampMagnitude(characterBehaviour.GetInputLook(), 1f);
		Vector2 vector2 = Vector2.ClampMagnitude(characterBehaviour.GetInputMovement(), 1f);
		if (characterBehaviour.GetComponent<CharacterMultiplayer>().isBot)
		{
			Vector3 world_velocity = characterBehaviour.GetComponent<NetworkTransformSynch>().world_velocity;
			world_velocity.y = 0f;
			Vector3 vector3 = characterBehaviour.transform.InverseTransformDirection(world_velocity);
			vector2 = Vector2.ClampMagnitude(new Vector2(vector3.x, vector3.z), 1f);
		}
		FeelPreset preset = feelManager.Preset;
		if (preset == null)
		{
			return;
		}
		Feel feel = preset.GetFeel(motionType);
		if (!(feel == null))
		{
			feelState = feel.GetState(characterAnimator);
			ScopeBehaviour equippedScope = inventoryBehaviour.GetEquipped().GetAttachmentManager().GetEquippedScope();
			SwayData swayData = feelState.SwayData;
			if (!(swayData == null))
			{
				Vector3 vector4 = default(Vector3);
				vector4 += swayData.Look.Horizontal.locationCurves.EvaluateCurves(vector.x) * swayData.Look.Horizontal.locationMultiplier;
				vector4 += swayData.Movement.Horizontal.locationCurves.EvaluateCurves(vector2.x) * swayData.Movement.Horizontal.locationMultiplier;
				Vector3 vector5 = default(Vector3);
				vector5 += swayData.Look.Vertical.locationCurves.EvaluateCurves(vector.y) * swayData.Look.Vertical.locationMultiplier;
				vector5 += swayData.Movement.Vertical.locationCurves.EvaluateCurves(vector2.y) * swayData.Movement.Vertical.locationMultiplier;
				Vector3 vector6 = default(Vector3);
				vector6 += swayData.Look.Horizontal.rotationCurves.EvaluateCurves(vector.x) * swayData.Look.Horizontal.rotationMultiplier;
				vector6 += swayData.Movement.Horizontal.rotationCurves.EvaluateCurves(vector2.x) * swayData.Movement.Horizontal.rotationMultiplier;
				Vector3 vector7 = default(Vector3);
				vector7 += swayData.Look.Vertical.rotationCurves.EvaluateCurves(vector.y) * swayData.Look.Vertical.rotationMultiplier;
				vector7 += swayData.Movement.Vertical.rotationCurves.EvaluateCurves(vector2.y) * swayData.Movement.Vertical.rotationMultiplier;
				springLocation.UpdateEndValue(equippedScope.GetSwayMultiplier() * (vector4 + vector5));
				springRotation.UpdateEndValue(equippedScope.GetSwayMultiplier() * (vector6 + vector7));
			}
		}
	}

	public override Vector3 GetLocation()
	{
		if (feelState.SwayData == null)
		{
			return default(Vector3);
		}
		return springLocation.Evaluate(feelState.SwayData.SpringSettings);
	}

	public override Vector3 GetEulerAngles()
	{
		if (feelState.SwayData == null)
		{
			return default(Vector3);
		}
		return springRotation.Evaluate(feelState.SwayData.SpringSettings);
	}
}
