using System;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class footstepSystem : MonoBehaviour
{
	[Serializable]
	public class Footsteps
	{
		public List<AudioClip> footstepSamples;

		public string MaterialTag = "";
	}

	[Serializable]
	public class compareCharacterMagnitude
	{
		public float listTimeBetweenTwoStep = 0.3f;

		public float listCharacterMangnitude = 0.4f;
	}

	public bool SeeInspector = true;

	public LayerMask myLayerMask;

	public Rigidbody rb;

	public AudioSource _audio;

	public List<Footsteps> listFootstepSystem = new List<Footsteps>();

	public List<compareCharacterMagnitude> listCompareMangnitude = new List<compareCharacterMagnitude>();

	private float _Timer;

	private Vector3 lastPos = new Vector3(0f, 0f, 0f);

	private Vector3 bodyVelocity = new Vector3(0f, 0f, 0f);

	private int currentFootstepType;

	private int currentSample;

	private characterMovement charMovement;

	private void Start()
	{
		charMovement = GetComponent<characterMovement>();
	}

	private void FixedUpdate()
	{
		if (Physics.Raycast(rb.transform.position + Vector3.up * 0.2f, -Vector3.up, out var hitInfo, 10f, myLayerMask))
		{
			if ((bool)charMovement && charMovement.isOnFloor)
			{
				playFootstep(hitInfo.transform.tag);
			}
			else if (!charMovement)
			{
				playFootstep(hitInfo.transform.tag);
			}
		}
	}

	private void playFootstep(string _tag)
	{
		if (CheckTag(_tag))
		{
			for (int i = 0; i < listCompareMangnitude.Count; i++)
			{
				if (bodyVelocity.magnitude > listCompareMangnitude[i].listCharacterMangnitude && !_audio.isPlaying)
				{
					float num = listCompareMangnitude[i].listTimeBetweenTwoStep;
					if ((bool)charMovement && charMovement.isRunning)
					{
						num = listCompareMangnitude[i].listTimeBetweenTwoStep - 0.2f;
					}
					if (_Timer == num)
					{
						playSound(ChooseSound());
						_Timer = 0f;
					}
					else
					{
						_Timer = Mathf.MoveTowards(_Timer, num, Time.deltaTime);
					}
				}
			}
		}
		bodyVelocity = (rb.position - lastPos) * 50f;
		lastPos = rb.position;
	}

	private bool CheckTag(string _tag)
	{
		for (int i = 0; i < listFootstepSystem.Count; i++)
		{
			if (_tag == listFootstepSystem[i].MaterialTag)
			{
				currentFootstepType = i;
				return true;
			}
		}
		return false;
	}

	private int ChooseSound()
	{
		currentSample++;
		currentSample %= listFootstepSystem[currentFootstepType].footstepSamples.Count;
		return currentSample;
	}

	private void playSound(int newSample)
	{
		if (listFootstepSystem[currentFootstepType].footstepSamples[newSample] != null)
		{
			_audio.clip = listFootstepSystem[currentFootstepType].footstepSamples[newSample];
			int num = UnityEngine.Random.Range(-5, 6);
			_audio.pitch = 1f + (float)num * 0.01f;
			int num2 = UnityEngine.Random.Range(0, 9);
			_audio.time = (float)num2 * 0.001f;
			_audio.Play();
		}
	}
}
