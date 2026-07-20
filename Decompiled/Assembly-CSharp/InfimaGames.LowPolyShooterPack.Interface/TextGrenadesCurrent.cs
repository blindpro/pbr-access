using System.Globalization;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class TextGrenadesCurrent : ElementText
{
	[Tooltip("Determines if the color of the text should changes as grenades are thrown.")]
	[SerializeField]
	private bool updateColor = true;

	[Tooltip("Determines how fast the color changes as the grenade are thrown.")]
	[SerializeField]
	private float emptySpeed = 1.5f;

	[Tooltip("Color used on this text when the player character has no grendes.")]
	[SerializeField]
	private Color emptyColor = Color.red;

	protected override void Tick()
	{
		float num = characterBehaviour.GetGrenadesCurrent();
		float num2 = characterBehaviour.GetGrenadesTotal();
		textMesh.text = num.ToString(CultureInfo.InvariantCulture);
		if (updateColor)
		{
			float t = num / num2 * emptySpeed;
			textMesh.color = Color.Lerp(emptyColor, Color.white, t);
		}
	}
}
