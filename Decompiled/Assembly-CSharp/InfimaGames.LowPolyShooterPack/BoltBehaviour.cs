using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class BoltBehaviour : StateMachineBehaviour
{
	private CharacterBehaviour playerCharacter;

	private InventoryBehaviour playerInventoryBehaviour;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (playerCharacter == null)
		{
			playerCharacter = animator.GetComponentInParent<Character>();
			if ((object)playerInventoryBehaviour == null)
			{
				playerInventoryBehaviour = playerCharacter.GetInventory();
			}
		}
		playerInventoryBehaviour.GetEquipped()?.gameObject.GetComponent<Animator>().Play("Bolt Action");
	}
}
