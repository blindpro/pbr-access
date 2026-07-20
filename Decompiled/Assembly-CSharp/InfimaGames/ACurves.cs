using UnityEngine;

namespace InfimaGames;

[CreateAssetMenu(fileName = "SO_AC_Default", menuName = "Infima Games/Animation Curves")]
public class ACurves : ScriptableObject
{
	[Range(0f, 10f)]
	[Tooltip("Multiplier applied to the location curves.")]
	[SerializeField]
	private float locationMultiplier = 1f;

	[Tooltip("Interpolation settings for the location.")]
	[SerializeField]
	private SpringSettings locationSpring = SpringSettings.Default();

	[Tooltip("Animated location curves.")]
	[SerializeField]
	private AnimationCurve[] locationCurves;

	[Range(0f, 10f)]
	[Tooltip("Multiplier applied to the rotation curves.")]
	[SerializeField]
	private float rotationMultiplier = 1f;

	[Tooltip("Interpolation settings for the rotation.")]
	[SerializeField]
	private SpringSettings rotationSpring = SpringSettings.Default();

	[Tooltip("Animated rotation curves.")]
	[SerializeField]
	private AnimationCurve[] rotationCurves;

	public SpringSettings LocationSpring => locationSpring;

	public AnimationCurve[] LocationCurves => locationCurves;

	public float LocationMultiplier => locationMultiplier;

	public SpringSettings RotationSpring => rotationSpring;

	public AnimationCurve[] RotationCurves => rotationCurves;

	public float RotationMultiplier => rotationMultiplier;
}
