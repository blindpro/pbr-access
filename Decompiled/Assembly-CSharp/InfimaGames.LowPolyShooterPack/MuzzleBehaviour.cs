using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class MuzzleBehaviour : MonoBehaviour
{
	public abstract Transform GetSocket();

	public abstract Sprite GetSprite();

	public abstract AudioClip GetAudioClipFire();

	public abstract ParticleSystem GetParticlesFire();

	public abstract int GetParticlesFireCount();

	public abstract Light GetFlashLight();

	public abstract float GetFlashLightDuration();

	public abstract void Effect();
}
