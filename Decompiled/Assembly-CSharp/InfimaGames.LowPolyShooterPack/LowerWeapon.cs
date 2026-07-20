using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class LowerWeapon : MonoBehaviour
{
	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator characterAnimator;

	[Tooltip("A WallAvoidance component is required so we can check if the character is facing a wall and lower the weapon automatically. If there's no such component assigned, this will never happen.")]
	[SerializeField]
	private WallAvoidance wallAvoidance;

	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

	[Tooltip("The character's CharacterBehaviour component.")]
	[SerializeField]
	private CharacterBehaviour characterBehaviour;

	[Tooltip("If true, the lowered state is stopped when the character starts firing.")]
	[SerializeField]
	private bool stopWhileFiring = true;

	private bool lowered;

	private bool loweredPressed;

	private void Update()
	{
		if (characterAnimator == null || characterBehaviour == null || inventoryBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		lowered = (loweredPressed || (wallAvoidance != null && wallAvoidance.HasWall)) && !characterBehaviour.IsAiming() && !characterBehaviour.IsRunning() && !characterBehaviour.IsInspecting() && !characterBehaviour.IsHolstered();
		if (stopWhileFiring && characterBehaviour.IsHoldingButtonFire())
		{
			lowered = false;
		}
		ItemAnimationDataBehaviour component = inventoryBehaviour.GetEquipped().GetComponent<ItemAnimationDataBehaviour>();
		if (component == null)
		{
			lowered = false;
		}
		else if (component.GetLowerData() == null)
		{
			lowered = false;
		}
		characterAnimator.SetBool(AHashes.Lowered, lowered);
	}

	public bool IsLowered()
	{
		return lowered;
	}

	public void Lower(InputAction.CallbackContext context)
	{
		if (characterBehaviour.IsCursorLocked() && !characterBehaviour.IsAiming() && !characterBehaviour.IsInspecting() && !characterBehaviour.IsRunning() && !characterBehaviour.IsHolstered() && context.phase == InputActionPhase.Performed)
		{
			loweredPressed = !loweredPressed;
		}
	}
}
