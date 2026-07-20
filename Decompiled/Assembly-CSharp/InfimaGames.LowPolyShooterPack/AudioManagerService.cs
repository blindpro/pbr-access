using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class AudioManagerService : MonoBehaviour, IAudioManagerService, IGameService
{
	private readonly struct OneShotCoroutine
	{
		public AudioClip Clip { get; }

		public AudioSettings Settings { get; }

		public float Delay { get; }

		public OneShotCoroutine(AudioClip clip, AudioSettings settings, float delay)
		{
			Clip = clip;
			Settings = settings;
			Delay = delay;
		}
	}

	private readonly struct OneShotCoroutine3D
	{
		public AudioClip Clip { get; }

		public AudioSettings Settings { get; }

		public float Delay { get; }

		public Transform Obj { get; }

		public int Priority { get; }

		public int IsFarSound { get; }

		public OneShotCoroutine3D(AudioClip clip, AudioSettings settings, float delay, Transform obj, int priority, int isFarSound)
		{
			Clip = clip;
			Settings = settings;
			Delay = delay;
			Obj = obj;
			Priority = priority;
			IsFarSound = isFarSound;
		}
	}

	private bool IsPlayingSource(AudioSource source)
	{
		if (source == null)
		{
			return false;
		}
		return source.isPlaying;
	}

	private IEnumerator DestroySourceWhenFinished(AudioSource source)
	{
		yield return new WaitWhile(() => IsPlayingSource(source));
		if (source != null)
		{
			Object.DestroyImmediate(source.gameObject);
		}
	}

	private IEnumerator PlayOneShotAfterDelay(OneShotCoroutine value)
	{
		yield return new WaitForSeconds(value.Delay);
		PlayOneShot_Internal(value.Clip, value.Settings);
	}

	private IEnumerator PlayOneShotAfterDelay3D(OneShotCoroutine3D value)
	{
		yield return new WaitForSeconds(value.Delay);
		PlayOneShot_Internal(value.Clip, value.Settings, value.Obj, value.Priority, value.IsFarSound);
	}

	private void PlayOneShot_Internal(AudioClip clip, AudioSettings settings, Transform posObj = null, int priority = 128, int isFarSound = 0)
	{
		if (!(clip == null))
		{
			AudioSource obj = ((isFarSound == 0) ? GameManager.Instance.GetNextAudioSource() : GameManager.Instance.GetNextAudioSourceFar());
			obj.volume = settings.Volume;
			obj.transform.position = posObj.position;
			obj.priority = priority;
			obj.PlayOneShot(clip);
		}
	}

	public void PlayOneShot(AudioClip clip, AudioSettings settings = default(AudioSettings))
	{
		PlayOneShot_Internal(clip, settings);
	}

	public void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default(AudioSettings), float delay = 1f)
	{
		StartCoroutine("PlayOneShotAfterDelay", new OneShotCoroutine(clip, settings, delay));
	}

	public void PlayOneShot3D(AudioClip clip, AudioSettings settings = default(AudioSettings), Transform posObj = null, int priority = 128)
	{
		PlayOneShot_Internal(clip, settings, posObj, priority);
	}

	public void PlayOneShotDelayed3D(AudioClip clip, AudioSettings settings = default(AudioSettings), float delay = 1f, Transform posObj = null, int priority = 128, int isFarSound = 0)
	{
		StartCoroutine("PlayOneShotAfterDelay3D", new OneShotCoroutine3D(clip, settings, delay, posObj, priority, isFarSound));
	}
}
