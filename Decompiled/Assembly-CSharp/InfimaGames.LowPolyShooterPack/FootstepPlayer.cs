using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class FootstepPlayer : MonoBehaviour
{
	[Tooltip("The character's Movement Behaviour component.")]
	[SerializeField]
	private MovementBehaviour movementBehaviour;

	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("The character component.")]
	[SerializeField]
	private Character character;

	[Tooltip("The character's footstep-dedicated Audio Source component.")]
	[SerializeField]
	private AudioSource audioSource;

	[Tooltip("Minimum magnitude of the movement velocity at which the audio clips will start playing.")]
	[SerializeField]
	private float minVelocityMagnitude = 1f;

	[Tooltip("The audio clip that is played while walking.")]
	[SerializeField]
	private AudioClip audioClipWalking;

	[Tooltip("The audio clip that is played while running.")]
	[SerializeField]
	private AudioClip audioClipRunning;

	[Tooltip("The running audio clip pitch.")]
	[SerializeField]
	private float runningAudioPitch = 0.7f;

	[Tooltip("The walking audio clip pitch.")]
	[SerializeField]
	private float walkingAudioPitch = 1.4f;

	[Tooltip("The crouching audio clip pitch.")]
	[SerializeField]
	private float crouchingAudioPitch = 1.4f;

	[Tooltip("The running audio clip volume.")]
	[SerializeField]
	private float runningAudioVolume = 1f;

	[Tooltip("The walking audio clip volume.")]
	[SerializeField]
	private float walkingAudioVolume = 0.2f;

	[Tooltip("The crouching audio clip volume.")]
	[SerializeField]
	private float crouchingAudioVolume = 0.2f;

	[Tooltip("The audio clip that is played while walking for others.")]
	[SerializeField]
	private AudioClip audioClipWalkingOthers;

	[Tooltip("The audio clip that is played while running for others.")]
	[SerializeField]
	private AudioClip audioClipRunningOthers;

	[Tooltip("The running audio clip pitch Others.")]
	[SerializeField]
	private float runningAudioPitchOthers = 0.7f;

	[Tooltip("The walking audio clip pitch Others.")]
	[SerializeField]
	private float walkingAudioPitchOthers = 1.4f;

	[Tooltip("The crouching audio clip pitch Others.")]
	[SerializeField]
	private float crouchingAudioPitchOthers = 1.4f;

	[Tooltip("The running audio clip volume Others.")]
	[SerializeField]
	private float runningAudioVolumeOthers = 1f;

	[Tooltip("The walking audio clip volume Others.")]
	[SerializeField]
	private float walkingAudioVolumeOthers = 0.2f;

	[Tooltip("The crouching audio clip volume Others.")]
	[SerializeField]
	private float crouchingAudioVolumeOthers = 0.2f;

	private float pitchRun;

	private float pitchWalk;

	private float pitchCrouch;

	private float volumeRun;

	private float volumeWalk;

	private float volumeCrouch;

	private AudioClip runClip;

	private AudioClip walkClip;

	private void Awake()
	{
		if (audioSource != null)
		{
			audioSource.clip = audioClipWalking;
			audioSource.loop = true;
		}
	}

	private void Start()
	{
		pitchRun = runningAudioPitch;
		pitchWalk = walkingAudioPitch;
		pitchCrouch = crouchingAudioPitch;
		volumeRun = runningAudioVolume;
		volumeWalk = walkingAudioVolume;
		volumeCrouch = crouchingAudioVolume;
		runClip = audioClipRunning;
		walkClip = audioClipWalking;
		if (!GetComponent<CharacterMultiplayer>().isMainPlayer)
		{
			pitchRun = runningAudioPitchOthers;
			pitchWalk = walkingAudioPitchOthers;
			pitchCrouch = crouchingAudioPitchOthers;
			volumeRun = runningAudioVolumeOthers;
			volumeWalk = walkingAudioVolumeOthers;
			volumeCrouch = crouchingAudioVolumeOthers;
			runClip = audioClipRunningOthers;
			walkClip = audioClipWalkingOthers;
		}
	}

	private void Update()
	{
		if (characterAnimator == null || movementBehaviour == null || audioSource == null || character == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		if (!movementBehaviour.GetComponent<ThirdPerson>().isActive)
		{
			if (audioSource.isPlaying)
			{
				audioSource.Pause();
			}
			return;
		}
		Vector3 world_velocity = GetComponent<NetworkTransformSynch>().world_velocity;
		if (movementBehaviour.IsGroundedApproximate() && world_velocity.sqrMagnitude > minVelocityMagnitude)
		{
			audioSource.clip = (characterAnimator.GetBool(AHashes.Running) ? runClip : walkClip);
			audioSource.pitch = (characterAnimator.GetBool(AHashes.Running) ? pitchRun : (character.IsCrouching() ? pitchCrouch : pitchWalk));
			audioSource.volume = (characterAnimator.GetBool(AHashes.Running) ? volumeRun : (character.IsCrouching() ? volumeCrouch : volumeWalk));
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
		}
		else if (audioSource.isPlaying)
		{
			audioSource.Pause();
		}
	}
}
