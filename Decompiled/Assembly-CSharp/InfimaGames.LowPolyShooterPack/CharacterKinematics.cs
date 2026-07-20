using System.Collections.Generic;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterKinematics : MonoBehaviour
{
	[Tooltip("Reference to the character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("Left Arm Target. Determines what the IK target is.")]
	[SerializeField]
	private Transform armLeftTarget;

	[Range(0f, 1f)]
	[Tooltip("Inverse Kinematics Weight for the left arm.")]
	[SerializeField]
	private float armLeftWeightPosition = 1f;

	[Range(0f, 1f)]
	[Tooltip("Inverse Kinematics Weight for the left arm.")]
	[SerializeField]
	private float armLeftWeightRotation = 1f;

	[Tooltip("Left Arm Hierarchy. Root, Mid, Tip.")]
	[SerializeField]
	private Transform[] armLeftHierarchy;

	[Tooltip("Left Arm Target. Determines what the IK target is.")]
	[SerializeField]
	private Transform armRightTarget;

	[Range(0f, 1f)]
	[Tooltip("Inverse Kinematics Weight for the right arm.")]
	[SerializeField]
	private float armRightWeightPosition = 1f;

	[Range(0f, 1f)]
	[Tooltip("Inverse Kinematics Weight for the right arm.")]
	[SerializeField]
	private float armRightWeightRotation = 1f;

	[Tooltip("Right Arm Hierarchy. Root, Mid, Tip.")]
	[SerializeField]
	private Transform[] armRightHierarchy;

	[Tooltip("Hint.")]
	[SerializeField]
	private Transform hint;

	[Range(0f, 1f)]
	[Tooltip("Hint Weight.")]
	[SerializeField]
	private float weightHint;

	private bool maintainTargetPositionOffset;

	private bool maintainTargetRotationOffset;

	private float alphaLeft;

	private float alphaRight;

	private const float kSqrEpsilon = 1E-08f;

	private void Update()
	{
		alphaLeft = characterAnimator.GetFloat(AHashes.AlphaIKHandLeft);
		alphaRight = characterAnimator.GetFloat(AHashes.AlphaIKHandRight);
	}

	private void LateUpdate()
	{
		if (characterAnimator == null)
		{
			Log.ReferenceError(this, base.gameObject);
		}
		else
		{
			Compute(alphaLeft, alphaRight);
		}
	}

	private void Compute(float weightLeft = 1f, float weightRight = 1f)
	{
		ComputeOnce(armLeftHierarchy, armLeftTarget, armLeftWeightPosition * weightLeft, armLeftWeightRotation * weightLeft);
		ComputeOnce(armRightHierarchy, armRightTarget, armRightWeightPosition * weightRight, armRightWeightRotation * weightRight);
	}

	private void ComputeOnce(IReadOnlyList<Transform> hierarchy, Transform target, float weightPosition = 1f, float weightRotation = 1f)
	{
		Vector3 vector = Vector3.zero;
		Quaternion quaternion = Quaternion.identity;
		if (maintainTargetPositionOffset)
		{
			vector = hierarchy[2].position - target.position;
		}
		if (maintainTargetRotationOffset)
		{
			quaternion = Quaternion.Inverse(target.rotation) * hierarchy[2].rotation;
		}
		Vector3 position = hierarchy[0].position;
		Vector3 position2 = hierarchy[1].position;
		Vector3 position3 = hierarchy[2].position;
		Vector3 position4 = target.position;
		Quaternion rotation = target.rotation;
		Vector3 vector2 = Vector3.Lerp(position3, position4 + vector, weightPosition);
		Quaternion rotation2 = Quaternion.Lerp(hierarchy[2].rotation, rotation * quaternion, weightRotation);
		bool flag = hint != null && weightHint > 0f;
		Vector3 lhs = position2 - position;
		Vector3 rhs = position3 - position2;
		Vector3 vector3 = position3 - position;
		Vector3 vector4 = vector2 - position;
		float magnitude = lhs.magnitude;
		float magnitude2 = rhs.magnitude;
		float magnitude3 = vector3.magnitude;
		float magnitude4 = vector4.magnitude;
		float num = TriangleAngle(magnitude3, magnitude, magnitude2);
		float num2 = TriangleAngle(magnitude4, magnitude, magnitude2);
		Vector3 value = Vector3.Cross(lhs, rhs);
		if (value.sqrMagnitude < 1E-08f)
		{
			value = (flag ? Vector3.Cross(hint.position - position, rhs) : Vector3.zero);
			if (value.sqrMagnitude < 1E-08f)
			{
				value = Vector3.Cross(vector4, rhs);
			}
			if (value.sqrMagnitude < 1E-08f)
			{
				value = Vector3.up;
			}
		}
		value = Vector3.Normalize(value);
		float f = 0.5f * (num - num2);
		float num3 = Mathf.Sin(f);
		float w = Mathf.Cos(f);
		Quaternion quaternion2 = new Quaternion(value.x * num3, value.y * num3, value.z * num3, w);
		hierarchy[1].rotation = quaternion2 * hierarchy[1].rotation;
		vector3 = hierarchy[2].position - position;
		hierarchy[0].rotation = Quaternion.FromToRotation(vector3, vector4) * hierarchy[0].rotation;
		if (flag)
		{
			float sqrMagnitude = vector3.sqrMagnitude;
			if (sqrMagnitude > 0f)
			{
				position2 = hierarchy[1].position;
				Vector3 position5 = hierarchy[2].position;
				lhs = position2 - position;
				vector3 = position5 - position;
				Vector3 vector5 = vector3 / Mathf.Sqrt(sqrMagnitude);
				Vector3 vector6 = hint.position - position;
				Vector3 fromDirection = lhs - vector5 * Vector3.Dot(lhs, vector5);
				Vector3 toDirection = vector6 - vector5 * Vector3.Dot(vector6, vector5);
				float num4 = magnitude + magnitude2;
				if (fromDirection.sqrMagnitude > num4 * num4 * 0.001f && toDirection.sqrMagnitude > 0f)
				{
					Quaternion q = Quaternion.FromToRotation(fromDirection, toDirection);
					q.x *= weightHint;
					q.y *= weightHint;
					q.z *= weightHint;
					q = Quaternion.Normalize(q);
					hierarchy[0].rotation = q * hierarchy[0].rotation;
				}
			}
		}
		hierarchy[2].rotation = rotation2;
	}

	private static float TriangleAngle(float aLen, float aLen1, float aLen2)
	{
		return Mathf.Acos(Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2f, -1f, 1f));
	}
}
