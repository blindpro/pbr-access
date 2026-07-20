using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class InteractorBehaviour : MonoBehaviour
{
	protected virtual void Awake()
	{
	}

	protected virtual void Start()
	{
	}

	protected virtual void Update()
	{
	}

	protected virtual void FixedUpdate()
	{
	}

	protected virtual void LateUpdate()
	{
	}

	public abstract bool CanInteract();

	public abstract RaycastHit GetHitResult();

	public abstract Interactable GetInteractable();
}
