using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_Feel", menuName = "Infima Games/Low Poly Shooter Pack/Feel", order = 0)]
public class Feel : ScriptableObject
{
	[Tooltip("FeelState used while just standing around.")]
	[SerializeField]
	private FeelState standing;

	[Tooltip("FeelState used while crouching.")]
	[SerializeField]
	private FeelState crouching;

	[Tooltip("FeelState used while aiming.")]
	[SerializeField]
	private FeelState aiming;

	[Tooltip("FeelState used while running.")]
	[SerializeField]
	private FeelState running;

	public FeelState Standing => standing;

	public FeelState Crouching => crouching;

	public FeelState Aiming => aiming;

	public FeelState Running => running;

	public FeelState GetState(Animator characterAnimator)
	{
		if (characterAnimator.GetBool(AHashes.Running))
		{
			return Running;
		}
		if (characterAnimator.GetBool(AHashes.Aim))
		{
			return Aiming;
		}
		if (characterAnimator.GetBool(AHashes.Crouching))
		{
			return Crouching;
		}
		return Standing;
	}
}
