using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class TextTimescale : ElementText
{
	protected override void Tick()
	{
		textMesh.text = "Timescale : " + Time.timeScale;
	}
}
