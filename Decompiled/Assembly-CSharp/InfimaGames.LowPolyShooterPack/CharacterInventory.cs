using System.Collections.Generic;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterInventory : MonoBehaviour
{
	public List<PickupsManager.Item> inventory = new List<PickupsManager.Item>();

	public PickupsManager.Item weapon1;

	public PickupsManager.Item weapon1_silencer;

	public PickupsManager.Item weapon1_scope;

	public PickupsManager.Item weapon1_laser;

	public PickupsManager.Item weapon1_grip;

	public PickupsManager.Item weapon2;

	public PickupsManager.Item weapon2_silencer;

	public PickupsManager.Item weapon2_scope;

	public PickupsManager.Item weapon2_laser;

	public PickupsManager.Item weapon2_grip;

	public PickupsManager.Item vest;

	public PickupsManager.Item bag;

	public PickupsManager.Item helmet;

	private PickupsManager pickupsManager;

	private ItemsCollectionsSync itemsCollections;

	private int currentWeapon;

	private void Awake()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		itemsCollections = GetComponent<ItemsCollectionsSync>();
	}

	private void Start()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		itemsCollections = GetComponent<ItemsCollectionsSync>();
	}

	private void Update()
	{
	}

	public void OnMatchStarted()
	{
	}

	public void OnMatchFinished()
	{
	}

	public void OnDead()
	{
	}

	public void Apply()
	{
		Character component = GetComponent<Character>();
		if (!component)
		{
			return;
		}
		PickupsManager.Item item = weapon1;
		if (this.weapon2 == null)
		{
			currentWeapon = 0;
		}
		if (currentWeapon == 1 && this.weapon2 != null)
		{
			item = this.weapon2;
		}
		Debug.Log(item.name + " current weapon " + currentWeapon + " " + component.name);
		component.OnSetInventoryWeapon(item.name, cursorLockedInclude: true);
		Inventory inventory = (Inventory)component.GetInventory();
		if ((bool)inventory)
		{
			int weaponId = inventory.GetWeaponId(weapon1.name);
			int index = -1;
			if (this.weapon2 != null)
			{
				index = inventory.GetWeaponId(this.weapon2.name);
			}
			Weapon weapon = (Weapon)inventory.GetWeapon(weaponId);
			Weapon weapon2 = (Weapon)inventory.GetWeapon(index);
			if ((bool)weapon)
			{
				WeaponAttachmentManager weaponAttachmentManager = (WeaponAttachmentManager)weapon.GetAttachmentManager();
				if ((bool)weaponAttachmentManager)
				{
					if (weapon1_silencer == null)
					{
						weaponAttachmentManager.muzzleIndex = 0;
					}
					else
					{
						weaponAttachmentManager.muzzleIndex = weapon1_silencer.id;
					}
					if (weapon1_scope == null)
					{
						weaponAttachmentManager.scopeIndex = -1;
					}
					else
					{
						weaponAttachmentManager.scopeIndex = weapon1_scope.id;
					}
					if (weapon1_grip == null)
					{
						weaponAttachmentManager.gripIndex = -1;
					}
					else
					{
						weaponAttachmentManager.gripIndex = weapon1_grip.id;
					}
					if (weapon1_laser == null)
					{
						weaponAttachmentManager.laserIndex = -1;
					}
					else
					{
						weaponAttachmentManager.laserIndex = weapon1_laser.id;
					}
					weaponAttachmentManager.UpdateWeapon();
				}
			}
			if ((bool)weapon2)
			{
				WeaponAttachmentManager weaponAttachmentManager2 = (WeaponAttachmentManager)weapon2.GetAttachmentManager();
				if ((bool)weaponAttachmentManager2)
				{
					if (weapon2_silencer == null)
					{
						weaponAttachmentManager2.muzzleIndex = 0;
					}
					else
					{
						weaponAttachmentManager2.muzzleIndex = weapon2_silencer.id;
					}
					if (weapon2_scope == null)
					{
						weaponAttachmentManager2.scopeIndex = -1;
					}
					else
					{
						weaponAttachmentManager2.scopeIndex = weapon2_scope.id;
					}
					if (weapon2_grip == null)
					{
						weaponAttachmentManager2.gripIndex = -1;
					}
					else
					{
						weaponAttachmentManager2.gripIndex = weapon2_grip.id;
					}
					if (weapon2_laser == null)
					{
						weaponAttachmentManager2.laserIndex = -1;
					}
					else
					{
						weaponAttachmentManager2.laserIndex = weapon2_laser.id;
					}
					weaponAttachmentManager2.UpdateWeapon();
				}
			}
		}
		string text = "";
		if (vest != null)
		{
			text = vest.objName;
		}
		SetVest(text);
		string text2 = "";
		if (bag != null)
		{
			text2 = bag.objName;
		}
		SetBag(text2);
		string text3 = "";
		if (helmet != null)
		{
			text3 = helmet.objName;
		}
		SetHelmet(text3);
	}

	public GameObject GetCollectionActiveObj(string collection_name)
	{
		if (itemsCollections == null)
		{
			return null;
		}
		ItemsCollectionsSync.ItemsCollection collection = itemsCollections.GetCollection(collection_name);
		if (collection == null)
		{
			Debug.LogWarning("GetCollectionActiveObj collection null " + collection_name);
			return null;
		}
		foreach (GameObject item in collection.items)
		{
			if ((bool)item && item.activeSelf)
			{
				return item;
			}
		}
		return null;
	}

	public void SetCollectionActiveObj(string collection_name, string obj_name)
	{
		if (itemsCollections == null)
		{
			return;
		}
		ItemsCollectionsSync.ItemsCollection collection = itemsCollections.GetCollection(collection_name);
		if (collection == null)
		{
			Debug.LogWarning("SetCollectionActiveObj collection null " + collection_name + " " + obj_name);
			return;
		}
		foreach (GameObject item in collection.items)
		{
			if ((bool)item)
			{
				item.SetActive(item.name == obj_name);
			}
		}
		GetComponent<ItemsCollectionsSync>().mainPlayerChanged = true;
	}

	public bool IsFemaleBody()
	{
		GameObject collectionActiveObj = GetCollectionActiveObj("body");
		if ((bool)collectionActiveObj && collectionActiveObj.name.ToLower().Contains("female"))
		{
			return true;
		}
		return false;
	}

	public void SetBag(string bag_name)
	{
		SetCollectionActiveObj("backbag", bag_name);
	}

	public void SetHelmet(string helmet_name)
	{
		SetCollectionActiveObj("hat", helmet_name);
	}

	public void SetVest(string vest_name)
	{
		if (IsFemaleBody())
		{
			vest_name.Replace("Male", "Female");
		}
		SetCollectionActiveObj("armor", vest_name);
	}

	public void Restart()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		weapon1 = pickupsManager.GetItem("Handgun 01");
		weapon2 = null;
		weapon1_silencer = null;
		weapon1_scope = null;
		weapon1_laser = null;
		weapon1_grip = null;
		weapon2_silencer = null;
		weapon2_scope = null;
		weapon2_laser = null;
		weapon2_grip = null;
		vest = null;
		helmet = null;
		bag = pickupsManager.GetItem("Bag A (Lv1)");
		inventory.Clear();
		currentWeapon = 0;
		Apply();
	}

	public bool AddInventoryItem(PickupsManager.Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (inventory.Count >= bag.capacity)
		{
			if ((bool)pickupsManager)
			{
				pickupsManager.OnShowBagFullMsg();
			}
			Debug.LogWarning("bag full");
			return false;
		}
		inventory.Add(item);
		return true;
	}

	public bool IsBagFull()
	{
		if (inventory == null || bag == null)
		{
			return false;
		}
		return inventory.Count >= bag.capacity;
	}

	public bool RemoveInventoryItem(PickupsManager.Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (inventory.Contains(item))
		{
			inventory.Remove(item);
		}
		return true;
	}

	public void SetBag(PickupsManager.Item item)
	{
		bag = item;
		if (bag.capacity < inventory.Count)
		{
			inventory.RemoveRange(bag.capacity, inventory.Count - bag.capacity);
		}
	}

	public bool UseHealthItem(PickupsManager.Item item)
	{
		CharacterMultiplayer component = GetComponent<CharacterMultiplayer>();
		if (component.isHealing)
		{
			return false;
		}
		component.isHealing = true;
		component.healing_add = (byte)item.value;
		GameManager.Instance.usedHealths++;
		return true;
	}

	public bool UseAmmoItem(PickupsManager.Item item, bool isWeapon1)
	{
		PickupsManager.Item item2 = weapon1;
		if (!isWeapon1)
		{
			item2 = weapon2;
		}
		if (item2 == null)
		{
			return false;
		}
		if ((item2.name.Contains("SMG") || item2.name.Contains("Handgun")) && item.type != PickupsManager.ItemType.ammo_smg_gun)
		{
			return false;
		}
		if (item2.name.Contains("Sniper") && item.type != PickupsManager.ItemType.ammo_sniper)
		{
			return false;
		}
		if (item2.name.Contains("Assault") && item.type != PickupsManager.ItemType.ammo_assault)
		{
			return false;
		}
		if (item2.name.Contains("Shotgun") && item.type != PickupsManager.ItemType.ammo_shotgun)
		{
			return false;
		}
		if ((item2.name.Contains("Rocket Launcher") || item2.name.Contains("Grenade Launcher")) && item.type != PickupsManager.ItemType.ammo_grenades_launchers)
		{
			return false;
		}
		Inventory inventory = (Inventory)GetComponent<Character>().GetInventory();
		if ((bool)inventory)
		{
			int weaponId = inventory.GetWeaponId(item2.name);
			if (weaponId >= 0)
			{
				Weapon weapon = (Weapon)inventory.GetWeapon(weaponId);
				if ((bool)weapon)
				{
					weapon.AddMags(item.value);
				}
			}
		}
		return true;
	}

	public bool UseGrenadeItem(PickupsManager.Item item)
	{
		GetComponent<Character>().AddGrenades(2);
		return true;
	}

	public bool UseFuelItem(PickupsManager.Item item)
	{
		return true;
	}

	public void SetCurrentWeapon(int current)
	{
		if (weapon2 != null)
		{
			currentWeapon = current;
			Apply();
		}
	}

	public int GetCurrentWeapon()
	{
		return currentWeapon;
	}
}
