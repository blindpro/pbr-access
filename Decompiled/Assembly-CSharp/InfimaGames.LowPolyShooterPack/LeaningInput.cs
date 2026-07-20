using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class LeaningInput : MonoBehaviour
{
	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	private float leaningInput;

	private bool isLeaning;

	private void Update()
	{
		isLeaning = leaningInput != 0f;
		characterAnimator.SetFloat(AHashes.LeaningInput, leaningInput);
		characterAnimator.SetBool(AHashes.Leaning, isLeaning);
	}

	public void Lean(InputAction.CallbackContext context)
	{
		if (!characterBehaviour.GetComponent<CharacterMultiplayer>() || characterBehaviour.GetComponent<CharacterMultiplayer>().isMainPlayer)
		{
			if (!characterBehaviour.IsCursorLocked())
			{
				leaningInput = 0f;
			}
			else
			{
				leaningInput = context.ReadValue<float>();
			}
		}
	}
}
