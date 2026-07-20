using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class CanvasAlpha : Element
{
	[Tooltip("Canvas group to update the alpha for.")]
	[SerializeField]
	private CanvasGroup canvasGroup;

	[Tooltip("Speed of interpolation.")]
	[Range(0f, 25f)]
	[SerializeField]
	private float interpolationSpeed = 12f;

	[Tooltip("Alpha of the canvasGroup while the cursor is unlocked (pause menu is open).")]
	[Range(0f, 1f)]
	[SerializeField]
	private float cursorUnlockedAlpha = 0.6f;

	protected override void Tick()
	{
		base.Tick();
		if (canvasGroup == null)
		{
			Log.ReferenceError(this, base.gameObject);
		}
		else
		{
			canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, (characterBehaviour.IsCursorLocked() && !CharacterMultiplayer.GetSpectatingPlayer()) ? 1f : cursorUnlockedAlpha, Time.deltaTime * interpolationSpeed);
		}
	}
}
