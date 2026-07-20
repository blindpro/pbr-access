using System.Collections.Generic;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class MotionApplier : MonoBehaviour
{
	[Tooltip("Determines the way this component applies the values for all subscribed Motion components.")]
	[SerializeField]
	private ApplyMode applyMode;

	private readonly List<Motion> motions = new List<Motion>();

	private Transform thisTransform;

	private void Awake()
	{
		thisTransform = base.transform;
	}

	private void LateUpdate()
	{
		Vector3 finalLocation = default(Vector3);
		Vector3 finaEulerAngles = default(Vector3);
		motions.ForEach(delegate(Motion motion)
		{
			motion.Tick();
			finalLocation += motion.GetLocation() * motion.Alpha;
			finaEulerAngles += motion.GetEulerAngles() * motion.Alpha;
		});
		if (applyMode == ApplyMode.Override)
		{
			thisTransform.localPosition = finalLocation;
			thisTransform.localEulerAngles = finaEulerAngles;
		}
		else if (applyMode == ApplyMode.Add)
		{
			thisTransform.localPosition += finalLocation;
			thisTransform.localEulerAngles += finaEulerAngles;
		}
	}

	public void Subscribe(Motion motion)
	{
		motions.Add(motion);
	}
}
