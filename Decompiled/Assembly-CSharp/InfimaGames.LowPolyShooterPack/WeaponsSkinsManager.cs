using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class WeaponsSkinsManager : MonoBehaviour
{
	[Serializable]
	public class Skin
	{
		public Material material;

		public int grade = 2;

		public int coins = 100;
	}

	public Material defaultSkin;

	public Skin[] skins;

	public Camera cam;

	public RenderTexture rt;

	public Transform weaponsParent;

	private float aspect;

	private int currentRenderWidth;

	private int currentRenderHeight;

	private Transform clonedWeaponsParent;

	public Transform not_enough_coins;

	public Transform not_enough_grade;

	private int currentSkin;

	private string currentWeapon = "";

	private DataManager dataManager;

	public CustomizeRow[] customizeRows;

	private void Awake()
	{
		dataManager = GetComponent<DataManager>();
	}

	private void Start()
	{
		Init();
	}

	public void Init()
	{
		dataManager = GetComponent<DataManager>();
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
		OnSelectWeapon("Handgun 01", resetSkin: true);
	}

	private void UpdateCustomizeRows()
	{
		CustomizeRow[] array = customizeRows;
		foreach (CustomizeRow customizeRow in array)
		{
			if ((bool)customizeRow)
			{
				customizeRow.UpdateWeapon();
			}
		}
	}

	private void Update()
	{
		if (currentRenderWidth != Screen.width || currentRenderHeight != Screen.height)
		{
			ResizeRT();
		}
		if (GameManager.Instance.CheatCodes && cam.gameObject.activeInHierarchy && currentWeapon != "")
		{
			ProgressionManager component = GameManager.Instance.GetComponent<ProgressionManager>();
			string weaponShortDesc = GameManager.Instance.GetComponent<PickupsManager>().GetWeaponShortDesc(currentWeapon);
			int weaponGrade = component.GetWeaponGrade(weaponShortDesc);
			if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.V))
			{
				component.SetWeaponGrade(weaponShortDesc, weaponGrade + 1);
				UpdateCustomizeRows();
			}
			if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.V))
			{
				component.SetWeaponGrade(weaponShortDesc, weaponGrade - 1);
				UpdateCustomizeRows();
			}
		}
	}

	public void ApplySkin(GameObject weapon, int skin)
	{
		if (skin >= skins.Length || skin < 0)
		{
			return;
		}
		Renderer[] componentsInChildren = weapon.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			Material[] materials = renderer.materials;
			for (int j = 0; j < renderer.materials.Length; j++)
			{
				string text = renderer.materials[j].name.ToLower();
				if (!text.Contains("basic") && !text.Contains("carbonfibre_001") && !text.Contains("sight") && !text.Contains("scope") && !text.Contains("laser_beam") && !renderer.gameObject.GetComponent<ParticleSystem>())
				{
					materials[j] = skins[skin].material;
				}
			}
			renderer.materials = materials;
		}
	}

	public GameObject OnSelectWeapon(string weapon, bool resetSkin)
	{
		HideAllUnlocks();
		GameObject gameObject = null;
		if ((bool)clonedWeaponsParent)
		{
			UnityEngine.Object.DestroyImmediate(clonedWeaponsParent.gameObject);
			clonedWeaponsParent = null;
		}
		clonedWeaponsParent = UnityEngine.Object.Instantiate(weaponsParent, weaponsParent.parent);
		clonedWeaponsParent.gameObject.SetActive(value: true);
		weaponsParent.gameObject.SetActive(value: false);
		clonedWeaponsParent.localPosition = weaponsParent.localPosition;
		clonedWeaponsParent.localRotation = weaponsParent.localRotation;
		clonedWeaponsParent.localScale = weaponsParent.localScale;
		for (int i = 0; i < clonedWeaponsParent.childCount; i++)
		{
			Transform child = clonedWeaponsParent.GetChild(i);
			if ((bool)child)
			{
				child.gameObject.SetActive(child.name == weapon);
				if (child.name == weapon)
				{
					gameObject = child.gameObject;
				}
			}
		}
		Debug.Log(weapon);
		if (resetSkin)
		{
			currentSkin = -1;
		}
		if ((bool)gameObject)
		{
			int num = dataManager.GetInt(weapon + "_skin", -1);
			if (num >= 0 && resetSkin)
			{
				currentSkin = num;
				ApplySkin(gameObject, currentSkin);
			}
			currentWeapon = gameObject.name;
		}
		return gameObject;
	}

	private void ResizeRT()
	{
		if (cam.gameObject.activeInHierarchy)
		{
			cam.aspect = (float)Screen.width / (float)Screen.height;
			currentRenderWidth = Screen.width;
			currentRenderHeight = Screen.height;
			Debug.LogWarning("WeaponsSkinsManager ResizeRT");
		}
	}

	public void NextItem(string weapon, int row)
	{
		GameObject gameObject = OnSelectWeapon(weapon, currentWeapon != weapon);
		currentSkin++;
		if (currentSkin >= skins.Length)
		{
			currentSkin = skins.Length - 1;
		}
		if (currentSkin < 0)
		{
			currentSkin = 0;
		}
		if (dataManager.GetInt(weapon + "_skin_" + currentSkin) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(weapon + "_skin_" + currentSkin, 1, save: true);
			SaveItem(gameObject, weapon, row);
		}
		else
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: true);
			customizeRows[row].gradeTxt.text = skins[currentSkin].grade.ToString();
			customizeRows[row].coinsTxt.text = skins[currentSkin].coins.ToString();
			ApplySkin(gameObject, currentSkin);
		}
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
	}

	public void PrevItem(string weapon, int row)
	{
		GameObject gameObject = OnSelectWeapon(weapon, currentWeapon != weapon);
		currentSkin--;
		if (currentSkin >= skins.Length)
		{
			currentSkin = skins.Length - 1;
		}
		if (currentSkin < 0)
		{
			currentSkin = 0;
		}
		if (dataManager.GetInt(weapon + "_skin_" + currentSkin) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(weapon + "_skin_" + currentSkin, 1, save: true);
			SaveItem(gameObject, weapon, row);
		}
		else
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: true);
			customizeRows[row].gradeTxt.text = skins[currentSkin].grade.ToString();
			customizeRows[row].coinsTxt.text = skins[currentSkin].coins.ToString();
			ApplySkin(gameObject, currentSkin);
		}
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
	}

	private void SaveItem(GameObject w, string weapon, int row)
	{
		if (dataManager.GetInt(weapon + "_skin_" + currentSkin) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(weapon + "_skin", currentSkin, save: true);
			ApplySkin(w, currentSkin);
		}
	}

	private void HideAllUnlocks()
	{
		CustomizeRow[] array = customizeRows;
		foreach (CustomizeRow customizeRow in array)
		{
			if ((bool)customizeRow)
			{
				customizeRow.unlockUI.gameObject.SetActive(value: false);
			}
		}
	}

	public void UnlockItem(string weapon, int row)
	{
		GameObject gameObject = OnSelectWeapon(weapon, currentWeapon != weapon);
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
		if (currentSkin >= skins.Length || currentSkin < 0)
		{
			return;
		}
		if (dataManager.GetInt(weapon + "_skin_" + currentSkin) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(weapon + "_skin_" + currentSkin, 1, save: true);
			SaveItem(gameObject, weapon, row);
			return;
		}
		customizeRows[row].unlockUI.gameObject.SetActive(value: true);
		customizeRows[row].gradeTxt.text = skins[currentSkin].grade.ToString();
		customizeRows[row].coinsTxt.text = skins[currentSkin].coins.ToString();
		string weaponShortDesc = GameManager.Instance.GetComponent<PickupsManager>().GetWeaponShortDesc(weapon);
		int weaponGrade = GameManager.Instance.GetComponent<ProgressionManager>().GetWeaponGrade(weaponShortDesc);
		int coins = GameManager.Instance.GetComponent<ProgressionManager>().GetCoins();
		ApplySkin(gameObject, currentSkin);
		if (weaponGrade < skins[currentSkin].grade)
		{
			not_enough_grade.gameObject.SetActive(value: true);
			return;
		}
		if (coins < skins[currentSkin].coins)
		{
			not_enough_coins.gameObject.SetActive(value: true);
			return;
		}
		GameManager.Instance.GetComponent<ProgressionManager>().SetCoins(coins - skins[currentSkin].coins);
		customizeRows[row].unlockUI.gameObject.SetActive(value: false);
		dataManager.SetInt(weapon + "_skin_" + currentSkin, 1, save: true);
		SaveItem(gameObject, weapon, row);
	}

	public void RemoveItem(string weapon, int row)
	{
		dataManager.SetInt(weapon + "_skin", -1, save: true);
		OnSelectWeapon(weapon, resetSkin: true);
		customizeRows[row].unlockUI.gameObject.SetActive(value: false);
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
		currentSkin = -1;
	}

	public void NextItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string weapon = array[0];
		int row = int.Parse(array[1]);
		NextItem(weapon, row);
	}

	public void PrevItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string weapon = array[0];
		int row = int.Parse(array[1]);
		PrevItem(weapon, row);
	}

	public void UnlockItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string weapon = array[0];
		int row = int.Parse(array[1]);
		UnlockItem(weapon, row);
	}

	public void RemoveItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string weapon = array[0];
		int row = int.Parse(array[1]);
		RemoveItem(weapon, row);
	}
}
