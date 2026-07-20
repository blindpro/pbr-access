using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterCustomizationManager : MonoBehaviour
{
	public int body_default;

	public int head_default = -1;

	public int neck_default = -1;

	public int glasses_default = -1;

	public int earmuffs_default = -1;

	public int beard_default = -1;

	public int hair_default;

	public int facemask_default = -1;

	public int vest_default;

	public int bag_default;

	public int parachute_default;

	public Text not_enough_coins;

	public Text not_enough_grade;

	private DataManager dataManager;

	public CustomizeRow[] customizeRows;

	private void Awake()
	{
		dataManager = GetComponent<DataManager>();
		SteamCloudManager.OnSteamDataLoadingFinished += OnSteamDataLoadingFinished;
	}

	private void Start()
	{
		UnlockDefaults();
		ResetFromSaved();
		CancelInvoke("ResetFromSaved");
		Invoke("ResetFromSaved", 0.5f);
	}

	private void Update()
	{
	}

	private void UnlockDefaults()
	{
		string appUniqueId = GetComponent<MatchmakingManager>().GetAppUniqueId();
		dataManager.SetInt("cc_body_" + appUniqueId + "_" + body_default, 1);
		dataManager.SetInt("cc_hair_" + appUniqueId + "_" + hair_default, 1, save: true);
	}

	private void OnSteamDataLoadingFinished()
	{
		Debug.Log("OnSteamDataLoadingFinished event");
		ResetFromSaved();
	}

	public void ResetFromSaved()
	{
		if ((bool)dataManager)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 1;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			int num10 = 0;
			string appUniqueId = GetComponent<MatchmakingManager>().GetAppUniqueId();
			int body = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("body"), body_default);
			num = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("head"), head_default);
			num2 = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("neck"), neck_default);
			num3 = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("glasses"), glasses_default);
			num4 = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("earmuffs"), earmuffs_default);
			num5 = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("beard"), beard_default);
			num6 = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("hair"), hair_default);
			num7 = dataManager.GetInt(ItemsCollectionsSync.GetCollectionActiveObjSaveName("facemask"), facemask_default);
			num8 = dataManager.GetInt("cc_vest_" + appUniqueId, vest_default);
			num9 = dataManager.GetInt("cc_bag_" + appUniqueId, bag_default);
			num10 = dataManager.GetInt("cc_parachute_" + appUniqueId, parachute_default);
			ItemsCollectionsSync.ApplyToMainPlayer(body, num, num2, num3, num4, num5, num6, num7, num8, num9, num10);
			CustomizeRow[] array = customizeRows;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].unlockUI.gameObject.SetActive(value: false);
			}
			not_enough_coins.gameObject.SetActive(value: false);
			not_enough_grade.gameObject.SetActive(value: false);
			UnlockDefaults();
			GameManager.Instance.GetComponent<ProgressionManager>().UpdateProfile();
		}
	}

	public void NextItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string collection = array[0];
		int row = int.Parse(array[1]);
		NextItem(collection, row);
	}

	public void PrevItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string collection = array[0];
		int row = int.Parse(array[1]);
		PrevItem(collection, row);
	}

	public void UnlockItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string collection = array[0];
		int row = int.Parse(array[1]);
		UnlockItem(collection, row);
	}

	public void RemoveItem(string collection_row)
	{
		string[] array = collection_row.Split(',');
		string collection = array[0];
		int row = int.Parse(array[1]);
		RemoveItem(collection, row);
	}

	private void SaveItem(string collection, int row)
	{
		string collectionActiveObjUnlockSaveName = ItemsCollectionsSync.GetCollectionActiveObjUnlockSaveName(collection);
		string collectionActiveObjSaveName = ItemsCollectionsSync.GetCollectionActiveObjSaveName(collection);
		if (dataManager.GetInt(collectionActiveObjUnlockSaveName) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			int collectionActiveObjId = ItemsCollectionsSync.GetCollectionActiveObjId(ItemsCollectionsSync.GetMainPlayerCollections(), collection);
			dataManager.SetInt(collectionActiveObjSaveName, collectionActiveObjId, save: true);
			CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
			if ((bool)mainPlayer)
			{
				mainPlayer.GetComponent<ItemsCollectionsSync>().mainPlayerChanged = true;
			}
		}
	}

	public void NextItem(string collection, int row)
	{
		ItemsCollectionsSync.SetCollectionNextActiveObj(ItemsCollectionsSync.GetMainPlayerCollections(), collection);
		string collectionActiveObjUnlockSaveName = ItemsCollectionsSync.GetCollectionActiveObjUnlockSaveName(collection);
		if (dataManager.GetInt(collectionActiveObjUnlockSaveName) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(collectionActiveObjUnlockSaveName, 1, save: true);
			SaveItem(collection, row);
		}
		else
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: true);
			ItemsCollectionsSync.ItemsCollection collection2 = ItemsCollectionsSync.GetCollection(ItemsCollectionsSync.GetMainPlayerCollections(), collection);
			if (collection2 != null)
			{
				customizeRows[row].gradeTxt.text = collection2.grade.ToString();
				customizeRows[row].coinsTxt.text = collection2.coins.ToString();
			}
		}
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
	}

	public void PrevItem(string collection, int row)
	{
		ItemsCollectionsSync.SetCollectionPrevActiveObj(ItemsCollectionsSync.GetMainPlayerCollections(), collection);
		string collectionActiveObjUnlockSaveName = ItemsCollectionsSync.GetCollectionActiveObjUnlockSaveName(collection);
		if (dataManager.GetInt(collectionActiveObjUnlockSaveName) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(collectionActiveObjUnlockSaveName, 1, save: true);
			SaveItem(collection, row);
		}
		else
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: true);
			ItemsCollectionsSync.ItemsCollection collection2 = ItemsCollectionsSync.GetCollection(ItemsCollectionsSync.GetMainPlayerCollections(), collection);
			if (collection2 != null)
			{
				customizeRows[row].gradeTxt.text = collection2.grade.ToString();
				customizeRows[row].coinsTxt.text = collection2.coins.ToString();
			}
		}
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
	}

	public void UnlockItem(string collection, int row)
	{
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
		string collectionActiveObjUnlockSaveName = ItemsCollectionsSync.GetCollectionActiveObjUnlockSaveName(collection);
		if (dataManager.GetInt(collectionActiveObjUnlockSaveName) == 1)
		{
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(collectionActiveObjUnlockSaveName, 1, save: true);
			SaveItem(collection, row);
			return;
		}
		customizeRows[row].unlockUI.gameObject.SetActive(value: true);
		ItemsCollectionsSync.ItemsCollection collection2 = ItemsCollectionsSync.GetCollection(ItemsCollectionsSync.GetMainPlayerCollections(), collection);
		if (collection2 != null)
		{
			int playerGrade = GameManager.Instance.GetComponent<ProgressionManager>().GetPlayerGrade();
			int coins = GameManager.Instance.GetComponent<ProgressionManager>().GetCoins();
			if (playerGrade < collection2.grade)
			{
				not_enough_grade.gameObject.SetActive(value: true);
				return;
			}
			if (coins < collection2.coins)
			{
				not_enough_coins.gameObject.SetActive(value: true);
				return;
			}
			GameManager.Instance.GetComponent<ProgressionManager>().SetCoins(coins - collection2.coins);
			customizeRows[row].unlockUI.gameObject.SetActive(value: false);
			dataManager.SetInt(collectionActiveObjUnlockSaveName, 1, save: true);
			SaveItem(collection, row);
		}
	}

	public void RemoveItem(string collection, int row)
	{
		ItemsCollectionsSync.SetCollectionActiveObj(ItemsCollectionsSync.GetMainPlayerCollections(), collection, -1);
		string collectionActiveObjSaveName = ItemsCollectionsSync.GetCollectionActiveObjSaveName(collection);
		dataManager.SetInt(collectionActiveObjSaveName, -1, save: true);
		customizeRows[row].unlockUI.gameObject.SetActive(value: false);
		not_enough_coins.gameObject.SetActive(value: false);
		not_enough_grade.gameObject.SetActive(value: false);
		SaveItem(collection, row);
	}
}
