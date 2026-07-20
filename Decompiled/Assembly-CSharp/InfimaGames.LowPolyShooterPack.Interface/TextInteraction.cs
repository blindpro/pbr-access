using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

[RequireComponent(typeof(Animator))]
public class TextInteraction : Element
{
	[Tooltip("Text that gets modified when looking at something to pick up.")]
	[SerializeField]
	private TextMeshProUGUI textToModify;

	[Tooltip("Name of the boolean to set when changing state.")]
	[SerializeField]
	private string stateName = "Visible";

	private Animator animator;

	private InteractorBehaviour interactorBehaviour;

	protected override void Awake()
	{
		base.Awake();
		animator = GetComponent<Animator>();
	}

	protected override void Tick()
	{
		if ((object)interactorBehaviour == null)
		{
			interactorBehaviour = characterBehaviour.GetComponentInChildren<InteractorBehaviour>();
		}
		if (!(interactorBehaviour != null) || !interactorBehaviour.CanInteract())
		{
			return;
		}
		Interactable interactable = interactorBehaviour.GetInteractable();
		if (interactable != null)
		{
			animator.SetBool(stateName, value: true);
			if (textToModify != null)
			{
				textToModify.text = interactable.GetInteractionText().ToUpper();
			}
		}
		else
		{
			animator.SetBool(stateName, value: false);
		}
	}
}
