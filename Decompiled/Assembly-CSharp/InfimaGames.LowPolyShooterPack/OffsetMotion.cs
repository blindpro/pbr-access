using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class OffsetMotion : Motion
{
	[Tooltip("The character's FeelManager component.")]
	[SerializeField]
	private FeelManager feelManager;

	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

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
		FeelPreset preset = feelManager.Preset;
		if (preset == null)
		{
			return;
		}
		Feel feel = preset.GetFeel(motionType);
		if (feel == null)
		{
			return;
		}
		WeaponBehaviour equipped = inventoryBehaviour.GetEquipped();
		if (equipped == null)
		{
			return;
		}
		ItemAnimationDataBehaviour component = equipped.GetComponent<ItemAnimationDataBehaviour>();
		if (component == null)
		{
			return;
		}
		WeaponAttachmentManagerBehaviour attachmentManager = equipped.GetAttachmentManager();
		if (attachmentManager == null)
		{
			return;
		}
		ScopeBehaviour equippedScope = attachmentManager.GetEquippedScope();
		if (equippedScope == null)
		{
			return;
		}
		ItemOffsets itemOffsets = component.GetItemOffsets();
		if (!(itemOffsets == null))
		{
			Vector3 value = default(Vector3);
			Vector3 value2 = default(Vector3);
			if (characterAnimator.GetBool(AHashes.Running))
			{
				value += itemOffsets.RunningLocation;
				value2 += itemOffsets.RunningRotation;
				feelState = feel.Running;
			}
			else if (characterAnimator.GetBool(AHashes.Aim))
			{
				value += itemOffsets.AimingLocation;
				value2 += itemOffsets.AimingRotation;
				value += equippedScope.GetOffsetAimingLocation();
				value2 += equippedScope.GetOffsetAimingRotation();
				feelState = feel.Aiming;
			}
			else if (characterAnimator.GetBool(AHashes.Crouching))
			{
				value += itemOffsets.CrouchingLocation;
				value2 += itemOffsets.CrouchingRotation;
				feelState = feel.Crouching;
			}
			else
			{
				value += itemOffsets.StandingLocation;
				value2 += itemOffsets.StandingRotation;
				feelState = feel.Standing;
			}
			float num = characterAnimator.GetFloat(AHashes.AlphaActionOffset);
			value += itemOffsets.ActionLocation * num;
			value2 += itemOffsets.ActionRotation * num;
			value += feelState.Offset.OffsetLocation;
			value2 += feelState.Offset.OffsetRotation;
			springLocation.UpdateEndValue(value);
			springRotation.UpdateEndValue(value2);
		}
	}

	public override Vector3 GetLocation()
	{
		if (feelState.Offset == null)
		{
			return default(Vector3);
		}
		return springLocation.Evaluate(feelState.Offset.SpringSettingsLocation);
	}

	public override Vector3 GetEulerAngles()
	{
		if (feelState.Offset == null)
		{
			return default(Vector3);
		}
		return springRotation.Evaluate(feelState.Offset.SpringSettingsRotation);
	}
}
