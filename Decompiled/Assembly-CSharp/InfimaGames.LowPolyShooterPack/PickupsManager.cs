using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class PickupsManager : MonoBehaviour
{
	[Serializable]
	public enum ItemType
	{
		money,
		health,
		ammo_sniper,
		ammo_smg_gun,
		ammo_assault,
		ammo_grenades_launchers,
		ammo_shotgun,
		grenade,
		weapon,
		silencer_attachment,
		scope_attachment,
		grip_attachment,
		laser_attachment,
		helmet,
		bag,
		vest,
		fuel
	}

	[Serializable]
	public class Item
	{
		public int id;

		public string name;

		public string description;

		public string short_description;

		public Sprite image;

		public int level;

		public int value;

		public int capacity;

		public ItemType type;

		public string objName;
	}

	public Text pickUI_full;

	public Text inventory_bag_full;

	public Text pickUI_desc;

	public Text pickUI_lv;

	public Image pickUI_icon;

	public RectTransform pickUI;

	public bool pickUI_visible;

	public AmmoBox pickAmmoBox;

	public Item pickItem;

	public Vector3 pickConvasFpsPos;

	public Vector3 pickConvasTpsPos;

	public Item[] items;

	public Transform itemsObjectsContainer;

	public float pickRange = 1.5f;

	public GameObject ammoBoxPrefab;

	public LayerMask pickLayerMask;

	public List<AmmoBox> ammoBoxes = new List<AmmoBox>();

	public List<AmmoBox> ammoBoxesAchievable = new List<AmmoBox>();

	public Transform pickupsList;

	public Transform myInventoryList;

	public GameObject listRowPrefab;

	private List<InventoryListItem> pickupsListItems = new List<InventoryListItem>();

	private List<InventoryListItem> myInventoryListItems = new List<InventoryListItem>();

	public Image weapon1;

	public Image weapon1_empty;

	public Image weapon1_frame;

	public Text weapon1_name;

	public Text weapon1_ammo;

	public Text weapon1_lv;

	public Image weapon1_silencer;

	public Image weapon1_silencer_empty;

	public Image weapon1_silencer_frame;

	public Image weapon1_scope;

	public Image weapon1_scope_empty;

	public Image weapon1_scope_frame;

	public Image weapon1_grip;

	public Image weapon1_grip_empty;

	public Image weapon1_grip_frame;

	public Image weapon1_laser;

	public Image weapon1_laser_empty;

	public Image weapon1_laser_frame;

	public Image weapon2;

	public Image weapon2_empty;

	public Image weapon2_frame;

	public Text weapon2_name;

	public Text weapon2_ammo;

	public Text weapon2_lv;

	public Image weapon2_silencer;

	public Image weapon2_silencer_empty;

	public Image weapon2_silencer_frame;

	public Image weapon2_scope;

	public Image weapon2_scope_empty;

	public Image weapon2_scope_frame;

	public Image weapon2_grip;

	public Image weapon2_grip_empty;

	public Image weapon2_grip_frame;

	public Image weapon2_laser;

	public Image weapon2_laser_empty;

	public Image weapon2_laser_frame;

	public Image vest;

	public Image vest_empty;

	public Image vest_frame;

	public Text vest_health;

	public Text vest_level;

	public Image helmet;

	public Image helmet_empty;

	public Image helmet_frame;

	public Text helmet_health;

	public Text helmet_level;

	public Image bag;

	public Image bag_empty;

	public Image bag_frame;

	public Text bag_health;

	public Text bag_level;

	public Image inventory_frame;

	public Image pickups_frame;

	public Image drop_frame;

	public Image use_frame;

	public float ammoboxShowDistance = 30f;

	private ProgressionManager progressionManager;

	[SerializeField]
	private float interactCooldown = 0.2f;

	private bool canInteract = true;

	private int frameChunks = 5;

	private int currentChunk;

	private float updateInterval = 0.2f;

	private void Awake()
	{
		progressionManager = GetComponent<ProgressionManager>();
	}

	private void Start()
	{
		Statics.SetActive(pickUI.gameObject, active: false);
		Statics.SetActive(pickUI_full.gameObject, active: false);
		Statics.SetActive(inventory_bag_full.gameObject, active: false);
		StartCoroutine(UpdateAmmoBoxesWithInterval());
	}

	private void Update()
	{
	}

	public void OnMatchStarted()
	{
		foreach (AmmoBox ammoBox in ammoBoxes)
		{
			ammoBox.CreateRandomItems();
		}
	}

	public void OnMatchFinished()
	{
	}

	private void FillPickupsList()
	{
		InventoryListItem[] componentsInChildren = pickupsList.GetComponentsInChildren<InventoryListItem>();
		foreach (InventoryListItem inventoryListItem in componentsInChildren)
		{
			if ((bool)inventoryListItem)
			{
				UnityEngine.Object.Destroy(inventoryListItem.gameObject);
			}
		}
		foreach (InventoryListItem pickupsListItem in pickupsListItems)
		{
			if ((bool)pickupsListItem)
			{
				UnityEngine.Object.Destroy(pickupsListItem.gameObject);
			}
		}
		pickupsListItems.Clear();
		AmmoBox currentAmmoBox = GetCurrentAmmoBox();
		if (currentAmmoBox == null)
		{
			return;
		}
		progressionManager = GetComponent<ProgressionManager>();
		foreach (Item item in currentAmmoBox.items)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(listRowPrefab, pickupsList);
			if ((bool)gameObject)
			{
				InventoryListItem component = gameObject.GetComponent<InventoryListItem>();
				component.item = item;
				component.description.text = item.description;
				if (item.short_description != "")
				{
					component.description.text = item.short_description;
				}
				if (item.level > 0)
				{
					component.lv.text = "Lv " + item.level;
				}
				else
				{
					component.lv.text = "";
				}
				if (item.type == ItemType.weapon)
				{
					component.lv.text = "Lv " + progressionManager.GetWeaponGrade(item.short_description);
				}
				component.image.sprite = null;
				component.image.sprite = item.image;
				component.image.GetComponent<UIDragHandler>().item = component.item;
				component.image.GetComponent<UIDragHandler>().dragSource = UIDragHandler.DragSource.pickups;
				pickupsListItems.Add(component);
			}
		}
	}

	private void FillInventoryList(List<Item> characterInventory)
	{
		InventoryListItem[] componentsInChildren = myInventoryList.GetComponentsInChildren<InventoryListItem>();
		foreach (InventoryListItem inventoryListItem in componentsInChildren)
		{
			if ((bool)inventoryListItem)
			{
				UnityEngine.Object.Destroy(inventoryListItem.gameObject);
			}
		}
		foreach (InventoryListItem myInventoryListItem in myInventoryListItems)
		{
			if ((bool)myInventoryListItem)
			{
				UnityEngine.Object.Destroy(myInventoryListItem.gameObject);
			}
		}
		myInventoryListItems.Clear();
		if (characterInventory == null)
		{
			return;
		}
		foreach (Item item in characterInventory)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(listRowPrefab, myInventoryList);
			if ((bool)gameObject)
			{
				InventoryListItem component = gameObject.GetComponent<InventoryListItem>();
				component.item = item;
				component.lv.text = "";
				component.description.text = item.description;
				component.image.sprite = null;
				component.image.sprite = item.image;
				component.image.GetComponent<UIDragHandler>().item = component.item;
				component.image.GetComponent<UIDragHandler>().dragSource = UIDragHandler.DragSource.myInventory;
				myInventoryListItems.Add(component);
			}
		}
	}

	public void EnableListsItemsRaycast(bool enable)
	{
		foreach (InventoryListItem myInventoryListItem in myInventoryListItems)
		{
			if ((bool)myInventoryListItem)
			{
				CanvasGroup component = myInventoryListItem.GetComponent<CanvasGroup>();
				if ((bool)component)
				{
					component.interactable = enable;
					component.blocksRaycasts = enable;
				}
			}
		}
		foreach (InventoryListItem pickupsListItem in pickupsListItems)
		{
			if ((bool)pickupsListItem)
			{
				CanvasGroup component2 = pickupsListItem.GetComponent<CanvasGroup>();
				if ((bool)component2)
				{
					component2.interactable = enable;
					component2.blocksRaycasts = enable;
				}
			}
		}
	}

	public void OnBeginDrag(UIDragHandler dragHandler)
	{
		EnableListsItemsRaycast(enable: false);
		DisableAllOutlines();
		if (dragHandler.dragSource == UIDragHandler.DragSource.pickups)
		{
			if (dragHandler.item.type == ItemType.weapon)
			{
				weapon1_frame.enabled = true;
				weapon2_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.silencer_attachment)
			{
				weapon1_silencer_frame.enabled = true;
				weapon2_silencer_frame.enabled = true;
				inventory_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.scope_attachment)
			{
				weapon1_scope_frame.enabled = true;
				weapon2_scope_frame.enabled = true;
				inventory_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.grip_attachment)
			{
				weapon1_grip_frame.enabled = true;
				weapon2_grip_frame.enabled = true;
				inventory_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.laser_attachment)
			{
				weapon1_laser_frame.enabled = true;
				weapon2_laser_frame.enabled = true;
				inventory_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.helmet)
			{
				helmet_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.bag)
			{
				bag_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.vest)
			{
				vest_frame.enabled = true;
			}
			else
			{
				inventory_frame.enabled = true;
			}
			if (dragHandler.item.type.ToString().Contains("ammo"))
			{
				weapon1_frame.enabled = true;
				weapon2_frame.enabled = true;
			}
		}
		else if (dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
		{
			if (dragHandler.item.type == ItemType.silencer_attachment)
			{
				weapon1_silencer_frame.enabled = true;
				weapon2_silencer_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.scope_attachment)
			{
				weapon1_scope_frame.enabled = true;
				weapon2_scope_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.grip_attachment)
			{
				weapon1_grip_frame.enabled = true;
				weapon2_grip_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.laser_attachment)
			{
				weapon1_laser_frame.enabled = true;
				weapon2_laser_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else
			{
				drop_frame.enabled = true;
			}
			if (dragHandler.item.type.ToString().Contains("ammo"))
			{
				weapon1_frame.enabled = true;
				weapon2_frame.enabled = true;
			}
			if (dragHandler.item.type == ItemType.health || dragHandler.item.type == ItemType.grenade || dragHandler.item.type == ItemType.fuel)
			{
				use_frame.enabled = true;
			}
		}
		else
		{
			if (dragHandler.dragSource != UIDragHandler.DragSource.equipments)
			{
				return;
			}
			if (dragHandler.item.type == ItemType.weapon)
			{
				if (!dragHandler.isWeapon1)
				{
					drop_frame.enabled = true;
				}
			}
			else if (dragHandler.item.type == ItemType.silencer_attachment)
			{
				inventory_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.scope_attachment)
			{
				inventory_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.grip_attachment)
			{
				inventory_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.laser_attachment)
			{
				inventory_frame.enabled = true;
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type == ItemType.helmet)
			{
				drop_frame.enabled = true;
			}
			else if (dragHandler.item.type != ItemType.bag && dragHandler.item.type == ItemType.vest)
			{
				drop_frame.enabled = true;
			}
		}
	}

	public void Interact_PickCurrentItem(InputAction.CallbackContext context)
	{
		if (context.performed && canInteract)
		{
			StartCoroutine(HandleInteractCooldown());
		}
	}

	private IEnumerator HandleInteractCooldown()
	{
		canInteract = false;
		PickCurrentItem();
		yield return new WaitForSeconds(interactCooldown);
		canInteract = true;
	}

	public void PickCurrentItem()
	{
		AmmoBox ammoBox = pickAmmoBox;
		CharacterInventory characterInventory = null;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!mainPlayer || !ammoBox)
		{
			return;
		}
		Debug.Log("calling PickCurrentItem");
		characterInventory = mainPlayer.GetComponent<CharacterInventory>();
		if (!characterInventory)
		{
			Debug.LogError("characterInventory null in PickCurrentItem");
			return;
		}
		Item currentItem = ammoBox.currentItem;
		if (currentItem != null)
		{
			pick_item_from_box(characterInventory, ammoBox, currentItem, characterInventory.GetCurrentWeapon() == 0);
			ammoBox.currentItem = null;
			UpdateMainPlayer();
			ReadMainPlayer();
			EnableListsItemsRaycast(enable: true);
			DisableAllOutlines();
		}
		else
		{
			Debug.Log("calling PickCurrentItem item null");
		}
	}

	public void pick_item_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item, bool isWeapon1)
	{
		if (!currentAmmoBox.items.Contains(item))
		{
			return;
		}
		if (item.type != ItemType.weapon && item.type != ItemType.helmet && item.type != ItemType.bag && item.type != ItemType.vest)
		{
			if (item.type == ItemType.silencer_attachment)
			{
				if (isWeapon1)
				{
					pick_silencer1_from_box(characterInventory, currentAmmoBox, item);
				}
				else
				{
					pick_silencer2_from_box(characterInventory, currentAmmoBox, item);
				}
			}
			else if (item.type == ItemType.grip_attachment)
			{
				if (isWeapon1)
				{
					pick_grip1_from_box(characterInventory, currentAmmoBox, item);
				}
				else
				{
					pick_grip2_from_box(characterInventory, currentAmmoBox, item);
				}
			}
			else if (item.type == ItemType.laser_attachment)
			{
				if (isWeapon1)
				{
					pick_laser1_from_box(characterInventory, currentAmmoBox, item);
				}
				else
				{
					pick_laser2_from_box(characterInventory, currentAmmoBox, item);
				}
			}
			else if (item.type == ItemType.scope_attachment)
			{
				if (isWeapon1)
				{
					pick_scope1_from_box(characterInventory, currentAmmoBox, item);
				}
				else
				{
					pick_scope2_from_box(characterInventory, currentAmmoBox, item);
				}
			}
			else
			{
				pick_other_from_box_to_inventory(characterInventory, currentAmmoBox, item);
			}
		}
		else if (item.type == ItemType.helmet)
		{
			pick_helmet_from_box(characterInventory, currentAmmoBox, item);
		}
		else if (item.type == ItemType.vest)
		{
			pick_vest_from_box(characterInventory, currentAmmoBox, item);
		}
		else if (item.type == ItemType.bag)
		{
			pick_bag_from_box(characterInventory, currentAmmoBox, item);
		}
		else if (item.type == ItemType.weapon)
		{
			if (isWeapon1)
			{
				pick_weapon1_from_box(characterInventory, currentAmmoBox, item);
			}
			else
			{
				pick_weapon2_from_box(characterInventory, currentAmmoBox, item);
			}
		}
	}

	private bool pick_other_from_box_to_inventory(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		if (item.type != ItemType.weapon && item.type != ItemType.helmet && item.type != ItemType.bag && item.type != ItemType.vest && characterInventory.AddInventoryItem(item))
		{
			if ((bool)currentAmmoBox)
			{
				currentAmmoBox.RemoveItem(item);
			}
			UseGrenadeAuto();
			return true;
		}
		return false;
	}

	private void pick_helmet_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.helmet = currentAmmoBox.ReplaceItem(item, characterInventory.helmet);
	}

	private void pick_vest_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.vest = currentAmmoBox.ReplaceItem(item, characterInventory.vest);
	}

	private void pick_bag_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.SetBag(currentAmmoBox.ReplaceItem(item, characterInventory.bag));
	}

	private void pick_weapon1_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		if (characterInventory.weapon2 == null || characterInventory.weapon2 != item)
		{
			if (characterInventory.weapon1 != null && characterInventory.weapon2 == null)
			{
				characterInventory.weapon2 = characterInventory.weapon1;
				characterInventory.weapon1 = null;
			}
			characterInventory.weapon1 = currentAmmoBox.ReplaceItem(item, characterInventory.weapon1);
		}
	}

	private void pick_weapon2_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		if (characterInventory.weapon1 == null || characterInventory.weapon1 != item)
		{
			characterInventory.weapon2 = currentAmmoBox.ReplaceItem(item, characterInventory.weapon2);
		}
	}

	private void pick_silencer1_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon1_silencer = currentAmmoBox.ReplaceItem(item, characterInventory.weapon1_silencer);
	}

	private void pick_silencer2_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon2_silencer = currentAmmoBox.ReplaceItem(item, characterInventory.weapon2_silencer);
	}

	private void pick_scope1_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon1_scope = currentAmmoBox.ReplaceItem(item, characterInventory.weapon1_scope);
	}

	private void pick_scope2_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon2_scope = currentAmmoBox.ReplaceItem(item, characterInventory.weapon2_scope);
	}

	private void pick_grip1_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon1_grip = currentAmmoBox.ReplaceItem(item, characterInventory.weapon1_grip);
	}

	private void pick_grip2_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon2_grip = currentAmmoBox.ReplaceItem(item, characterInventory.weapon2_grip);
	}

	private void pick_laser1_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon1_laser = currentAmmoBox.ReplaceItem(item, characterInventory.weapon1_laser);
	}

	private void pick_laser2_from_box(CharacterInventory characterInventory, AmmoBox currentAmmoBox, Item item)
	{
		characterInventory.weapon2_laser = currentAmmoBox.ReplaceItem(item, characterInventory.weapon2_laser);
	}

	public void OnEndDrag(UIDragHandler dragHandler, Image dropZone)
	{
		AmmoBox currentAmmoBox = GetCurrentAmmoBox();
		CharacterInventory characterInventory = null;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			characterInventory = mainPlayer.GetComponent<CharacterInventory>();
			if (!characterInventory)
			{
				Debug.LogError("characterInventory null in OnEndDrag");
				return;
			}
		}
		if (dragHandler.item.type == ItemType.weapon)
		{
			if ((dropZone == weapon1 || dropZone == weapon1_empty) && weapon1_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups && dragHandler.item != characterInventory.weapon2)
			{
				pick_weapon1_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon2 || dropZone == weapon2_empty) && weapon2_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups && dragHandler.item != characterInventory.weapon1)
			{
				pick_weapon2_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				if (dragHandler.isWeapon1)
				{
					if (characterInventory.weapon2 != null)
					{
						characterInventory.weapon1 = characterInventory.weapon2;
						characterInventory.weapon2 = null;
						PlayDropSound();
					}
				}
				else
				{
					characterInventory.weapon2 = null;
					PlayDropSound();
				}
			}
		}
		if (dragHandler.item.type == ItemType.silencer_attachment)
		{
			if ((dropZone == weapon1_silencer || dropZone == weapon1_silencer_empty) && weapon1_silencer_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_silencer1_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon2_silencer || dropZone == weapon2_silencer_empty) && weapon2_silencer_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_silencer2_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon1_silencer || dropZone == weapon1_silencer_empty) && weapon1_silencer_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon1_silencer != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon1_silencer);
				}
				characterInventory.weapon1_silencer = dragHandler.item;
				PlayLootSound();
			}
			if ((dropZone == weapon2_silencer || dropZone == weapon2_silencer_empty) && weapon2_silencer_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon2_silencer != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon2_silencer);
				}
				characterInventory.weapon2_silencer = dragHandler.item;
				PlayLootSound();
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				if (dragHandler.isWeapon1)
				{
					characterInventory.weapon1_silencer = null;
					PlayDropSound();
				}
				else
				{
					characterInventory.weapon2_silencer = null;
					PlayDropSound();
				}
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayDropSound();
			}
		}
		if (dragHandler.item.type == ItemType.scope_attachment)
		{
			if ((dropZone == weapon1_scope || dropZone == weapon1_scope_empty) && weapon1_scope_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_scope1_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon2_scope || dropZone == weapon2_scope_empty) && weapon2_scope_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_scope2_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon1_scope || dropZone == weapon1_scope_empty) && weapon1_scope_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon1_scope != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon1_scope);
				}
				characterInventory.weapon1_scope = dragHandler.item;
				PlayLootSound();
			}
			if ((dropZone == weapon2_scope || dropZone == weapon2_scope_empty) && weapon2_scope_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon2_scope != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon2_scope);
				}
				characterInventory.weapon2_scope = dragHandler.item;
				PlayLootSound();
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				if (dragHandler.isWeapon1)
				{
					characterInventory.weapon1_scope = null;
					PlayDropSound();
				}
				else
				{
					characterInventory.weapon2_scope = null;
					PlayDropSound();
				}
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayDropSound();
			}
		}
		if (dragHandler.item.type == ItemType.grip_attachment)
		{
			if ((dropZone == weapon1_grip || dropZone == weapon1_grip_empty) && weapon1_grip_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_grip1_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon2_grip || dropZone == weapon2_grip_empty) && weapon2_grip_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_grip2_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon1_grip || dropZone == weapon1_grip_empty) && weapon1_grip_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon1_grip != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon1_grip);
				}
				characterInventory.weapon1_grip = dragHandler.item;
				PlayLootSound();
			}
			if ((dropZone == weapon2_grip || dropZone == weapon2_grip_empty) && weapon2_grip_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon2_grip != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon2_grip);
				}
				characterInventory.weapon2_grip = dragHandler.item;
				PlayLootSound();
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				if (dragHandler.isWeapon1)
				{
					characterInventory.weapon1_grip = null;
					PlayDropSound();
				}
				else
				{
					characterInventory.weapon2_grip = null;
					PlayDropSound();
				}
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayDropSound();
			}
		}
		if (dragHandler.item.type == ItemType.laser_attachment)
		{
			if ((dropZone == weapon1_laser || dropZone == weapon1_laser_empty) && weapon1_laser_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_laser1_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon2_laser || dropZone == weapon2_laser_empty) && weapon2_laser_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_laser2_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if ((dropZone == weapon1_laser || dropZone == weapon1_laser_empty) && weapon1_laser_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon1_laser != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon1_laser);
				}
				characterInventory.weapon1_laser = dragHandler.item;
				PlayLootSound();
			}
			if ((dropZone == weapon2_laser || dropZone == weapon2_laser_empty) && weapon2_laser_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				if (characterInventory.weapon2_laser != null)
				{
					characterInventory.AddInventoryItem(characterInventory.weapon2_laser);
				}
				characterInventory.weapon2_laser = dragHandler.item;
				PlayLootSound();
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				if (dragHandler.isWeapon1)
				{
					characterInventory.weapon1_laser = null;
					PlayDropSound();
				}
				else
				{
					characterInventory.weapon2_laser = null;
					PlayDropSound();
				}
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayDropSound();
			}
		}
		if (dragHandler.item.type == ItemType.helmet)
		{
			if ((dropZone == helmet || dropZone == helmet_empty) && helmet_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_helmet_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				characterInventory.helmet = null;
				PlayDropSound();
			}
		}
		if (dragHandler.item.type == ItemType.vest)
		{
			if ((dropZone == vest || dropZone == vest_empty) && vest_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_vest_from_box(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments)
			{
				characterInventory.vest = null;
				PlayDropSound();
			}
		}
		if (dragHandler.item.type == ItemType.bag && (dropZone == bag || dropZone == bag_empty) && bag_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
		{
			pick_bag_from_box(characterInventory, currentAmmoBox, dragHandler.item);
		}
		if (dragHandler.item.type != ItemType.weapon && dragHandler.item.type != ItemType.helmet && dragHandler.item.type != ItemType.bag && dragHandler.item.type != ItemType.vest)
		{
			if (dropZone.name == "ViewportMyInventory" && inventory_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.pickups)
			{
				pick_other_from_box_to_inventory(characterInventory, currentAmmoBox, dragHandler.item);
			}
			if (dropZone.name == "ViewportMyInventory" && inventory_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.equipments && characterInventory.AddInventoryItem(dragHandler.item))
			{
				if (dragHandler.item.type == ItemType.silencer_attachment && dragHandler.isWeapon1)
				{
					characterInventory.weapon1_silencer = null;
				}
				if (dragHandler.item.type == ItemType.scope_attachment && dragHandler.isWeapon1)
				{
					characterInventory.weapon1_scope = null;
				}
				if (dragHandler.item.type == ItemType.laser_attachment && dragHandler.isWeapon1)
				{
					characterInventory.weapon1_laser = null;
				}
				if (dragHandler.item.type == ItemType.grip_attachment && dragHandler.isWeapon1)
				{
					characterInventory.weapon1_grip = null;
				}
				if (dragHandler.item.type == ItemType.silencer_attachment && !dragHandler.isWeapon1)
				{
					characterInventory.weapon2_silencer = null;
				}
				if (dragHandler.item.type == ItemType.scope_attachment && !dragHandler.isWeapon1)
				{
					characterInventory.weapon2_scope = null;
				}
				if (dragHandler.item.type == ItemType.laser_attachment && !dragHandler.isWeapon1)
				{
					characterInventory.weapon2_laser = null;
				}
				if (dragHandler.item.type == ItemType.grip_attachment && !dragHandler.isWeapon1)
				{
					characterInventory.weapon2_grip = null;
				}
				PlayLootSound();
			}
			if (dropZone == drop_frame && drop_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory && !dragHandler.item.type.ToString().Contains("attachment"))
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayDropSound();
			}
			if (dropZone == use_frame && use_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory)
			{
				if (dragHandler.item.type == ItemType.health && characterInventory.UseHealthItem(dragHandler.item))
				{
					characterInventory.RemoveInventoryItem(dragHandler.item);
					PlayLootSound();
				}
				if (dragHandler.item.type == ItemType.grenade && characterInventory.UseGrenadeItem(dragHandler.item))
				{
					characterInventory.RemoveInventoryItem(dragHandler.item);
					PlayLootSound();
				}
				if (dragHandler.item.type == ItemType.fuel && characterInventory.UseFuelItem(dragHandler.item))
				{
					characterInventory.RemoveInventoryItem(dragHandler.item);
					PlayLootSound();
				}
			}
			if (dropZone == weapon1 && weapon1_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory && dragHandler.item.type.ToString().Contains("ammo") && characterInventory.UseAmmoItem(dragHandler.item, isWeapon1: true))
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayLootSound();
			}
			if (dropZone == weapon2 && weapon2_frame.enabled && dragHandler.dragSource == UIDragHandler.DragSource.myInventory && dragHandler.item.type.ToString().Contains("ammo") && characterInventory.UseAmmoItem(dragHandler.item, isWeapon1: false))
			{
				characterInventory.RemoveInventoryItem(dragHandler.item);
				PlayLootSound();
			}
		}
		UpdateMainPlayer();
		ReadMainPlayer();
		EnableListsItemsRaycast(enable: true);
		DisableAllOutlines();
	}

	public void UseHealthAuto()
	{
		Debug.Log("use health auto");
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			CharacterInventory component = mainPlayer.GetComponent<CharacterInventory>();
			if ((bool)component)
			{
				foreach (Item item in component.inventory)
				{
					if (item != null && item.type == ItemType.health && component.UseHealthItem(item))
					{
						component.RemoveInventoryItem(item);
						break;
					}
				}
			}
		}
		UpdateMainPlayer();
		ReadMainPlayer();
		EnableListsItemsRaycast(enable: true);
		DisableAllOutlines();
	}

	public void UseGrenadeAuto()
	{
		Debug.Log("use grenade auto");
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			CharacterInventory component = mainPlayer.GetComponent<CharacterInventory>();
			if ((bool)component)
			{
				foreach (Item item in component.inventory)
				{
					if (item != null && item.type == ItemType.grenade && component.UseGrenadeItem(item))
					{
						component.RemoveInventoryItem(item);
						break;
					}
				}
			}
		}
		UpdateMainPlayer();
		ReadMainPlayer();
		EnableListsItemsRaycast(enable: true);
		DisableAllOutlines();
	}

	public void UseAmmoAuto()
	{
		Debug.Log("use ammo auto");
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			CharacterInventory component = mainPlayer.GetComponent<CharacterInventory>();
			if ((bool)component)
			{
				foreach (Item item in component.inventory)
				{
					if (item != null && item.type.ToString().Contains("ammo"))
					{
						int currentWeapon = component.GetCurrentWeapon();
						if (component.UseAmmoItem(item, currentWeapon == 0))
						{
							component.RemoveInventoryItem(item);
							break;
						}
					}
				}
			}
		}
		UpdateMainPlayer();
		ReadMainPlayer();
		EnableListsItemsRaycast(enable: true);
		DisableAllOutlines();
	}

	public void PlayOpenBoxSound()
	{
	}

	private void PlayLootSound()
	{
		GetComponent<HitCursorsManager>().PlayLootSound();
	}

	private void PlayDropSound()
	{
		GetComponent<HitCursorsManager>().PlayDropSound();
	}

	private void UpdateMainPlayer()
	{
		Debug.Log("Pickable Manager UpdateMainPlayer");
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			CharacterInventory component = mainPlayer.GetComponent<CharacterInventory>();
			if ((bool)component)
			{
				component.Apply();
			}
		}
	}

	private void ReadMainPlayer()
	{
		Debug.Log("Pickable Manager ReadMainPlayer");
		progressionManager = GetComponent<ProgressionManager>();
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!mainPlayer)
		{
			return;
		}
		Character component = mainPlayer.GetComponent<Character>();
		CharacterInventory component2 = mainPlayer.GetComponent<CharacterInventory>();
		if (!component2)
		{
			return;
		}
		SetImage(component2.weapon1, weapon1, weapon1_empty, isWeapon1: true);
		SetImage(component2.weapon1_silencer, weapon1_silencer, weapon1_silencer_empty, isWeapon1: true);
		SetImage(component2.weapon1_scope, weapon1_scope, weapon1_scope_empty, isWeapon1: true);
		SetImage(component2.weapon1_grip, weapon1_grip, weapon1_grip_empty, isWeapon1: true);
		SetImage(component2.weapon1_laser, weapon1_laser, weapon1_laser_empty, isWeapon1: true);
		SetImage(component2.weapon2, this.weapon2, weapon2_empty);
		SetImage(component2.weapon2_silencer, weapon2_silencer, weapon2_silencer_empty);
		SetImage(component2.weapon2_scope, weapon2_scope, weapon2_scope_empty);
		SetImage(component2.weapon2_grip, weapon2_grip, weapon2_grip_empty);
		SetImage(component2.weapon2_laser, weapon2_laser, weapon2_laser_empty);
		SetImage(component2.vest, vest, vest_empty);
		SetImage(component2.helmet, helmet, helmet_empty);
		SetImage(component2.bag, bag, bag_empty);
		if (component2.bag != null)
		{
			bag_level.text = "Lv " + component2.bag.level;
			bag_health.text = component2.inventory.Count + "/" + component2.bag.capacity;
		}
		if (component2.helmet != null)
		{
			helmet_level.text = "Lv " + component2.helmet.level;
			helmet_health.text = component2.helmet.value + "/" + component2.helmet.capacity;
		}
		if (component2.vest != null)
		{
			vest_level.text = "Lv " + component2.vest.level;
			vest_health.text = component2.vest.value + "/" + component2.vest.capacity;
		}
		Inventory inventory = (Inventory)component.GetInventory();
		if (component2.weapon1 != null && (bool)inventory)
		{
			int weaponId = inventory.GetWeaponId(component2.weapon1.name);
			if (weaponId >= 0)
			{
				Weapon weapon = (Weapon)inventory.GetWeapon(weaponId);
				if ((bool)weapon)
				{
					weapon1_ammo.text = weapon.GetAmmunitionCurrent() + "/" + weapon.GetCurrentMags() * weapon.GetAmmunitionTotal();
				}
				weapon1_lv.text = "Lv " + progressionManager.GetWeaponGrade(component2.weapon1.short_description) + "/" + ProgressionManager.MaxWeaponGrade;
			}
		}
		if (component2.weapon2 != null && (bool)inventory)
		{
			int weaponId2 = inventory.GetWeaponId(component2.weapon2.name);
			if (weaponId2 >= 0)
			{
				Weapon weapon2 = (Weapon)inventory.GetWeapon(weaponId2);
				if ((bool)weapon2)
				{
					weapon2_ammo.text = weapon2.GetAmmunitionCurrent() + "/" + weapon2.GetCurrentMags() * weapon2.GetAmmunitionTotal();
				}
				weapon2_lv.text = "Lv " + progressionManager.GetWeaponGrade(component2.weapon2.short_description) + "/" + ProgressionManager.MaxWeaponGrade;
			}
		}
		if (component2.weapon1 != null)
		{
			weapon1_name.text = component2.weapon1.short_description;
		}
		else
		{
			weapon1_name.text = "";
		}
		if (component2.weapon2 != null)
		{
			weapon2_name.text = component2.weapon2.short_description;
		}
		else
		{
			weapon2_name.text = "";
		}
		FillInventoryList(component2.inventory);
		FillPickupsList();
	}

	private void SetImage(Item item, Image image, Image empty, bool isWeapon1 = false)
	{
		if (item != null)
		{
			image.gameObject.SetActive(value: true);
			empty.gameObject.SetActive(value: false);
			image.sprite = null;
			image.sprite = item.image;
			image.GetComponent<UIDragHandler>().item = item;
			image.GetComponent<UIDragHandler>().dragSource = UIDragHandler.DragSource.equipments;
			image.GetComponent<UIDragHandler>().isWeapon1 = isWeapon1;
		}
		else
		{
			image.gameObject.SetActive(value: false);
			empty.gameObject.SetActive(value: true);
		}
	}

	public void Init()
	{
		DisableAllOutlines();
		ReadMainPlayer();
	}

	private void DisableAllOutlines()
	{
		weapon1_frame.enabled = false;
		weapon1_silencer_frame.enabled = false;
		weapon1_grip_frame.enabled = false;
		weapon1_scope_frame.enabled = false;
		weapon1_laser_frame.enabled = false;
		weapon2_frame.enabled = false;
		weapon2_silencer_frame.enabled = false;
		weapon2_grip_frame.enabled = false;
		weapon2_scope_frame.enabled = false;
		weapon2_laser_frame.enabled = false;
		helmet_frame.enabled = false;
		bag_frame.enabled = false;
		vest_frame.enabled = false;
		inventory_frame.enabled = false;
		pickups_frame.enabled = false;
		drop_frame.enabled = false;
		use_frame.enabled = false;
	}

	public AmmoBox GetCurrentAmmoBox()
	{
		foreach (AmmoBox ammoBox in ammoBoxes)
		{
			if (ammoBox.UI.gameObject.activeSelf)
			{
				return ammoBox;
			}
		}
		foreach (AmmoBox ammoBox2 in ammoBoxes)
		{
			if (ammoBox2.isNearPlayer)
			{
				return ammoBox2;
			}
		}
		return null;
	}

	public Item GetItem(string name)
	{
		Item[] array = items;
		foreach (Item item in array)
		{
			if (item != null && item.name == name)
			{
				return item;
			}
		}
		return null;
	}

	public Item GetItemByObj(string objName)
	{
		Item[] array = items;
		foreach (Item item in array)
		{
			if (item.objName == objName)
			{
				return item;
			}
		}
		return null;
	}

	private void EnableAmmoBoxesNearMainplayer()
	{
		CharacterMultiplayer characterMultiplayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)characterMultiplayer && characterMultiplayer.IsDead())
		{
			characterMultiplayer = CharacterMultiplayer.GetSpectatingPlayer();
		}
		Vector3 b = Vector3.zero;
		if ((bool)characterMultiplayer)
		{
			b = characterMultiplayer.transform.position;
		}
		foreach (AmmoBox ammoBox in ammoBoxes)
		{
			if ((bool)ammoBox.lootPoint)
			{
				if (Vector3.Distance(ammoBox.transform.position, b) < 7f)
				{
					Statics.SetActive(ammoBox.lootPoint.gameObject, active: true);
				}
				else
				{
					Statics.SetActive(ammoBox.lootPoint.gameObject, active: false);
				}
			}
			else if (Vector3.Distance(ammoBox.transform.position, b) < 7f)
			{
				Statics.SetActive(ammoBox.gameObject, active: true);
			}
			else
			{
				Statics.SetActive(ammoBox.gameObject, active: false);
			}
		}
	}

	private IEnumerator UpdateAmmoBoxes()
	{
		while (true)
		{
			CharacterMultiplayer characterMultiplayer = CharacterMultiplayer.GetMainPlayer();
			if ((bool)characterMultiplayer && characterMultiplayer.IsDead())
			{
				characterMultiplayer = CharacterMultiplayer.GetSpectatingPlayer();
			}
			Vector3 b = (characterMultiplayer ? characterMultiplayer.transform.position : Vector3.zero);
			int count = ammoBoxes.Count;
			int num = Mathf.CeilToInt((float)count / (float)frameChunks);
			int num2 = currentChunk * num;
			int num3 = Mathf.Min(num2 + num, count);
			for (int i = num2; i < num3; i++)
			{
				AmmoBox ammoBox = ammoBoxes[i];
				bool active = Vector3.Distance(ammoBox.transform.position, b) < 7f;
				if ((bool)ammoBox.lootPoint)
				{
					Statics.SetActive(ammoBox.lootPoint.gameObject, active);
				}
				else
				{
					Statics.SetActive(ammoBox.gameObject, active);
				}
			}
			currentChunk++;
			if (currentChunk >= frameChunks)
			{
				currentChunk = 0;
			}
			yield return null;
		}
	}

	private IEnumerator UpdateAmmoBoxesWithInterval()
	{
		while (true)
		{
			CharacterMultiplayer characterMultiplayer = CharacterMultiplayer.GetMainPlayer();
			if ((bool)characterMultiplayer && characterMultiplayer.IsDead())
			{
				characterMultiplayer = CharacterMultiplayer.GetSpectatingPlayer();
			}
			Vector3 b = (characterMultiplayer ? characterMultiplayer.transform.position : Vector3.zero);
			int count = ammoBoxes.Count;
			int num = Mathf.CeilToInt((float)count / (float)frameChunks);
			int num2 = currentChunk * num;
			int num3 = Mathf.Min(num2 + num, count);
			for (int i = num2; i < num3; i++)
			{
				AmmoBox ammoBox = ammoBoxes[i];
				bool active = Vector3.Distance(ammoBox.transform.position, b) < ammoboxShowDistance;
				if ((bool)ammoBox.lootPoint)
				{
					Statics.SetActive(ammoBox.lootPoint.gameObject, active);
				}
				else
				{
					Statics.SetActive(ammoBox.gameObject, active);
				}
			}
			currentChunk++;
			if (currentChunk >= frameChunks)
			{
				currentChunk = 0;
			}
			yield return new WaitForSeconds(updateInterval / (float)frameChunks);
		}
	}

	public GameObject InstantiateItemObject(Item item, Transform parent)
	{
		if (item == null)
		{
			Statics.SetActive(parent.gameObject, active: false);
			return null;
		}
		for (int i = 0; i < itemsObjectsContainer.childCount; i++)
		{
			Transform child = itemsObjectsContainer.GetChild(i);
			if (!child || !(child.name == item.name))
			{
				continue;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(child.gameObject, parent);
			gameObject.transform.localPosition = child.localPosition;
			gameObject.transform.localRotation = child.localRotation;
			gameObject.transform.localScale = child.localScale;
			gameObject.name = child.name;
			if (item.type == ItemType.weapon)
			{
				DataManager component = GetComponent<DataManager>();
				WeaponsSkinsManager component2 = GetComponent<WeaponsSkinsManager>();
				int num = component.GetInt(item.name + "_skin", -1);
				if (num >= 0)
				{
					component2.ApplySkin(gameObject, num);
				}
			}
			return gameObject;
		}
		return null;
	}

	private void LateUpdate()
	{
		if (!pickUI_visible)
		{
			pickAmmoBox = null;
			pickItem = null;
		}
		Statics.SetActive(pickUI.gameObject, pickUI_visible);
		pickUI_visible = false;
	}

	public string GetWeaponShortDesc(string weaponName)
	{
		Item item = GetItem(weaponName);
		if (item != null)
		{
			return item.short_description;
		}
		return weaponName;
	}

	public Sprite GetWeaponIcon(string weaponShortDesc)
	{
		Item[] array = items;
		foreach (Item item in array)
		{
			if (item != null && item.short_description == weaponShortDesc)
			{
				return item.image;
			}
		}
		return null;
	}

	public void OnShowBagFullMsg()
	{
		Statics.SetActive(pickUI_full.gameObject, active: true);
		Statics.SetActive(inventory_bag_full.gameObject, active: true);
		CancelInvoke("HideBagFullMsg");
		Invoke("HideBagFullMsg", 3f);
	}

	private void HideBagFullMsg()
	{
		Statics.SetActive(pickUI_full.gameObject, active: false);
		Statics.SetActive(inventory_bag_full.gameObject, active: false);
	}
}
