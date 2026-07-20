using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class MusicPlayer : MonoBehaviour
{
	[Header("Assign your lobby music clips here")]
	public AudioClip[] musicClips;

	[Header("Audio settings")]
	public AudioSource audioSource;

	public bool shuffle = true;

	public Transform homeUI;

	[Header("Volume Settings")]
	[Range(0f, 1f)]
	public float globalMusicVolume = 1f;

	[Range(0f, 1f)]
	public float lobbyTargetVolume = 1f;

	[Range(0f, 5f)]
	public float fadeSpeed = 1.5f;

	private AudioClip currentClip;

	private MatchmakingManager matchmakingManager;

	private void OnDestroy()
	{
	}

	private void OnDisable()
	{
	}

	private void Start()
	{
		matchmakingManager = GetComponent<MatchmakingManager>();
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		audioSource.loop = false;
		audioSource.volume = 0f;
		PlayRandomMusic();
	}

	private void Update()
	{
		if (!audioSource.isPlaying)
		{
			PlayRandomMusic();
		}
		float b = (homeUI.gameObject.activeSelf ? lobbyTargetVolume : 0f) * globalMusicVolume;
		audioSource.volume = Mathf.Lerp(audioSource.volume, b, Time.deltaTime * fadeSpeed);
	}

	private void PlayRandomMusic()
	{
		if (musicClips != null && musicClips.Length != 0)
		{
			AudioClip audioClip;
			do
			{
				audioClip = musicClips[Random.Range(0, musicClips.Length)];
			}
			while (shuffle && audioClip == currentClip && musicClips.Length > 1);
			currentClip = audioClip;
			audioSource.clip = currentClip;
			audioSource.Play();
		}
	}
}
