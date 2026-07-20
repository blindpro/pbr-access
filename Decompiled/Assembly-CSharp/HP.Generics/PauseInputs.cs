using System.Collections;
using UnityEngine;

namespace HP.Generics;

public class PauseInputs : MonoBehaviour
{
	public KeyCode[] btnsPause = new KeyCode[2];

	private ConditionsToPauseGame conditions;

	private bool isButtonAllowed = true;

	private void Awake()
	{
		conditions = GetComponent<ConditionsToPauseGame>();
	}

	private void Update()
	{
		for (int i = 0; i < btnsPause.Length; i++)
		{
			if (Input.GetKeyDown(btnsPause[i]) && isButtonAllowed)
			{
				StartCoroutine(CheckConditionRoutine());
			}
		}
	}

	private IEnumerator CheckConditionRoutine()
	{
		isButtonAllowed = false;
		conditions.isProcessDone = false;
		conditions.StartCoroutine(conditions.IsPauseAllowedRoutine());
		yield return new WaitUntil(() => conditions.isProcessDone);
		UpdatePause();
		isButtonAllowed = true;
		yield return null;
	}

	private void UpdatePause()
	{
		PauseManager.instance.Bool_IsGamePaused = !PauseManager.instance.Bool_IsGamePaused;
		if (PauseManager.instance.Bool_IsGamePaused)
		{
			PauseManager.instance.PauseGame();
		}
		else
		{
			PauseManager.instance.UnpauseGame();
		}
	}
}
