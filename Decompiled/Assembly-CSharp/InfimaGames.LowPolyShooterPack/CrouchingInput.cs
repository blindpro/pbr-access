using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class CrouchingInput : MonoBehaviour
{
	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The character's MovementBehaviour component.")]
	[SerializeField]
	private MovementBehaviour movementBehaviour;

	[Tooltip("If true, the crouch button has to be held to keep crouching.")]
	[SerializeField]
	private bool holdToCrouch;

	private bool holding;

	private void Update()
	{
		if (holdToCrouch)
		{
			movementBehaviour.TryCrouch(holding);
		}
	}

	public void Crouch(InputAction.CallbackContext context)
	{
		if (characterBehaviour == null || movementBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
		}
		else
		{
			if (((bool)characterBehaviour.GetComponent<CharacterMultiplayer>() && !characterBehaviour.GetComponent<CharacterMultiplayer>().isMainPlayer) || !characterBehaviour.IsCursorLocked())
			{
				return;
			}
			switch (context.phase)
			{
			case InputActionPhase.Started:
				holding = true;
				break;
			case InputActionPhase.Performed:
				if (!holdToCrouch)
				{
					movementBehaviour.TryToggleCrouch();
					characterBehaviour.GetComponent<InputSimulator>().RPC_Action(1);
				}
				break;
			case InputActionPhase.Canceled:
				holding = false;
				break;
			}
		}
	}

	public void OnTryCrouchSimulator(InputSimulator.Action context)
	{
		if (characterBehaviour == null || movementBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
		}
		else
		{
			if (!characterBehaviour.IsCursorLocked())
			{
				return;
			}
			switch (context.phase)
			{
			case InputActionPhase.Started:
				holding = true;
				break;
			case InputActionPhase.Performed:
				if (!holdToCrouch)
				{
					movementBehaviour.TryToggleCrouch();
				}
				break;
			case InputActionPhase.Canceled:
				holding = false;
				break;
			}
		}
	}
}
