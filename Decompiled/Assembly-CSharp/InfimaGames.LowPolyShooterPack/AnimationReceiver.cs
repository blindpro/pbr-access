using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class AnimationReceiver : MonoBehaviour
{
	private CharacterDemonstration characterDemonstration;

	private void Awake()
	{
		characterDemonstration = GetComponent<CharacterDemonstration>();
	}

	private void OnAmmunitionFill(int amount = 0)
	{
	}

	private void OnGrenade()
	{
	}

	private void OnSetActiveMagazine(int active)
	{
	}

	private void OnAnimationEndedBolt()
	{
	}

	private void OnAnimationEndedReload()
	{
	}

	private void OnAnimationEndedGrenadeThrow()
	{
	}

	private void OnAnimationEndedMelee()
	{
	}

	private void OnAnimationEndedInspect()
	{
	}

	private void OnAnimationEndedHolster()
	{
	}

	private void OnEjectCasing()
	{
	}

	private void OnSlideBack()
	{
	}

	private void OnSetActiveKnife()
	{
	}

	private void OnDropMagazine(int drop = 0)
	{
		if (characterDemonstration != null)
		{
			characterDemonstration.DropMagazine(drop == 0);
		}
	}
}
