using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class JumpMotion : Motion
{
	[Tooltip("The character's FeelManager.")]
	[SerializeField]
	private FeelManager feelManager;

	[Tooltip("The character's MovementBehaviour component.")]
	[SerializeField]
	private MovementBehaviour movementBehaviour;

	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("The type of this motion.")]
	[SerializeField]
	private MotionType motionType;

	private readonly Spring springLocation = new Spring();

	private readonly Spring springRotation = new Spring();

	private ACurves playedCurves;

	public override void Tick()
	{
		if (feelManager == null || movementBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		Feel feel = feelManager.Preset.GetFeel(motionType);
		if (feel == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		Vector3 value = default(Vector3);
		Vector3 value2 = default(Vector3);
		FeelState state = feel.GetState(characterAnimator);
		if (!movementBehaviour.IsGrounded())
		{
			float num = Time.time - movementBehaviour.GetLastJumpTime();
			if (movementBehaviour.IsJumping())
			{
				float maxCurveLength = 0f;
				ACurves jumpingCurves = state.JumpingCurves;
				jumpingCurves.LocationCurves.ForEach(delegate(AnimationCurve curve)
				{
					if ((float)curve.length > maxCurveLength)
					{
						maxCurveLength = curve.length;
					}
				});
				jumpingCurves.RotationCurves.ForEach(delegate(AnimationCurve curve)
				{
					if ((float)curve.length > maxCurveLength)
					{
						maxCurveLength = curve.length;
					}
				});
				if (Time.time - movementBehaviour.GetLastJumpTime() >= maxCurveLength)
				{
					num -= maxCurveLength;
					playedCurves = state.FallingCurves;
				}
				else
				{
					playedCurves = state.JumpingCurves;
				}
			}
			else
			{
				playedCurves = state.FallingCurves;
			}
			value += playedCurves.LocationCurves.EvaluateCurves(num);
			value2 += playedCurves.RotationCurves.EvaluateCurves(num);
		}
		springLocation.UpdateEndValue(value);
		springRotation.UpdateEndValue(value2);
	}

	public override Vector3 GetLocation()
	{
		if (playedCurves == null)
		{
			return default(Vector3);
		}
		return springLocation.Evaluate(playedCurves.LocationSpring);
	}

	public override Vector3 GetEulerAngles()
	{
		if (playedCurves == null)
		{
			return default(Vector3);
		}
		return springRotation.Evaluate(playedCurves.RotationSpring);
	}
}
