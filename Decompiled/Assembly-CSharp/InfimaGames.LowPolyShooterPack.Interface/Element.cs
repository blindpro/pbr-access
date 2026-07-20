using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public abstract class Element : MonoBehaviour
{
	protected IGameModeService gameModeService;

	protected CharacterBehaviour characterBehaviour;

	protected InventoryBehaviour inventoryBehaviour;

	protected WeaponBehaviour equippedWeaponBehaviour;

	protected virtual void Awake()
	{
		gameModeService = ServiceLocator.Current.Get<IGameModeService>();
		characterBehaviour = CharacterMultiplayer.GetMainPlayer()?.GetComponent<Character>();
		inventoryBehaviour = characterBehaviour?.GetInventory();
	}

	private void Update()
	{
		characterBehaviour = CharacterMultiplayer.GetMainPlayer()?.GetComponent<Character>();
		if (!(characterBehaviour == null))
		{
			inventoryBehaviour = characterBehaviour.GetInventory();
			if (!object.Equals(inventoryBehaviour, null))
			{
				equippedWeaponBehaviour = inventoryBehaviour.GetEquipped();
				Tick();
			}
		}
	}

	protected virtual void Tick()
	{
	}
}
