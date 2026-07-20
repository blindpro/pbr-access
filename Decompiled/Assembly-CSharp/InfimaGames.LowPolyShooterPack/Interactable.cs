using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class Interactable : MonoBehaviour
{
	[SerializeField]
	protected string interactionText;

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

	public abstract void Interact(GameObject actor = null);

	public virtual string GetInteractionText()
	{
		return interactionText;
	}
}
