using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public interface IAudioManagerService : IGameService
{
	void PlayOneShot(AudioClip clip, AudioSettings settings = default(AudioSettings));

	void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default(AudioSettings), float delay = 1f);

	void PlayOneShot3D(AudioClip clip, AudioSettings settings = default(AudioSettings), Transform posObj = null, int priority = 128);

	void PlayOneShotDelayed3D(AudioClip clip, AudioSettings settings = default(AudioSettings), float delay = 1f, Transform posObj = null, int priority = 128, int isFarSound = 0);
}
