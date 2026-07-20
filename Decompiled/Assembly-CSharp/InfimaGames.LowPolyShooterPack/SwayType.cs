using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_ST_Default", menuName = "Infima Games/Low Poly Shooter Pack/Sway Type")]
public class SwayType : ScriptableObject
{
	[Tooltip("Horizontal Sway.")]
	[SerializeField]
	private SwayDirection horizontal;

	[Tooltip("Vertical Sway.")]
	[SerializeField]
	private SwayDirection vertical;

	public SwayDirection Horizontal => horizontal;

	public SwayDirection Vertical => vertical;
}
