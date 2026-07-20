using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Magazine : MagazineBehaviour
{
	[Tooltip("Total Ammunition.")]
	[SerializeField]
	private int ammunitionTotal = 10;

	[Tooltip("Total Mags.")]
	[SerializeField]
	private int magsTotal = 3;

	[Tooltip("Interface Sprite.")]
	[SerializeField]
	private Sprite sprite;

	private int currentMags;

	public override int GetAmmunitionTotal()
	{
		return ammunitionTotal;
	}

	public int GetMagsTotal()
	{
		return magsTotal;
	}

	public int GetCurrentMags()
	{
		return currentMags;
	}

	public override Sprite GetSprite()
	{
		return sprite;
	}

	public void Restart()
	{
		currentMags = magsTotal;
	}

	public void ReduceMag()
	{
		currentMags--;
		if (currentMags < 0)
		{
			currentMags = 0;
		}
	}

	public void SetMags(int mags)
	{
		currentMags = mags;
	}
}
