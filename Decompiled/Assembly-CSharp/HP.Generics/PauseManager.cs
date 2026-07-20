using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class PauseManager : MonoBehaviour
{
	[Serializable]
	public class C_Pause
	{
		public string m_Name;

		public UnityEvent m_Pause;

		public UnityEvent m_Unpause;
	}

	public bool SeeInspector;

	public static PauseManager instance;

	public bool helpBox = true;

	public bool moreOptions;

	public bool Bool_IsGamePaused;

	public Action<int> OnPause;

	public Action<int> OnUnPause;

	public bool isPauseModeEnable;

	public List<C_Pause> listOfPause = new List<C_Pause>();

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	public void PauseChangeAnimatorSpeed(float _speed)
	{
		Animator[] array = UnityEngine.Object.FindObjectsOfType<Animator>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].speed = _speed;
		}
	}

	public void PauseAudio(bool b_PauseAudio)
	{
		AudioSource[] array = UnityEngine.Object.FindObjectsOfType<AudioSource>();
		foreach (AudioSource audioSource in array)
		{
			if (!audioSource.GetComponent<IgnorePause>())
			{
				if (b_PauseAudio)
				{
					audioSource.Pause();
				}
				else
				{
					audioSource.UnPause();
				}
			}
		}
	}

	public void PauseParticle(bool b_PauseParticle)
	{
		ParticleSystem[] array = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
		foreach (ParticleSystem particleSystem in array)
		{
			if (!particleSystem.GetComponent<IgnorePause>())
			{
				if (b_PauseParticle)
				{
					particleSystem.Pause();
				}
				else if (particleSystem.isPaused)
				{
					particleSystem.Play();
				}
			}
		}
	}

	public void bPauseGame_Bool_IsGamePaused()
	{
		Bool_IsGamePaused = true;
	}

	public void UnpauseGame_Bool_IsGamePaused()
	{
		Bool_IsGamePaused = false;
	}

	public void PauseGame(int selectedPause = 0)
	{
		OnPause?.Invoke(selectedPause);
		listOfPause[selectedPause].m_Pause.Invoke();
	}

	public void UnpauseGame(int SelectedUnpause = 0)
	{
		OnUnPause?.Invoke(SelectedUnpause);
		listOfPause[SelectedUnpause].m_Unpause.Invoke();
	}
}
