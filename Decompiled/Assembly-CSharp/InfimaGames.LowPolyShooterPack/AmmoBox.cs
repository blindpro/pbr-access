using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class AmmoBox : MonoBehaviour
{
	public Text desc;

	public Text lv;

	public Image icon;

	public LootPoint lootPoint;

	public bool achievable;

	public Transform UI;

	public Canvas UICanvas;

	public List<PickupsManager.Item> items = new List<PickupsManager.Item>();

	public List<GameObject> items_objs = new List<GameObject>();

	public bool random_items = true;

	public bool isNearPlayer;

	public Transform[] objectsContainers;

	private PickupsManager pickupsManager;

	private ProgressionManager progressionManager;

	public PickupsManager.Item currentItem;

	public bool lastPickedItemWasDroped;

	private void Awake()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
	}

	private void Start()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
		if (!pickupsManager.ammoBoxes.Contains(this))
		{
			pickupsManager.ammoBoxes.Add(this);
		}
		if (achievable && !pickupsManager.ammoBoxesAchievable.Contains(this))
		{
			pickupsManager.ammoBoxesAchievable.Add(this);
		}
		UI.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		if (!(GameManager.Instance == null))
		{
			pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
			progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
			if (pickupsManager.ammoBoxes.Contains(this))
			{
				pickupsManager.ammoBoxes.Remove(this);
			}
			if (pickupsManager.ammoBoxesAchievable.Contains(this))
			{
				pickupsManager.ammoBoxesAchievable.Remove(this);
			}
		}
	}

	public void CreateRandomItems()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
		if (random_items)
		{
			items.Clear();
			int num = Random.Range(3, 6);
			for (int i = 0; i < num; i++)
			{
				int num2 = Random.Range(0, pickupsManager.items.Length);
				items.Add(pickupsManager.items[num2]);
			}
		}
		ShowObjects();
	}

	public void ShowObjects()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
		items_objs.Clear();
		for (int i = 0; i < objectsContainers.Length; i++)
		{
			Transform transform = objectsContainers[i];
			if ((bool)transform)
			{
				Statics.DestroyAllChildren(transform.gameObject);
				if (i < items.Count)
				{
					PickupsManager.Item item = items[i];
					Statics.SetActive(transform.gameObject, active: true);
					items_objs.Add(pickupsManager.InstantiateItemObject(item, transform));
				}
				else
				{
					Statics.SetActive(transform.gameObject, active: false);
				}
			}
		}
	}

	private void RemoveObject(PickupsManager.Item item)
	{
		if (item == null)
		{
			return;
		}
		int num = items.IndexOf(item);
		if (num != -1)
		{
			GameObject gameObject = items_objs[num];
			if ((bool)gameObject.transform.parent)
			{
				Statics.SetActive(gameObject.transform.parent.gameObject, active: false);
			}
			items_objs.RemoveAt(num);
			if ((bool)gameObject)
			{
				Object.DestroyImmediate(gameObject);
			}
		}
	}

	private void Update()
	{
		isNearPlayer = false;
		currentItem = null;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!mainPlayer || !(Vector3.Distance(base.transform.position, mainPlayer.transform.position) < pickupsManager.pickRange))
		{
			return;
		}
		isNearPlayer = true;
		ThirdPerson component = mainPlayer.GetComponent<ThirdPerson>();
		Vector3 position = component.fps_camera.transform.position;
		Vector3 endWorld = position + component.fps_camera.transform.forward * 5f;
		BoxCollider boxCollider = null;
		float num = float.MaxValue;
		PickupsManager.Item item = null;
		for (int i = 0; i < items_objs.Count; i++)
		{
			GameObject gameObject = items_objs[i];
			if ((bool)gameObject && gameObject.activeInHierarchy)
			{
				BoxCollider component2 = gameObject.GetComponent<BoxCollider>();
				if ((bool)component2 && LineIntersectsBoxLocal(position, endWorld, component2, out var distance) && distance < num)
				{
					boxCollider = component2;
					num = distance;
					item = items[i];
				}
			}
		}
		if (!boxCollider)
		{
			return;
		}
		Vector3 vector = (component.fps_camera.enabled ? pickupsManager.pickConvasFpsPos : pickupsManager.pickConvasTpsPos);
		Vector3 position2 = component.fps_camera.WorldToScreenPoint(boxCollider.transform.position) + vector;
		pickupsManager.pickUI.position = position2;
		if (item == null)
		{
			return;
		}
		pickupsManager.pickUI_visible = true;
		currentItem = item;
		pickupsManager.pickAmmoBox = this;
		pickupsManager.pickItem = currentItem;
		if (pickupsManager.pickUI_icon.sprite != item.image)
		{
			string text = item.short_description;
			if (text == "")
			{
				text = item.description;
			}
			pickupsManager.pickUI_desc.text = "<color=yellow>Pickup</color> " + text;
			pickupsManager.pickUI_icon.sprite = null;
			pickupsManager.pickUI_icon.sprite = item.image;
			if (item.level > 0)
			{
				pickupsManager.pickUI_lv.text = "Lv " + item.level;
			}
			else
			{
				pickupsManager.pickUI_lv.text = "";
			}
			if (item.type == PickupsManager.ItemType.weapon)
			{
				pickupsManager.pickUI_lv.text = "Lv " + progressionManager.GetWeaponGrade(item.short_description);
			}
		}
	}

	private void PlayLootSound()
	{
		GameManager.Instance.GetComponent<HitCursorsManager>().PlayLootSound();
	}

	public PickupsManager.Item RemoveItem(PickupsManager.Item item)
	{
		if (item != null && items.Contains(item))
		{
			RemoveObject(item);
			items.Remove(item);
			PlayLootSound();
			currentItem = null;
		}
		return item;
	}

	public PickupsManager.Item ReplaceItem(PickupsManager.Item item, PickupsManager.Item with)
	{
		lastPickedItemWasDroped = false;
		if (item == null)
		{
			return null;
		}
		if (with == null)
		{
			return RemoveItem(item);
		}
		if (items.Contains(item))
		{
			int num = items.IndexOf(item);
			if (num != -1)
			{
				items[num] = with;
			}
			if (num < items_objs.Count)
			{
				Transform transform = items_objs[num]?.transform.parent;
				if ((bool)transform)
				{
					if ((bool)items_objs[num] && items_objs[num].tag == "droped")
					{
						lastPickedItemWasDroped = true;
					}
					Statics.DestroyAllChildren(transform.gameObject);
					items_objs[num] = pickupsManager.InstantiateItemObject(with, transform);
					if ((bool)items_objs[num])
					{
						items_objs[num].tag = "droped";
					}
				}
			}
			PlayLootSound();
			currentItem = null;
		}
		return item;
	}

	private bool LineIntersectsBoxLocal(Vector3 startWorld, Vector3 endWorld, BoxCollider box, out float distance)
	{
		distance = float.MaxValue;
		Matrix4x4 worldToLocalMatrix = box.transform.worldToLocalMatrix;
		Vector3 vector = worldToLocalMatrix.MultiplyPoint(startWorld);
		Vector3 vector2 = worldToLocalMatrix.MultiplyPoint(endWorld);
		Vector3 vector3 = vector2 - vector;
		Vector3 vector4 = box.center - box.size * 0.5f;
		Vector3 vector5 = box.center + box.size * 0.5f;
		float num = 0f;
		float num2 = 1f;
		for (int i = 0; i < 3; i++)
		{
			float num3 = vector[i];
			float num4 = vector3[i];
			if (Mathf.Abs(num4) < 1E-06f)
			{
				if (num3 < vector4[i] || num3 > vector5[i])
				{
					return false;
				}
				continue;
			}
			float num5 = 1f / num4;
			float num6 = (vector4[i] - num3) * num5;
			float num7 = (vector5[i] - num3) * num5;
			if (num6 > num7)
			{
				float num8 = num7;
				float num9 = num6;
				num6 = num8;
				num7 = num9;
			}
			num = Mathf.Max(num, num6);
			num2 = Mathf.Min(num2, num7);
			if (num > num2)
			{
				return false;
			}
		}
		distance = (vector2 - vector).magnitude * num;
		return true;
	}
}
