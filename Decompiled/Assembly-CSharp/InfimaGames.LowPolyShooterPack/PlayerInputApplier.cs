using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputApplier : MonoBehaviour
{
	private PlayerInput playerInput;

	private void Awake()
	{
		playerInput = GetComponent<PlayerInput>();
		if (SharedInputManager.Instance != null)
		{
			SharedInputManager.Instance.ApplyTo(playerInput);
		}
	}

	private void OnEnable()
	{
		if (SharedInputManager.Instance != null)
		{
			SharedInputManager.Instance.OnBindingsChanged += HandleBindingsChanged;
		}
	}

	private void OnDisable()
	{
		if (SharedInputManager.Instance != null)
		{
			SharedInputManager.Instance.OnBindingsChanged -= HandleBindingsChanged;
		}
	}

	private void HandleBindingsChanged()
	{
		if (SharedInputManager.Instance != null)
		{
			SharedInputManager.Instance.ApplyTo(playerInput);
		}
	}
}
