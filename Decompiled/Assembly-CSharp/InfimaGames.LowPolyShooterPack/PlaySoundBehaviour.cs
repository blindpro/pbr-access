using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class PlaySoundBehaviour : StateMachineBehaviour
{
	[Tooltip("AudioClip to play!")]
	[SerializeField]
	private AudioClip clip;

	[Tooltip("Audio Settings.")]
	[SerializeField]
	private AudioSettings settings = new AudioSettings(1f, 0f, true);

	private IAudioManagerService audioManagerService;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (audioManagerService == null)
		{
			audioManagerService = ServiceLocator.Current.Get<IAudioManagerService>();
		}
		audioManagerService?.PlayOneShot3D(clip, settings, animator.transform);
	}
}
