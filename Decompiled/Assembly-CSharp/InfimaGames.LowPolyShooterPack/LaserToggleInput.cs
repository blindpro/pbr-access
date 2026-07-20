using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class LaserToggleInput : MonoBehaviour
{
	[Tooltip("The character's Animator component.")]
	[SerializeField]
	private Animator animator;

	[Tooltip("The character's InventoryBehaviour component.")]
	[SerializeField]
	private InventoryBehaviour inventoryBehaviour;

	private LaserBehaviour laserBehaviour;

	private bool wasAiming;

	private bool wasRunning;

	private void Update()
	{
		if (animator == null || inventoryBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		WeaponBehaviour equipped = inventoryBehaviour.GetEquipped();
		if (equipped == null)
		{
			return;
		}
		laserBehaviour = equipped.GetAttachmentManager().GetEquippedLaser();
		if (laserBehaviour == null)
		{
			return;
		}
		bool flag = animator.GetBool(AHashes.Aim);
		bool flag2 = animator.GetBool(AHashes.Running);
		if (flag && !wasAiming)
		{
			if (laserBehaviour.GetTurnOffWhileAiming())
			{
				laserBehaviour.Hide();
			}
		}
		else if (!flag && wasAiming && laserBehaviour.GetTurnOffWhileAiming())
		{
			laserBehaviour.Reapply();
		}
		if (flag2 && !wasRunning)
		{
			if (laserBehaviour.GetTurnOffWhileRunning())
			{
				laserBehaviour.Hide();
			}
		}
		else if (!flag2 && wasRunning && laserBehaviour.GetTurnOffWhileRunning())
		{
			laserBehaviour.Reapply();
		}
		wasAiming = flag;
		wasRunning = flag2;
	}

	public void Input(InputAction.CallbackContext context)
	{
		if ((!animator.transform.parent.GetComponent<CharacterMultiplayer>() || animator.transform.parent.GetComponent<CharacterMultiplayer>().isMainPlayer) && context.phase == InputActionPhase.Performed)
		{
			Toggle();
		}
	}

	private void Toggle()
	{
		if (!(laserBehaviour == null))
		{
			laserBehaviour.Toggle();
		}
	}
}
