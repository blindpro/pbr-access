using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class LandMotion : Motion
{
	[Tooltip("Reference to the character's FeelManager component.")]
	[SerializeField]
	private FeelManager feelManager;

	[Tooltip("Reference to the character's MovementBehaviour component.")]
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

	private float landingTime;

	private bool canLand;

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
		if (movementBehaviour.IsInAir())
		{
			canLand = true;
		}
		if (movementBehaviour.IsGrounded() && !movementBehaviour.WasGrounded() && canLand)
		{
			landingTime = Time.time;
			canLand = false;
		}
		playedCurves = feel.GetState(characterAnimator).LandingCurves;
		float time = Time.time - landingTime;
		value += playedCurves.LocationCurves.EvaluateCurves(time);
		value2 += playedCurves.RotationCurves.EvaluateCurves(time);
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
