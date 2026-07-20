using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class TSLightOptiInit : MonoBehaviour
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
		TSLightOpti[] array = Object.FindObjectsOfType<TSLightOpti>();
		foreach (TSLightOpti obj in array)
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
