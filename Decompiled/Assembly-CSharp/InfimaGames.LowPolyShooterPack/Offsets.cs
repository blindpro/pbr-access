using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[Serializable]
public struct Offsets
{
	[Header("Standing Offset")]
	[Tooltip("Weapon bone location offset while standing.")]
	[SerializeField]
	private Vector3 standingLocation;

	[Tooltip("Weapon bone rotation offset while standing.")]
	[SerializeField]
	private Vector3 standingRotation;

	[Header("Aiming Offset")]
	[Tooltip("Weapon bone location offset while aiming.")]
	[SerializeField]
	private Vector3 aimingLocation;

	[Tooltip("Weapon bone rotation offset while aiming.")]
	[SerializeField]
	private Vector3 aimingRotation;

	[Header("Running Offset")]
	[Tooltip("Weapon bone location offset while running.")]
	[SerializeField]
	private Vector3 runningLocation;

	[Tooltip("Weapon bone rotation offset while running.")]
	[SerializeField]
	private Vector3 runningRotation;

	[Header("Crouching Offset")]
	[Tooltip("Weapon bone location offset while crouching.")]
	[SerializeField]
	private Vector3 crouchingLocation;

	[Tooltip("Weapon bone rotation offset while crouching.")]
	[SerializeField]
	private Vector3 crouchingRotation;

	[Header("Action Offset")]
	[Tooltip("Weapon bone location offset while performing an action (grenade, melee).")]
	[SerializeField]
	private Vector3 actionLocation;

	[Tooltip("Weapon bone rotation offset while performing an action (grenade, melee).")]
	[SerializeField]
	private Vector3 actionRotation;

	public Vector3 StandingLocation => standingLocation;

	public Vector3 StandingRotation => standingRotation;

	public Vector3 AimingLocation => aimingLocation;

	public Vector3 AimingRotation => aimingRotation;

	public Vector3 RunningLocation => runningLocation;

	public Vector3 RunningRotation => runningRotation;

	public Vector3 CrouchingLocation => crouchingLocation;

	public Vector3 CrouchingRotation => crouchingRotation;

	public Vector3 ActionLocation => actionLocation;

	public Vector3 ActionRotation => actionRotation;
}
