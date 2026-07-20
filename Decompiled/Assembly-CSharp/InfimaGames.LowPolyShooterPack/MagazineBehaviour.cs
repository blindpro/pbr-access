using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class MagazineBehaviour : MonoBehaviour
{
	public abstract int GetAmmunitionTotal();

	public abstract Sprite GetSprite();
}
