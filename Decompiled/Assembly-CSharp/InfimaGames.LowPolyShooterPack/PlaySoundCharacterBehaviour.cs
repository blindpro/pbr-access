using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class PlaySoundCharacterBehaviour : StateMachineBehaviour
{
	private enum SoundType
	{
		GrenadeThrow,
		Melee,
		Holster,
		Unholster,
		Reload,
		ReloadEmpty,
		ReloadOpen,
		ReloadInsert,
		ReloadClose,
		Fire,
		FireEmpty,
		BoltAction
	}

	[Tooltip("Delay at which the audio is played.")]
	[SerializeField]
	private float delay;

	[Tooltip("Type of weapon sound to play.")]
	[SerializeField]
	private SoundType soundType;

	[Tooltip("Audio Settings.")]
	[SerializeField]
	private AudioSettings audioSettings = new AudioSettings(1f, 0f, true);

	private CharacterBehaviour playerCharacter;

	private InventoryBehaviour playerInventory;

	private IAudioManagerService audioManagerService;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (playerCharacter == null)
		{
			playerCharacter = animator.GetComponentInParent<Character>();
			if ((object)playerInventory == null)
			{
				playerInventory = playerCharacter.GetInventory();
			}
		}
		WeaponBehaviour equipped = playerInventory.GetEquipped();
		if ((object)equipped != null)
		{
			if (audioManagerService == null)
			{
				audioManagerService = ServiceLocator.Current.Get<IAudioManagerService>();
			}
			AudioClip clip = soundType switch
			{
				SoundType.GrenadeThrow => playerCharacter.GetAudioClipsGrenadeThrow().GetRandom(), 
				SoundType.Melee => playerCharacter.GetAudioClipsMelee().GetRandom(), 
				SoundType.Holster => equipped.GetAudioClipHolster(), 
				SoundType.Unholster => equipped.GetAudioClipUnholster(), 
				SoundType.Reload => equipped.GetAudioClipReload(), 
				SoundType.ReloadEmpty => equipped.GetAudioClipReloadEmpty(), 
				SoundType.ReloadOpen => equipped.GetAudioClipReloadOpen(), 
				SoundType.ReloadInsert => equipped.GetAudioClipReloadInsert(), 
				SoundType.ReloadClose => equipped.GetAudioClipReloadClose(), 
				SoundType.Fire => equipped.GetAudioClipFire(), 
				SoundType.FireEmpty => equipped.GetAudioClipFireEmpty(), 
				SoundType.BoltAction => equipped.GetAudioClipBoltAction(), 
				_ => null, 
			};
			audioManagerService.PlayOneShotDelayed3D(clip, audioSettings, delay, playerCharacter.transform, playerCharacter.GetComponent<CharacterMultiplayer>().isMainPlayer ? 100 : 128, (soundType == SoundType.Fire) ? 1 : 0);
		}
	}
}
