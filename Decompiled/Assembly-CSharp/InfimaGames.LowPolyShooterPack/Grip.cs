using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Grip : GripBehaviour
{
	[Tooltip("Sprite. Displayed on the player's interface.")]
	[SerializeField]
	private Sprite sprite;

	public override Sprite GetSprite()
	{
		return sprite;
	}
}
