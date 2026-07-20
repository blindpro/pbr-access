using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class TsAudioDistanceInit : MonoBehaviour
{
	public UnityEvent WaitForPlayerEvent;

	private bool wait = true;

	private void Start()
	{
		StartCoroutine(InitRoutine());
	}

	private IEnumerator InitRoutine()
	{
		while (wait)
		{
			WaitForPlayerEvent?.Invoke();
			yield return null;
		}
		TSAudioDistance[] array = Object.FindObjectsOfType<TSAudioDistance>();
		Debug.Log("TSAudioDistance: " + array.Length);
		TSAudioDistance[] array2 = array;
		foreach (TSAudioDistance obj in array2)
		{
			obj.StartCoroutine(obj.InitRoutine());
		}
		yield return null;
	}

	public void WaitForPlayer()
	{
		wait = false;
	}

	public void WaitForTwoPlayers()
	{
		if (Object.FindObjectsOfType<TSCharacterTag>().Length == 2)
		{
			wait = false;
		}
	}
}
