using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class InventoryBehaviour : MonoBehaviour
{
	public bool tps_mode;

	public abstract int GetLastIndex();

	public abstract int GetNextIndex();

	public abstract WeaponBehaviour GetEquipped();

	public abstract int GetEquippedIndex();

	public abstract void Init(int equippedAtStart = 0);

	public abstract WeaponBehaviour Equip(int index, bool restart = false);

	public abstract WeaponBehaviour GetWeapon(int index);

	public abstract void Restart(int equippedAtStart = 0);
}
