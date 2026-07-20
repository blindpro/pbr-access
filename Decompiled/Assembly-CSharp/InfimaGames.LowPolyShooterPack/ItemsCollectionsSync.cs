using System;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class ItemsCollectionsSync : MonoBehaviourPun, IPunObservable
{
	[Serializable]
	public class ItemsCollection
	{
		public string name;

		public List<GameObject> items;

		public int grade = 3;

		public int coins = 100;
	}

	[Header("Item Collections (One Active Per Group)")]
	public List<ItemsCollection> itemsCollections = new List<ItemsCollection>();

	public bool mainPlayerChanged = true;

	private byte[] visibleIndexes;

	private byte[] current_visibleIndexes;

	private CharacterBehaviour character;

	private CharacterMultiplayer characterMultiplayer;

	private float menuUpdateTimer;

	private float menuUpdateDuration = 3f;

	private MatchmakingManager matchmakingManager;

	private void Awake()
	{
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
		character = GetComponent<CharacterBehaviour>();
		visibleIndexes = new byte[itemsCollections.Count];
		current_visibleIndexes = new byte[itemsCollections.Count];
		UpdateVisibleIndexes(visibleIndexes);
	}

	private void Start()
	{
		matchmakingManager = GameManager.Instance.GetComponent<MatchmakingManager>();
	}

	private void Update()
	{
		if (base.photonView.IsMine && characterMultiplayer.isMainPlayer && matchmakingManager.WaitingPanel.activeInHierarchy && matchmakingManager.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
		{
			menuUpdateTimer -= Time.deltaTime;
			if (menuUpdateTimer <= 0f)
			{
				menuUpdateTimer = menuUpdateDuration;
				mainPlayerChanged = true;
				Debug.LogWarning("main player sync items forced");
			}
		}
	}

	private void UpdateVisibleIndexes(byte[] visibleIndexes)
	{
		for (int i = 0; i < itemsCollections.Count; i++)
		{
			visibleIndexes[i] = byte.MaxValue;
			ItemsCollection itemsCollection = itemsCollections[i];
			for (int j = 0; j < itemsCollection.items.Count; j++)
			{
				if (itemsCollection.items[j].activeSelf)
				{
					visibleIndexes[i] = (byte)j;
					break;
				}
			}
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			if (base.photonView.IsMine)
			{
				stream.SendNext((byte)(mainPlayerChanged ? 1u : 0u));
				if (mainPlayerChanged)
				{
					UpdateVisibleIndexes(visibleIndexes);
					stream.SendNext(visibleIndexes);
					mainPlayerChanged = false;
					Debug.Log("sent items sync " + character.name);
				}
			}
		}
		else
		{
			if (base.photonView.IsMine || (byte)stream.ReceiveNext() != 1)
			{
				return;
			}
			byte[] array = (byte[])stream.ReceiveNext();
			for (int i = 0; i < itemsCollections.Count; i++)
			{
				byte b = ((i < array.Length) ? array[i] : byte.MaxValue);
				ItemsCollection itemsCollection = itemsCollections[i];
				for (int j = 0; j < itemsCollection.items.Count; j++)
				{
					itemsCollection.items[j].SetActive(j == b);
				}
			}
			Debug.Log("received items sync " + character.name);
		}
	}

	public ItemsCollection GetCollection(string name)
	{
		foreach (ItemsCollection itemsCollection in itemsCollections)
		{
			if (itemsCollection != null && itemsCollection.name == name)
			{
				return itemsCollection;
			}
		}
		return null;
	}

	public GameObject GetCollectionActiveObj(string collection_name)
	{
		if (this == null)
		{
			return null;
		}
		ItemsCollection collection = GetCollection(collection_name);
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

	public void Apply(int body, int head, int neck, int glasses, int earmuffs, int beard, int hair, int facemask, int vest, int bag, int parachute, bool shouldUpdateOther = false)
	{
		if (base.photonView.IsMine && shouldUpdateOther)
		{
			mainPlayerChanged = true;
		}
		SetCollectionActiveObj(itemsCollections, "body", body);
		SetCollectionActiveObj(itemsCollections, "neck", neck);
		SetCollectionActiveObj(itemsCollections, "head", head);
		SetCollectionActiveObj(itemsCollections, "glasses", glasses);
		SetCollectionActiveObj(itemsCollections, "earmuffs", earmuffs);
		SetCollectionActiveObj(itemsCollections, "beard", beard);
		SetCollectionActiveObj(itemsCollections, "hair", hair);
		SetCollectionActiveObj(itemsCollections, "facemask", facemask);
	}

	public static void ApplyToMainPlayer(int body, int head, int neck, int glasses, int earmuffs, int beard, int hair, int facemask, int vest, int bag, int parachute, bool shouldUpdateOther = false)
	{
		if (!(GameManager.Instance == null))
		{
			MenuCharacter mainMenuCharacter = GameManager.Instance.GetComponent<MenuCharactersManager>().GetMainMenuCharacter();
			CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
			if ((bool)mainPlayer)
			{
				ItemsCollectionsSync component = mainPlayer.GetComponent<ItemsCollectionsSync>();
				Debug.Log("ApplyToMainPlayer player " + mainPlayer.name + mainPlayer.Nickname);
				SetCollectionActiveObj(component.itemsCollections, "body", body);
				SetCollectionActiveObj(component.itemsCollections, "neck", neck);
				SetCollectionActiveObj(component.itemsCollections, "head", head);
				SetCollectionActiveObj(component.itemsCollections, "glasses", glasses);
				SetCollectionActiveObj(component.itemsCollections, "earmuffs", earmuffs);
				SetCollectionActiveObj(component.itemsCollections, "beard", beard);
				SetCollectionActiveObj(component.itemsCollections, "hair", hair);
				SetCollectionActiveObj(component.itemsCollections, "facemask", facemask);
				component.mainPlayerChanged = shouldUpdateOther;
			}
			else if (!(mainMenuCharacter == null))
			{
				Debug.Log("ApplyToMainPlayer menu " + mainMenuCharacter.name + mainMenuCharacter.nickname);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "body", body);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "neck", neck);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "head", head);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "glasses", glasses);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "earmuffs", earmuffs);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "beard", beard);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "hair", hair);
				SetCollectionActiveObj(mainMenuCharacter.itemsCollections, "facemask", facemask);
			}
		}
	}

	public static List<ItemsCollection> GetMainPlayerCollections()
	{
		if (GameManager.Instance == null)
		{
			return null;
		}
		MenuCharacter mainMenuCharacter = GameManager.Instance.GetComponent<MenuCharactersManager>().GetMainMenuCharacter();
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			return mainPlayer.GetComponent<ItemsCollectionsSync>().itemsCollections;
		}
		return mainMenuCharacter.itemsCollections;
	}

	public static GameObject GetCollectionActiveObj(List<ItemsCollection> itemsCollections, string collection_name)
	{
		if (itemsCollections == null)
		{
			return null;
		}
		ItemsCollection collection = GetCollection(itemsCollections, collection_name);
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

	public static int GetCollectionActiveObjId(List<ItemsCollection> itemsCollections, string collection_name)
	{
		if (itemsCollections == null)
		{
			return -1;
		}
		ItemsCollection collection = GetCollection(itemsCollections, collection_name);
		if (collection == null)
		{
			Debug.LogWarning("GetCollectionActiveObjId collection null " + collection_name);
			return -1;
		}
		for (int i = 0; i < collection.items.Count; i++)
		{
			GameObject gameObject = collection.items[i];
			if ((bool)gameObject && gameObject.activeSelf)
			{
				return i;
			}
		}
		return -1;
	}

	public static ItemsCollection GetCollection(List<ItemsCollection> itemsCollections, string name)
	{
		foreach (ItemsCollection itemsCollection in itemsCollections)
		{
			if (itemsCollection != null && itemsCollection.name == name)
			{
				return itemsCollection;
			}
		}
		return null;
	}

	public static void SetCollectionActiveObj(List<ItemsCollection> itemsCollections, string collection_name, string obj_name)
	{
		if (itemsCollections == null)
		{
			return;
		}
		ItemsCollection collection = GetCollection(itemsCollections, collection_name);
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
	}

	public static void SetCollectionActiveObj(List<ItemsCollection> itemsCollections, string collection_name, int obj_id)
	{
		if (itemsCollections == null)
		{
			return;
		}
		ItemsCollection collection = GetCollection(itemsCollections, collection_name);
		if (collection == null)
		{
			Debug.LogWarning("SetCollectionActiveObj collection null " + collection_name + " " + obj_id);
			return;
		}
		foreach (GameObject item in collection.items)
		{
			if ((bool)item)
			{
				item.SetActive(value: false);
			}
		}
		for (int i = 0; i < collection.items.Count; i++)
		{
			if (obj_id >= 0 && obj_id < collection.items.Count)
			{
				GameObject gameObject = collection.items[i];
				if ((bool)gameObject)
				{
					gameObject.SetActive(i == obj_id);
				}
			}
		}
	}

	public static void SetCollectionNextActiveObj(List<ItemsCollection> itemsCollections, string collection_name)
	{
		if (itemsCollections == null)
		{
			return;
		}
		ItemsCollection collection = GetCollection(itemsCollections, collection_name);
		if (collection == null)
		{
			Debug.LogWarning("SetCollectionNextActiveObj collection null " + collection_name);
			return;
		}
		int collectionActiveObjId = GetCollectionActiveObjId(itemsCollections, collection_name);
		collectionActiveObjId = ((collectionActiveObjId != -1) ? (collectionActiveObjId + 1) : 0);
		if (collectionActiveObjId < 0)
		{
			collectionActiveObjId = 0;
		}
		if (collectionActiveObjId >= collection.items.Count)
		{
			collectionActiveObjId = collection.items.Count - 1;
		}
		SetCollectionActiveObj(itemsCollections, collection_name, collectionActiveObjId);
	}

	public static void SetCollectionPrevActiveObj(List<ItemsCollection> itemsCollections, string collection_name)
	{
		if (itemsCollections == null)
		{
			return;
		}
		ItemsCollection collection = GetCollection(itemsCollections, collection_name);
		if (collection == null)
		{
			Debug.LogWarning("SetCollectionNextActiveObj collection null " + collection_name);
			return;
		}
		int collectionActiveObjId = GetCollectionActiveObjId(itemsCollections, collection_name);
		collectionActiveObjId = ((collectionActiveObjId != -1) ? (collectionActiveObjId - 1) : 0);
		if (collectionActiveObjId < 0)
		{
			collectionActiveObjId = 0;
		}
		if (collectionActiveObjId >= collection.items.Count)
		{
			collectionActiveObjId = collection.items.Count - 1;
		}
		SetCollectionActiveObj(itemsCollections, collection_name, collectionActiveObjId);
	}

	public static string GetCollectionActiveObjUnlockSaveName(string collection, string prefix = "cc")
	{
		if (GameManager.Instance == null)
		{
			return "";
		}
		int collectionActiveObjId = GetCollectionActiveObjId(GetMainPlayerCollections(), collection);
		if (collectionActiveObjId == -1)
		{
			return "";
		}
		string appUniqueId = GameManager.Instance.GetComponent<MatchmakingManager>().GetAppUniqueId();
		return $"{prefix}_{collection}_{appUniqueId}_{collectionActiveObjId}";
	}

	public static string GetCollectionActiveObjSaveName(string collection, string prefix = "cc")
	{
		if (GameManager.Instance == null)
		{
			return "";
		}
		string appUniqueId = GameManager.Instance.GetComponent<MatchmakingManager>().GetAppUniqueId();
		return prefix + "_" + collection + "_" + appUniqueId;
	}

	public static byte[] EncodeBodyData(int body, int head, int neck, int glasses, int earmuffs, int beard, int hair, int facemask, int vest, int bag, int parachute)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(body);
		binaryWriter.Write(head);
		binaryWriter.Write(neck);
		binaryWriter.Write(glasses);
		binaryWriter.Write(earmuffs);
		binaryWriter.Write(beard);
		binaryWriter.Write(hair);
		binaryWriter.Write(facemask);
		binaryWriter.Write(vest);
		binaryWriter.Write(bag);
		binaryWriter.Write(parachute);
		return memoryStream.ToArray();
	}

	public static void DecodeBodyData(byte[] data, out int body, out int head, out int neck, out int glasses, out int earmuffs, out int beard, out int hair, out int facemask, out int vest, out int bag, out int parachute)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		body = binaryReader.ReadInt32();
		head = binaryReader.ReadInt32();
		neck = binaryReader.ReadInt32();
		glasses = binaryReader.ReadInt32();
		earmuffs = binaryReader.ReadInt32();
		beard = binaryReader.ReadInt32();
		hair = binaryReader.ReadInt32();
		facemask = binaryReader.ReadInt32();
		vest = binaryReader.ReadInt32();
		bag = binaryReader.ReadInt32();
		parachute = binaryReader.ReadInt32();
	}
}
