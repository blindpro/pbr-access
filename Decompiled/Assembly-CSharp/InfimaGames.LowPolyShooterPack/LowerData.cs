using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_Lower_Name", menuName = "Infima Games/Low Poly Shooter Pack/Lower Data", order = 0)]
public class LowerData : ScriptableObject
{
	[Tooltip("Interpolation settings.")]
	[SerializeField]
	private SpringSettings interpolation = SpringSettings.Default();

	[Tooltip("Location offset applied in the lowered state.")]
	[SerializeField]
	private Vector3 locationOffset;

	[Tooltip("Rotation offset applied in the lowered state.")]
	[SerializeField]
	private Vector3 rotationOffset;

	public SpringSettings Interpolation => interpolation;

	public Vector3 LocationOffset => locationOffset;

	public Vector3 RotationOffset => rotationOffset;
}
