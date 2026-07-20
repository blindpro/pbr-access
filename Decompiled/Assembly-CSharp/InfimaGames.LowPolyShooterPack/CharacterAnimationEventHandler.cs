using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterAnimationEventHandler : MonoBehaviour
{
	private CharacterBehaviour playerCharacter;

	private void Awake()
	{
		playerCharacter = GetComponentInParent<Character>();
	}

	private void OnEjectCasing()
	{
		if (playerCharacter != null)
		{
			playerCharacter.EjectCasing();
		}
	}

	private void OnAmmunitionFill(int amount = 0)
	{
		if (playerCharacter != null)
		{
			playerCharacter.FillAmmunition(amount);
		}
	}

	private void OnSetActiveKnife(int active)
	{
		if (playerCharacter != null)
		{
			playerCharacter.SetActiveKnife(active);
		}
	}

	private void OnGrenade()
	{
		if (playerCharacter != null)
		{
			playerCharacter.Grenade();
		}
	}

	private void OnSetActiveMagazine(int active)
	{
		if (playerCharacter != null)
		{
			playerCharacter.SetActiveMagazine(active);
		}
	}

	private void OnAnimationEndedBolt()
	{
		if (playerCharacter != null)
		{
			playerCharacter.AnimationEndedBolt();
		}
	}

	private void OnAnimationEndedReload()
	{
		if (playerCharacter != null)
		{
			playerCharacter.AnimationEndedReload();
		}
	}

	private void OnAnimationEndedGrenadeThrow()
	{
		if (playerCharacter != null)
		{
			playerCharacter.AnimationEndedGrenadeThrow();
		}
	}

	private void OnAnimationEndedMelee()
	{
		if (playerCharacter != null)
		{
			playerCharacter.AnimationEndedMelee();
		}
	}

	private void OnAnimationEndedInspect()
	{
		if (playerCharacter != null)
		{
			playerCharacter.AnimationEndedInspect();
		}
	}

	private void OnAnimationEndedHolster()
	{
		if (playerCharacter != null)
		{
			playerCharacter.AnimationEndedHolster();
		}
	}

	private void OnSlideBack(int back)
	{
		if (playerCharacter != null)
		{
			playerCharacter.SetSlideBack(back);
		}
	}
}
