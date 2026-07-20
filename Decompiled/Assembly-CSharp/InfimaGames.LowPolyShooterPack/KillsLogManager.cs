using System;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class KillsLogManager : MonoBehaviour
{
	[Serializable]
	public class Line
	{
		public Text shooter;

		public Text dead;

		public Image weapon;

		public CanvasGroup canvasGroup;
	}

	public Line[] lines;

	public CanvasGroup canvasGroup;

	public float showDuration = 10f;

	private float showTimer;

	private void Start()
	{
		Restart();
	}

	private void Update()
	{
		showTimer -= Time.deltaTime;
		if (showTimer < 0f)
		{
			showTimer = 0f;
		}
		float num = showDuration / 3f;
		if (showTimer < num)
		{
			canvasGroup.alpha = Mathf.Lerp(1f, 0f, 1f - showTimer / num);
			if (canvasGroup.alpha <= 0f && lines[0].shooter.text != "")
			{
				Restart();
			}
		}
	}

	public void AddKillLog(string shooter, string dead, Sprite weapon)
	{
		for (int num = lines.Length - 1; num > 0; num--)
		{
			CopyLine(lines[num - 1], lines[num]);
		}
		lines[0].shooter.text = shooter;
		lines[0].dead.text = dead;
		lines[0].weapon.sprite = null;
		lines[0].weapon.sprite = weapon;
		lines[0].weapon.enabled = lines[0].weapon.sprite != null;
		Debug.Log("added kill");
		canvasGroup.alpha = 1f;
		showTimer = showDuration;
	}

	private void CopyLine(Line from, Line to)
	{
		to.shooter.text = from.shooter.text;
		to.dead.text = from.dead.text;
		to.weapon.sprite = null;
		to.weapon.sprite = from.weapon.sprite;
		to.weapon.enabled = to.weapon.sprite != null;
	}

	public void OnMatchStarted()
	{
		Restart();
	}

	public void OnMatchFinished()
	{
		Restart();
	}

	private void Restart()
	{
		Line[] array = lines;
		foreach (Line obj in array)
		{
			obj.shooter.text = "";
			obj.dead.text = "";
			obj.weapon.sprite = null;
			obj.weapon.enabled = false;
		}
		canvasGroup.alpha = 0f;
		showTimer = showDuration;
		Debug.LogWarning("restart kills log");
	}
}
