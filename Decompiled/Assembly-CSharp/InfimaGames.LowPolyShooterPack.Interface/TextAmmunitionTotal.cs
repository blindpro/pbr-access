using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class TextAmmunitionTotal : ElementText
{
	protected override void Tick()
	{
		if (!(equippedWeaponBehaviour == null))
		{
			float num = equippedWeaponBehaviour.GetAmmunitionTotal();
			num *= (float)((Weapon)equippedWeaponBehaviour).GetCurrentMags();
			num = ((Weapon)equippedWeaponBehaviour).GetCurrentMags();
			textMesh.text = num.ToString(CultureInfo.InvariantCulture);
		}
	}
}
