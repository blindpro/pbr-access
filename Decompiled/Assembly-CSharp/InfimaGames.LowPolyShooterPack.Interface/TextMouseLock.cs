namespace InfimaGames.LowPolyShooterPack.Interface;

public class TextMouseLock : ElementText
{
	protected override void Tick()
	{
		textMesh.text = "Cursor " + (characterBehaviour.IsCursorLocked() ? "Locked" : "Unlocked");
	}
}
