using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_SD_Default", menuName = "Infima Games/Low Poly Shooter Pack/Sway Data")]
public class SwayData : ScriptableObject
{
	[Tooltip("Look Sway.")]
	[SerializeField]
	private SwayType look;

	[Tooltip("Movement Sway.")]
	[SerializeField]
	private SwayType movement;

	[Tooltip("Spring Settings For Sway.")]
	[SerializeField]
	private SpringSettings springSettings = SpringSettings.Default();

	public SwayType Look => look;

	public SwayType Movement => movement;

	public SpringSettings SpringSettings => springSettings;
}
