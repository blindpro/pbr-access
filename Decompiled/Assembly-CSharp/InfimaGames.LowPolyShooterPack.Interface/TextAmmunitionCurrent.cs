using System.Globalization;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class TextAmmunitionCurrent : ElementText
{
	[Tooltip("Determines if the color of the text should changes as ammunition is fired.")]
	[SerializeField]
	private bool updateColor = true;

	[Tooltip("Determines how fast the color changes as the ammunition is fired.")]
	[SerializeField]
	private float emptySpeed = 1.5f;

	[Tooltip("Color used on this text when the player character has no ammunition.")]
	[SerializeField]
	private Color emptyColor = Color.red;

	protected override void Tick()
	{
		if (!(equippedWeaponBehaviour == null))
		{
			float num = equippedWeaponBehaviour.GetAmmunitionCurrent();
			float num2 = equippedWeaponBehaviour.GetAmmunitionTotal();
			textMesh.text = num.ToString(CultureInfo.InvariantCulture);
			if (updateColor)
			{
				float t = num / num2 * emptySpeed;
				textMesh.color = Color.Lerp(emptyColor, Color.white, t);
			}
		}
	}
}
