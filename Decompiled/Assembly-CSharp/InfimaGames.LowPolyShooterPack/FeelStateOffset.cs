using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_FSO_Default", menuName = "Infima Games/Low Poly Shooter Pack/Feel State Offset", order = 0)]
public class FeelStateOffset : ScriptableObject
{
	[Tooltip("The location offset.")]
	[SerializeField]
	public Vector3 offsetLocation;

	[Tooltip("Spring settings relating to interpolating the location.")]
	[SerializeField]
	public SpringSettings springSettingsLocation = SpringSettings.Default();

	[Tooltip("The rotation offset.")]
	[SerializeField]
	public Vector3 offsetRotation;

	[Tooltip("Spring settings relating to interpolating the rotation.")]
	[SerializeField]
	public SpringSettings springSettingsRotation = SpringSettings.Default();

	public Vector3 OffsetLocation => offsetLocation;

	public SpringSettings SpringSettingsLocation => springSettingsLocation;

	public Vector3 OffsetRotation => offsetRotation;

	public SpringSettings SpringSettingsRotation => springSettingsRotation;
}
