using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Inventory : InventoryBehaviour
{
	public Material[] skins;

	private WeaponBehaviour[] weapons;

	private WeaponBehaviour equipped;

	private int equippedIndex = -1;

	public override void Init(int equippedAtStart = 5)
	{
		Restart(equippedAtStart);
	}

	public override void Restart(int equippedAtStart = 5)
	{
		if (weapons == null)
		{
			weapons = GetComponentsInChildren<WeaponBehaviour>(includeInactive: true);
		}
		WeaponBehaviour[] array = weapons;
		for (int i = 0; i < array.Length; i++)
		{
			Weapon obj = (Weapon)array[i];
			obj.Restart();
			obj.gameObject.SetActive(value: false);
		}
		equippedIndex = -1;
		Equip(equippedAtStart);
	}

	public override WeaponBehaviour Equip(int index, bool restart = false)
	{
		if (weapons == null)
		{
			return equipped;
		}
		if (index > weapons.Length - 1)
		{
			return equipped;
		}
		if (equippedIndex == index)
		{
			return equipped;
		}
		if (equipped != null)
		{
			equipped.gameObject.SetActive(value: false);
		}
		equippedIndex = index;
		equipped = weapons[equippedIndex];
		equipped.gameObject.SetActive(value: true);
		equipped.UpdateAttachements(restart);
		return equipped;
	}

	public override int GetLastIndex()
	{
		int num = equippedIndex - 1;
		if (num < 0)
		{
			num = weapons.Length - 1;
		}
		return num;
	}

	public override int GetNextIndex()
	{
		int num = equippedIndex + 1;
		if (num > weapons.Length - 1)
		{
			num = 0;
		}
		return num;
	}

	public override WeaponBehaviour GetEquipped()
	{
		return equipped;
	}

	public override int GetEquippedIndex()
	{
		return equippedIndex;
	}

	public override WeaponBehaviour GetWeapon(int index)
	{
		if (index < 0)
		{
			return null;
		}
		if (weapons == null)
		{
			return null;
		}
		if (index > weapons.Length - 1)
		{
			return null;
		}
		return weapons[index];
	}

	public int GetWeaponId(string name)
	{
		if (weapons == null)
		{
			return -1;
		}
		for (int i = 0; i < weapons.Length; i++)
		{
			if (((Weapon)weapons[i]).GetWeaponName() == name)
			{
				return i;
			}
		}
		return -1;
	}
}
