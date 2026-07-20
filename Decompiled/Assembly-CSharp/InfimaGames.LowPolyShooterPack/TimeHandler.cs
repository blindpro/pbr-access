using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class TimeHandler : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Value the time scale gets updated by every time.")]
	[SerializeField]
	private float increment = 0.1f;

	private bool paused;

	private float current = 1f;

	private CharacterMultiplayer characterMultiplayer;

	private void Awake()
	{
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
	}

	private void Scale()
	{
		if (characterMultiplayer.isMainPlayer)
		{
			Time.timeScale = current;
		}
	}

	private void Change(float value = 1f)
	{
		current = value;
		Scale();
	}

	private void Increase(float value = 1f)
	{
		Change(Mathf.Clamp01(current + value));
	}

	private void Pause()
	{
		if (characterMultiplayer.isMainPlayer)
		{
			paused = true;
			Time.timeScale = 0f;
		}
	}

	private void Toggle()
	{
		if (characterMultiplayer.isMainPlayer)
		{
			if (paused)
			{
				Unpause();
			}
			else
			{
				Pause();
			}
		}
	}

	private void Unpause()
	{
		if (characterMultiplayer.isMainPlayer)
		{
			paused = false;
			Change(current);
		}
	}

	public virtual void OnIncrease(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && context.phase == InputActionPhase.Performed)
		{
			Increase(increment);
		}
	}

	public virtual void OnDecrease(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && context.phase == InputActionPhase.Performed)
		{
			Increase(0f - increment);
		}
	}

	public virtual void OnToggle(InputAction.CallbackContext context)
	{
		if (characterMultiplayer.isMainPlayer && context.phase == InputActionPhase.Performed)
		{
			Toggle();
		}
	}
}
