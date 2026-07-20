using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_Leaning_Name", menuName = "Infima Games/Low Poly Shooter Pack/Leaning Data", order = 0)]
public class LeaningData : ScriptableObject
{
	[Tooltip("Animation curves played on the item when leaning while the character is aiming.")]
	[SerializeField]
	private ACurves itemAiming;

	[Tooltip("Animation curves played on the item when leaning while the character is standing.")]
	[SerializeField]
	private ACurves itemStanding;

	[Tooltip("Animation curves played on the camera when leaning while the character is aiming.")]
	[SerializeField]
	private ACurves cameraAiming;

	[Tooltip("Animation curves played on the camera when leaning while the character is standing.")]
	[SerializeField]
	private ACurves cameraStanding;

	public ACurves GetCurves(MotionType motionType, bool aiming = false)
	{
		return motionType switch
		{
			MotionType.Camera => aiming ? cameraAiming : cameraStanding, 
			MotionType.Item => aiming ? itemAiming : itemStanding, 
			_ => itemStanding, 
		};
	}
}
