using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class LaserBehaviour : MonoBehaviour
{
	public abstract Sprite GetSprite();

	public abstract bool GetTurnOffWhileRunning();

	public abstract bool GetTurnOffWhileAiming();

	public abstract void Toggle();

	public abstract void Reapply();

	public abstract void Hide();
}
