using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class Interactor : InteractorBehaviour
{
	[Header("References")]
	[Tooltip("Used to determine where to trace the interaction from, and what direction it should go in.")]
	[SerializeField]
	private Transform interactor;

	[Header("Settings")]
	[Tooltip("Mask used to trace for interactions.")]
	[SerializeField]
	private LayerMask mask;

	[Tooltip("Radius of the trace.")]
	[SerializeField]
	private float radius = 1f;

	[Tooltip("Maximum interaction distance.")]
	[SerializeField]
	private float maxDistance = 5f;

	private RaycastHit hitResult;

	private Interactable interactable;

	protected override void Update()
	{
		if (Physics.SphereCast(interactor.position, radius, interactor.forward, out hitResult, maxDistance, mask))
		{
			if (hitResult.collider != null)
			{
				interactable = hitResult.collider.GetComponent<Interactable>();
			}
			else
			{
				interactable = null;
			}
		}
		else
		{
			interactable = null;
		}
	}

	public void TryInteract(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed && CanInteract() && interactable != null)
		{
			interactable.Interact(base.gameObject);
		}
	}

	public override bool CanInteract()
	{
		return true;
	}

	public override RaycastHit GetHitResult()
	{
		return hitResult;
	}

	public override Interactable GetInteractable()
	{
		return interactable;
	}
}
