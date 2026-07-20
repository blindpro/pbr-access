using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_Recoil", menuName = "Infima Games/Low Poly Shooter Pack/Recoil Data", order = 0)]
public class RecoilData : ScriptableObject
{
	[Tooltip("Value to multiply the standingState location/rotation values by.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float standingStateMultiplier = 1f;

	[Tooltip("Standing State.")]
	[SerializeField]
	private ACurves standingState;

	[Tooltip("Value to multiply the aimingState location/rotation values by.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float aimingStateMultiplier = 1f;

	[Tooltip("Aiming State.")]
	[SerializeField]
	private ACurves aimingState;

	public float StandingStateMultiplier => standingStateMultiplier;

	public ACurves StandingState => standingState;

	public float AimingStateMultiplier => aimingStateMultiplier;

	public ACurves AimingState => aimingState;
}
