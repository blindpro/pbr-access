using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
	public class FakeRoom
	{
		public string name = "";

		public MatchMode mode;

		public string map;

		public string map_name;

		public int maxPlayers;
	}

	[Serializable]
	public class SquadPlayer
	{
		public int ActorNumber;

		public string name = "";
	}

	[Serializable]
	public class Squad
	{
		public int id;

		public List<SquadPlayer> squadPlayers = new List<SquadPlayer>();

		public bool AddPlayer(SquadPlayer squadPlayer)
		{
			if (squadPlayers.Count >= 5)
			{
				return false;
			}
			foreach (SquadPlayer squadPlayer2 in squadPlayers)
			{
				if (squadPlayer2 != null && squadPlayer2.name == squadPlayer.name && squadPlayer2.ActorNumber == squadPlayer.ActorNumber)
				{
					return false;
				}
			}
			squadPlayers.Add(squadPlayer);
			return true;
		}
	}

	[Serializable]
	public class SquadListWrapper
	{
		public List<Squad> squads;
	}

	public enum RoomStatus
	{
		Disconnected,
		Connected,
		Waiting,
		PrePlaying,
		Playing,
		Finish
	}

	public enum MatchMode
	{
		TeamDeathMatch,
		FreeForAll,
		BattleRoyale
	}

	public static MatchmakingManager Instance;

	public string appId = "cd281d60-42c9-43fb-93bf-6b147f990a3e";

	public string PlayerName = "player-name";

	private string appUniqueId = "123456";

	public int SquadId;

	public string GameVersion = "1.0";

	public MatchMode GameMatchMode = MatchMode.BattleRoyale;

	public int MaxPlayers = 50;

	public int MinPlayers = 45;

	public List<string> Log = new List<string>();

	public Dictionary<string, RoomInfo> availableRoomsList = new Dictionary<string, RoomInfo>();

	public bool AllowBots = true;

	public string BotName = "Player";

	public Vector2 BotsJoinTime = new Vector2(7f, 40f);

	public Vector2 BotsJoinTimeInPlay = new Vector2(20f, 60f);

	public Vector2 BotsJoinSameTimeCount = new Vector2(1f, 5f);

	public List<CharacterMultiplayer> playersList = new List<CharacterMultiplayer>();

	public List<Squad> squadsList = new List<Squad>();

	public int max_logs = 4;

	public bool private_match;

	public bool bots_only;

	public InputField PlayerNameTxt;

	public Text VersionTxt;

	public GameObject DisconnectedPanel;

	public Button PlayButn;

	public Button JoinFriendBtn;

	public Dropdown RegionList;

	public GameObject WaitingPanel;

	public Text RoomTxt;

	public Text SquadTxt;

	public Text RoomTitleTxt;

	public Text SquadTitleTxt;

	public Text RoomConnectedPlayers;

	public InputField friendRoom;

	public InputField squadInputField;

	public Button JoinFriendRoomBtn;

	public Button QuitWaitingBtn;

	public Button SpectateBtn;

	public string PlayerPrefabName = "PlayerPrefab";

	public GameObject Spawns;

	public float WaitingTime = 180f;

	public float WaitingTimeEditor = 180f;

	public float WaitingTimeNextMatch = 30f;

	public Text WaitingTimerTxt;

	public Transform leftSquad;

	public UnityEvent onConnectedToNetwork;

	public UnityEvent onJoinedRoom;

	public UnityEvent onMatchStarted;

	public UnityEvent onDead;

	public UnityEvent onRespawn;

	public UnityEvent onMatchFinished;

	public UnityEvent onMatchFinishedImmediate;

	public bool bot_random_body = true;

	public bool bot_random_hair = true;

	private float RefreshListsDelay = 1f;

	private bool joinedFakeRandomRoom;

	private FakeRoom[] fakeRooms;

	private RoomStatus roomStatus;

	private string[] codes = new string[15]
	{
		"asia", "au", "cae", "cn", "eu", "in", "jp", "ru", "rue", "za",
		"sa", "kr", "us", "usw", "tr"
	};

	private string[] regions = new string[15]
	{
		"Asia", "Australia", "Canada, East", "Chinese Mainland", "Europe", "India", "Japan", "Russia", "Russia, East", "South Africa",
		"South America", "South Korea", "USA, East", "USA, West", "Turkey"
	};

	private bool enableRegionSwitch;

	private float timeElapsedLobby;

	private float timeElapsedNextMatch;

	private int requestedSquadId;

	private bool offlineRequested;

	private bool offlineRoomRequested;

	private const string PREF_REGION = "PHOTON_REGION";

	public string GetAppUniqueId()
	{
		if ((bool)GameManager.Instance && GameManager.Instance.TestMode)
		{
			return appUniqueId;
		}
		return "";
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			PhotonNetwork.AutomaticallySyncScene = true;
			PhotonNetwork.OfflineMode = false;
			base.gameObject.AddComponent<PhotonView>().ViewID = 999;
			PlayerName += UnityEngine.Random.Range(1000, 9999);
			appUniqueId = UnityEngine.Random.Range(100000, 999999).ToString();
			appUniqueId = PlayerName;
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		Statics.SetActive(RoomConnectedPlayers.gameObject, GameManager.Instance.TestMode);
		Statics.SetActive(RoomTxt.gameObject, GameManager.Instance.TestMode);
		Statics.SetActive(RoomTitleTxt.gameObject, GameManager.Instance.TestMode);
		Statics.SetActive(SquadTxt.gameObject, GameManager.Instance.TestMode);
		Statics.SetActive(SquadTitleTxt.gameObject, GameManager.Instance.TestMode);
		PlayerNameTxt.text = PlayerName;
		appUniqueId = PlayerName;
		Connect();
		RefreshLists();
		BotJoin();
		UpdateFakeRooms();
		FillRegionsList();
		PhotonNetwork.OfflineMode = false;
	}

	public void Update()
	{
		appUniqueId = PlayerName;
		playersList = CharacterMultiplayer.characters;
		UpdateWaitingLobby();
	}

	public void AddLog(string text)
	{
		Debug.Log(text);
		Log.Add(text + "\n");
		if (Log.Count > max_logs)
		{
			Log.RemoveRange(0, 1);
		}
		string text2 = "";
		for (int i = 0; i < Log.Count; i++)
		{
			text2 += Log[i];
		}
	}

	private void EnablePlayButtons(bool enable)
	{
		PlayButn.interactable = enable;
		JoinFriendBtn.interactable = enable;
		RegionList.interactable = enable;
		WaitingPanel.SetActive(value: false);
	}

	public void OnResetMatchSetting()
	{
		private_match = false;
		bots_only = false;
		AllowBots = true;
	}

	public void Connect(string region_code, bool offline = false)
	{
		EnablePlayButtons(enable: false);
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
		}
		string text = GameVersion + "." + SceneManager.GetActiveScene().buildIndex;
		OnResetMatchSetting();
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.NickName = PlayerName;
		PhotonNetwork.GameVersion = text;
		PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = text;
		PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = appId;
		PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
		PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 240000;
		PhotonNetwork.MaxResendsBeforeDisconnect = 100;
		PhotonNetwork.KeepAliveInBackground = 1200f;
		ServerSettings.ResetBestRegionCodeInPreferences();
		Debug.Log("Photon appId:" + PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime + " Version:" + PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion);
		string text2 = PlayerPrefs.GetString("PHOTON_REGION", "");
		if (!string.IsNullOrEmpty(region_code))
		{
			PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = region_code;
			PlayerPrefs.SetString("PHOTON_REGION", region_code);
			PlayerPrefs.Save();
			Debug.Log("saving region " + region_code);
		}
		else if (!string.IsNullOrEmpty(text2))
		{
			PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = text2;
			Debug.Log("using saved region " + text2);
		}
		if (Application.isEditor && offline)
		{
			PhotonNetwork.OfflineMode = true;
			return;
		}
		squadsList.Clear();
		requestedSquadId = 0;
		SquadId = UnityEngine.Random.Range(1000, 9999);
		PhotonNetwork.ConnectUsingSettings();
	}

	public void Connect()
	{
		if (!PhotonNetwork.IsConnected)
		{
			GameManager.Instance.Log("Connection to server ...");
			Connect("");
		}
	}

	public void Reconnect()
	{
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
		}
		string text = PlayerPrefs.GetString("RestartScene", SceneManager.GetActiveScene().name);
		int sceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
		if (text != SceneManager.GetActiveScene().name)
		{
			sceneBuildIndex = 0;
			Debug.LogWarning("scene changed to " + text);
		}
		SceneManager.LoadScene(sceneBuildIndex);
	}

	public bool IsConnected()
	{
		return PhotonNetwork.IsConnected;
	}

	public bool IsOffline()
	{
		return PhotonNetwork.OfflineMode;
	}

	public bool IsInRoom()
	{
		return PhotonNetwork.InRoom;
	}

	public void Disconnect()
	{
		PhotonNetwork.Disconnect();
	}

	public string GetRegion()
	{
		string text = PhotonNetwork.CloudRegion;
		if (text != null && text != "")
		{
			text = text.Replace("/*", "");
		}
		return text;
	}

	public override void OnConnectedToMaster()
	{
		string region = GetRegion();
		string text = "game version:" + PhotonNetwork.GameVersion + "  region:" + region + "  ping:" + PhotonNetwork.GetPing();
		VersionTxt.text = text;
		roomStatus = RoomStatus.Connected;
		onConnectedToNetwork?.Invoke();
		Debug.Log("OnConnectedToMaster() was called by PUN " + text);
		GameManager.Instance.Log("");
		EnablePlayButtons(enable: true);
		FillRegionsList();
		availableRoomsList.Clear();
		PhotonNetwork.JoinLobby();
		Debug.Log("joining lobby");
	}

	public void SetOffline()
	{
		OnConnectedToMasterOffline();
	}

	public void JoinRoomOffline()
	{
		if (offlineRequested)
		{
			JoinRandomRoom();
			return;
		}
		offlineRoomRequested = true;
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
			offlineRequested = true;
		}
		else
		{
			JoinRoomOffline();
		}
	}

	private void OnConnectedToMasterOffline()
	{
		offlineRequested = true;
		OnConnectedToMaster();
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
			offlineRequested = true;
			offlineRoomRequested = false;
		}
		else
		{
			JoinOfflineRoom();
		}
	}

	private void JoinOfflineRoom()
	{
		Debug.LogWarningFormat("JoinOfflineRoom set offline mode");
		if (!PhotonNetwork.OfflineMode)
		{
			PhotonNetwork.OfflineMode = true;
		}
		if (offlineRoomRequested)
		{
			JoinRandomRoom();
			Debug.LogWarningFormat("JoinOfflineRoom join random room");
		}
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.ClearRichPresence();
		}
		if (offlineRequested)
		{
			Debug.LogWarningFormat("OnDisconnected() was called by offlineRequested=true");
			JoinOfflineRoom();
		}
		else if (PhotonNetwork.OfflineMode)
		{
			JoinOfflineRoom();
			Debug.LogWarningFormat("OnDisconnected() skipped photon in offline mode");
		}
		else if (cause != DisconnectCause.None)
		{
			CancelInvoke("ShowDisconnectedPanel");
			Invoke("ShowDisconnectedPanel", 4f);
			Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
			GameManager.Instance.Log("Connexion Lost");
			EnablePlayButtons(enable: false);
			roomStatus = RoomStatus.Disconnected;
		}
	}

	private void ShowDisconnectedPanel()
	{
		DisconnectedPanel.SetActive(value: true);
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		base.OnMasterClientSwitched(newMasterClient);
		AddLog("Host switched to " + newMasterClient.NickName);
	}

	public void OnSelectRegion(string code)
	{
		Connect(code);
		GameManager.Instance.Log("Connection to region " + code);
	}

	public void OnSelectBestRegion()
	{
		Debug.Log("deleting saved region");
		PlayerPrefs.DeleteKey("PHOTON_REGION");
		PlayerPrefs.Save();
		Connect("");
		GameManager.Instance.Log("Connection to the best region ...");
	}

	private void FillRegionsList()
	{
		enableRegionSwitch = false;
		string region = GetRegion();
		RegionList.options.Clear();
		int value = -1;
		RegionList.options.Add(new Dropdown.OptionData("Best Region"));
		for (int i = 0; i < regions.Length; i++)
		{
			RegionList.options.Add(new Dropdown.OptionData(regions[i]));
			if (region == codes[i])
			{
				value = i + 1;
			}
		}
		RegionList.value = -1;
		RegionList.value = value;
		enableRegionSwitch = true;
	}

	public void OnRegionListChanged(int selected_id)
	{
		string region = GetRegion();
		if (selected_id == 0 && enableRegionSwitch)
		{
			OnSelectBestRegion();
		}
		if (selected_id > 0 && selected_id - 1 < regions.Length && enableRegionSwitch && region != codes[selected_id - 1])
		{
			OnSelectRegion(codes[selected_id - 1]);
		}
	}

	public override void OnJoinedLobby()
	{
		base.OnJoinedLobby();
		Debug.Log("OnJoinedLobby");
	}

	public override void OnLeftLobby()
	{
		Debug.Log("OnLeftLobby");
		availableRoomsList.Clear();
	}

	public void RejoinLobby()
	{
		if (!IsInvoking("JoinLobby"))
		{
			if (PhotonNetwork.InLobby)
			{
				PhotonNetwork.LeaveLobby();
			}
			CancelInvoke("JoinLobby");
			Invoke("JoinLobby", 1f);
		}
	}

	public void JoinLobby()
	{
		if (!PhotonNetwork.InLobby)
		{
			PhotonNetwork.JoinLobby();
		}
	}

	public void ShowSpectateButton()
	{
		if (GameManager.Instance.TestMode)
		{
			Statics.SetActive(SpectateBtn.gameObject, active: true);
			return;
		}
		bool active = false;
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if ((bool)character && !character.isBot && !character.IsDead())
			{
				active = true;
				break;
			}
		}
		Statics.SetActive(SpectateBtn.gameObject, active);
	}

	private void PlayersFindSquads()
	{
		CharacterMultiplayer[] array = PlayerList();
		foreach (CharacterMultiplayer characterMultiplayer in array)
		{
			if ((bool)characterMultiplayer)
			{
				characterMultiplayer.FillSquad(squadsList);
			}
		}
	}

	private bool SquadAddPlayer(int squadId, string nickname, int actorNumber)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return false;
		}
		Squad squad = null;
		foreach (Squad squads in squadsList)
		{
			if (squads != null && squads.id == squadId)
			{
				squad = squads;
				break;
			}
		}
		if (squad == null)
		{
			squad = new Squad();
			squad.id = squadId;
			squadsList.Add(squad);
		}
		SquadPlayer squadPlayer = new SquadPlayer();
		squadPlayer.name = nickname;
		squadPlayer.ActorNumber = actorNumber;
		return squad.AddPlayer(squadPlayer);
	}

	private bool SquadRemovePlayer(int squadId, string nickname, int actorNumber)
	{
		if (!PhotonNetwork.IsMasterClient || roomStatus != RoomStatus.Waiting)
		{
			return false;
		}
		Squad squad = squadsList.Find((Squad p) => p.id == squadId);
		if (squad == null)
		{
			return false;
		}
		SquadPlayer squadPlayer = squad.squadPlayers.Find((SquadPlayer p) => p.name == nickname && p.ActorNumber == actorNumber);
		if (squadPlayer != null)
		{
			squad.squadPlayers.Remove(squadPlayer);
		}
		Debug.Log($"player removed from squad {squadId} {nickname} {actorNumber}");
		return true;
	}

	private void TryJoinSquad(int requestedSquadId, string nickname, int actorNumber)
	{
		if (!PhotonNetwork.IsMasterClient && requestedSquadId != 0)
		{
			base.photonView.RPC("SquadJoinRequest", RpcTarget.MasterClient, requestedSquadId, nickname, actorNumber);
		}
	}

	[PunRPC]
	private void SquadJoinRequest(int squadId, string nickname, int actorNumber)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			Debug.Log($"SquadJoinRequest {squadId} {nickname} {actorNumber}");
			SquadAddPlayer(squadId, nickname, actorNumber);
			SendSquadData();
		}
	}

	[PunRPC]
	private void SquadsData(string json)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			Debug.Log("received squads json " + json);
			if (!string.IsNullOrEmpty(json))
			{
				SquadListWrapper squadListWrapper = new SquadListWrapper();
				squadListWrapper = JsonUtility.FromJson<SquadListWrapper>(json);
				squadsList = squadListWrapper.squads;
				string text = JsonUtility.ToJson(new SquadListWrapper
				{
					squads = squadsList
				});
				Debug.Log("squads json from received data " + text);
				PlayersFindSquads();
				Debug.Log("Received squads data. Total squads: " + squadsList.Count);
			}
		}
	}

	private void SendSquadData()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			string text = JsonUtility.ToJson(new SquadListWrapper
			{
				squads = squadsList
			});
			Debug.Log("squads json " + text);
			base.photonView.RPC("SquadsData", RpcTarget.All, text);
		}
	}

	private void UpdateSquads()
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		CharacterMultiplayer[] array = PlayerList();
		int num = 1;
		foreach (Squad squads in squadsList)
		{
			if (squads.squadPlayers.Count > num)
			{
				num = squads.squadPlayers.Count;
			}
		}
		if (num == 1)
		{
			Debug.Log("No squads match");
			foreach (Squad squads2 in squadsList)
			{
				foreach (SquadPlayer squadPlayer2 in squads2.squadPlayers)
				{
					Debug.Log($"update squad {squadPlayer2.name} {squadPlayer2.ActorNumber} {squads2.id}");
				}
			}
		}
		else
		{
			int num2 = ((squadsList.Count > 0) ? Mathf.Max(squadsList.ConvertAll((Squad s) => s.id).ToArray()) : 0);
			List<CharacterMultiplayer> list = new List<CharacterMultiplayer>();
			CharacterMultiplayer[] array2 = array;
			foreach (CharacterMultiplayer player in array2)
			{
				if (!player)
				{
					continue;
				}
				bool flag = false;
				foreach (Squad squads3 in squadsList)
				{
					if (squads3.squadPlayers.Exists((SquadPlayer p) => p.name == player.Nickname && p.ActorNumber == player.ActorNumber))
					{
						player.SquadId = squads3.id;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					player.SquadId = 0;
					list.Add(player);
				}
			}
			for (int num4 = 0; num4 < list.Count; num4 += num)
			{
				num2++;
				Squad squad = new Squad
				{
					id = num2
				};
				int num5 = Mathf.Min(num, list.Count - num4);
				for (int num6 = 0; num6 < num5; num6++)
				{
					CharacterMultiplayer characterMultiplayer = list[num4 + num6];
					SquadPlayer squadPlayer = new SquadPlayer
					{
						name = characterMultiplayer.Nickname,
						ActorNumber = characterMultiplayer.ActorNumber
					};
					squad.AddPlayer(squadPlayer);
					characterMultiplayer.SquadId = squad.id;
				}
				squadsList.Add(squad);
			}
			foreach (Squad squads4 in squadsList)
			{
				foreach (SquadPlayer squadPlayer3 in squads4.squadPlayers)
				{
					Debug.Log($"update squad {squadPlayer3.name} {squadPlayer3.ActorNumber} {squads4.id}");
				}
			}
		}
		PlayersFindSquads();
		SendSquadData();
	}

	public bool IsSquadMatch()
	{
		return squadsList.Count > 1;
	}

	public void JoinRandomRoom(bool _bots_only = false)
	{
		RoomTxt.text = "...";
		SquadTxt.text = "...";
		RoomConnectedPlayers.text = "0";
		QuitWaitingBtn.interactable = false;
		bots_only = _bots_only;
		if (PhotonNetwork.IsConnectedAndReady || PhotonNetwork.OfflineMode)
		{
			if (PhotonNetwork.InLobby)
			{
				PhotonNetwork.LeaveLobby();
			}
			if (_bots_only)
			{
				GameManager.Instance.Log("Joining bots only match ...");
				CreateFakeRoomImmediate(GetRandomRoomName(), MaxPlayers, randomMap: false, randomMode: false);
			}
			else
			{
				GameManager.Instance.Log("Joining random match ...");
				PhotonNetwork.JoinRandomRoom();
			}
		}
		else
		{
			ShowDisconnectedPanel();
		}
	}

	public void JoinRoom(string room)
	{
		if (string.IsNullOrEmpty(room))
		{
			GameManager.Instance.Log("Match name can'be empty!");
			EnablePlayButtons(enable: true);
			WaitingPanel.SetActive(value: false);
			return;
		}
		RoomTxt.text = "...";
		SquadTxt.text = "...";
		RoomConnectedPlayers.text = "0";
		QuitWaitingBtn.interactable = false;
		requestedSquadId = 0;
		string text = squadInputField.text;
		if (!string.IsNullOrEmpty(text))
		{
			int.TryParse(text, out requestedSquadId);
		}
		if (PhotonNetwork.IsConnectedAndReady)
		{
			if (PhotonNetwork.InLobby)
			{
				PhotonNetwork.LeaveLobby();
			}
			GameManager.Instance.Log("Joining match " + room);
			PhotonNetwork.JoinRoom(room);
		}
		else
		{
			ShowDisconnectedPanel();
		}
	}

	public void JoinFriendRoom()
	{
		JoinRoom(friendRoom.text);
	}

	public void JoinFriendRoomViaSteam()
	{
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.ShowFriendsList();
		}
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public void CreateRoom(string room, bool _private_match = false)
	{
		joinedFakeRandomRoom = false;
		private_match = _private_match;
		if (private_match)
		{
			AllowBots = false;
		}
		else
		{
			AllowBots = true;
		}
		if (PhotonNetwork.IsConnectedAndReady)
		{
			if (PhotonNetwork.InLobby)
			{
				PhotonNetwork.LeaveLobby();
			}
			GameManager.Instance.Log("Creating match " + room);
			RoomOptions roomOptions = new RoomOptions
			{
				IsVisible = !private_match,
				IsOpen = true,
				MaxPlayers = (byte)MaxPlayers,
				PublishUserId = true
			};
			PhotonNetwork.CreateRoom(room, roomOptions);
		}
		else
		{
			ShowDisconnectedPanel();
		}
	}

	public void CreateFakeRoomImmediate(string roomName, int maxPlayers, bool randomMap = true, bool randomMode = true, string map = "", int matchMode = -1, int roomTime = 0, bool isVisible = true, bool isOpen = true)
	{
		joinedFakeRandomRoom = true;
		if (PhotonNetwork.IsConnectedAndReady)
		{
			if (PhotonNetwork.InLobby)
			{
				PhotonNetwork.LeaveLobby();
			}
			if (bots_only)
			{
				roomName += "-bots";
				Debug.Log("creating bots only room " + roomName);
				isVisible = false;
				isOpen = false;
				GameManager.Instance.Log("Joining bots only match " + roomName);
			}
			else
			{
				GameManager.Instance.Log("Joining match " + roomName);
			}
			RoomOptions roomOptions = new RoomOptions
			{
				IsVisible = isVisible,
				IsOpen = isOpen,
				MaxPlayers = (byte)maxPlayers,
				PublishUserId = true
			};
			PhotonNetwork.CreateRoom(roomName, roomOptions);
		}
		else
		{
			ShowDisconnectedPanel();
		}
	}

	public void CreateFakeRoomImmediate(string roomName)
	{
		CreateFakeRoomImmediate(roomName, MaxPlayers);
	}

	public void CreateFakeRoomImmediate()
	{
		CreateFakeRoomImmediate(GetRandomRoomName(), MaxPlayers);
	}

	public void JoinFakeAvailableRoomImmediate(int room_index)
	{
		OnResetMatchSetting();
		if (fakeRooms != null && fakeRooms.Length != 0 && room_index < fakeRooms.Length)
		{
			FakeRoom fakeRoom = fakeRooms[room_index];
			CreateFakeRoomImmediate(fakeRoom.name, fakeRoom.maxPlayers, randomMap: false, randomMode: false, fakeRoom.map, (int)fakeRoom.mode);
		}
		else
		{
			CreateFakeRoomImmediate();
		}
	}

	private void UpdateFakeRooms()
	{
		int num = UnityEngine.Random.Range(1, 5);
		fakeRooms = null;
		fakeRooms = new FakeRoom[num];
		for (int i = 0; i < num; i++)
		{
			fakeRooms[i] = new FakeRoom();
			fakeRooms[i].name = GetRandomRoomName();
			fakeRooms[i].map = "";
			fakeRooms[i].map_name = "";
			fakeRooms[i].mode = (MatchMode)UnityEngine.Random.Range(0, 2);
			fakeRooms[i].maxPlayers = MaxPlayers;
		}
	}

	public RoomStatus GetRoomStatus()
	{
		return roomStatus;
	}

	public void RefreshLists()
	{
		CancelInvoke("RefreshLists");
		Invoke("RefreshLists", RefreshListsDelay);
		if (roomStatus == RoomStatus.Waiting)
		{
			PlayersFindSquads();
		}
		_ = "fps:" + (int)(1f / Time.smoothDeltaTime) + " ping:" + PhotonNetwork.GetPing();
	}

	private void UpdateCachedRoomList(List<RoomInfo> roomList)
	{
		foreach (RoomInfo room in roomList)
		{
			if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
			{
				if (availableRoomsList.ContainsKey(room.Name))
				{
					availableRoomsList.Remove(room.Name);
				}
			}
			else if (availableRoomsList.ContainsKey(room.Name))
			{
				availableRoomsList[room.Name] = room;
			}
			else
			{
				availableRoomsList.Add(room.Name, room);
			}
		}
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		UpdateCachedRoomList(roomList);
	}

	public override void OnJoinedRoom()
	{
		RoomTxt.text = PhotonNetwork.CurrentRoom.Name;
		SquadTxt.text = SquadId.ToString();
		RoomConnectedPlayers.text = playersList.Count + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
		QuitWaitingBtn.interactable = true;
		timeElapsedLobby = 0f;
		timeElapsedNextMatch = 0f;
		roomStatus = RoomStatus.Waiting;
		onJoinedRoom?.Invoke();
		Debug.Log("OnJoinedRoom()");
		GameManager.Instance.Log("Match joined " + PhotonNetwork.CurrentRoom.Name);
		SetDefaultRoomProperties();
		Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.");
		if (PhotonNetwork.IsMasterClient)
		{
			int actorNumber = GenerateActorNumber();
			CreatePlayer(PhotonNetwork.LocalPlayer.NickName, GenerateActorNumber(), SquadId, IsBot: false);
			SquadAddPlayer(SquadId, PhotonNetwork.LocalPlayer.NickName, actorNumber);
		}
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.SetRichPresence(RoomTxt.text, SquadTxt.text);
		}
	}

	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		GameManager.Instance.Log("Join random room failed! " + message);
		Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
		CreateFakeRoomImmediate();
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		base.OnJoinRoomFailed(returnCode, message);
		GameManager.Instance.Log("Join room failed! " + message);
		Debug.Log("OnJoinRoomFailed" + message);
		EnablePlayButtons(enable: true);
		WaitingPanel.SetActive(value: false);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		GameManager.Instance.Log("Create room failed! " + message);
		Debug.Log("OnCreateRoomFailed" + message);
		EnablePlayButtons(enable: true);
		WaitingPanel.SetActive(value: false);
	}

	public override void OnLeftRoom()
	{
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.ClearRichPresence();
		}
		CancelInvoke("RoomTimeTick");
		Debug.Log("OnLeftRoom");
		LoadMainMenu();
	}

	public override void OnPlayerEnteredRoom(Player other)
	{
		Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName);
		if (PhotonNetwork.IsMasterClient)
		{
			base.photonView.RPC("CreateMainLocalPlayer", other, other.NickName, GenerateActorNumber(), other.ActorNumber, 0);
		}
	}

	public override void OnPlayerLeftRoom(Player other)
	{
		Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName);
		CharacterMultiplayer playerController = GetPlayerController(other);
		if ((bool)playerController)
		{
			playerController.RemovedFromRoom();
			SquadRemovePlayer(playerController.SquadId, playerController.Nickname, playerController.ActorNumber);
		}
	}

	private void SetDefaultRoomProperties()
	{
		if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
		{
			Debug.Log("SetDefaultRoomProperties by " + PhotonNetwork.LocalPlayer.NickName);
		}
	}

	private void UpdateWaitingLobby()
	{
		if (PhotonNetwork.InRoom)
		{
			Statics.SetActive(leftSquad.gameObject, IsSquadMatch());
			RoomConnectedPlayers.text = playersList.Count + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
			SquadTxt.text = SquadId.ToString();
			CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
			if ((bool)mainPlayer)
			{
				SquadTxt.text = mainPlayer.SquadId.ToString();
			}
			float num = 5f;
			float num2 = WaitingTime;
			if (Application.isEditor)
			{
				num2 = WaitingTimeEditor;
			}
			if (PhotonNetwork.OfflineMode)
			{
				num2 = 1f;
				num = 1f;
			}
			timeElapsedLobby += Time.deltaTime;
			float num3 = num + num2 - timeElapsedLobby;
			if (num3 < 0f)
			{
				num3 = 0f;
			}
			WaitingTimerTxt.text = Statics.ElapsedTimeToTimeFormat(num3);
			if (PhotonNetwork.IsMasterClient && roomStatus == RoomStatus.Waiting)
			{
				if (PlayerList().Length >= PhotonNetwork.CurrentRoom.MaxPlayers && roomStatus == RoomStatus.Waiting && !PhotonNetwork.OfflineMode)
				{
					roomStatus = RoomStatus.PrePlaying;
					PhotonNetwork.CurrentRoom.IsOpen = false;
					StartCoroutine(RPC_StartMatch(num));
				}
				if ((timeElapsedLobby > num2 || PhotonNetwork.OfflineMode) && roomStatus == RoomStatus.Waiting)
				{
					roomStatus = RoomStatus.PrePlaying;
					PhotonNetwork.CurrentRoom.IsOpen = false;
					CreateBots();
					StartCoroutine(RPC_StartMatch(num));
				}
			}
		}
		else
		{
			RoomConnectedPlayers.text = ".. / ..";
			WaitingTimerTxt.text = "";
		}
	}

	private void CreateBots()
	{
		int num = UnityEngine.Random.Range(Mathf.Max(PlayerList().Length, MinPlayers), PhotonNetwork.CurrentRoom.MaxPlayers + 1);
		if (PhotonNetwork.OfflineMode)
		{
			num = MaxPlayers;
			Debug.Log("CreateBots " + num);
		}
		if (PhotonNetwork.IsMasterClient)
		{
			for (int i = PlayerList().Length; i < num; i++)
			{
				CreateBot();
			}
		}
	}

	private void LoadMainMenu()
	{
		Reconnect();
	}

	public void RPC_MatchFinished()
	{
		if (PhotonNetwork.IsMasterClient && roomStatus == RoomStatus.Playing)
		{
			roomStatus = RoomStatus.Finish;
			base.photonView.RPC("MatchFinished", RpcTarget.All);
		}
	}

	public void RPC_RestartMatch()
	{
		if (PhotonNetwork.IsMasterClient && roomStatus == RoomStatus.Finish)
		{
			roomStatus = RoomStatus.Playing;
			GameManager.Instance.ResetPlaytime();
			float num = UnityEngine.Random.Range(0f, 360f);
			base.photonView.RPC("StartMatch", RpcTarget.All, num);
		}
	}

	private IEnumerator RPC_StartMatch(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.CurrentRoom.IsOpen = false;
			PhotonNetwork.CurrentRoom.IsVisible = true;
			roomStatus = RoomStatus.Playing;
			GameManager.Instance.ResetPlaytime();
			UpdateSquads();
			float num = UnityEngine.Random.Range(0f, 360f);
			base.photonView.RPC("StartMatch", RpcTarget.All, num);
		}
	}

	[PunRPC]
	private void StartMatch(float airplaneRotation)
	{
		_ = PhotonNetwork.IsMasterClient;
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.ClearRichPresence();
		}
		roomStatus = RoomStatus.Playing;
		GameManager.Instance.OnMatchStarted();
		onMatchStarted?.Invoke();
		SendAllPlayersMessage("OnMatchStarted");
		GetComponent<DamageZoneManager>().OnMatchStarted();
		GetComponent<AirplaneManager>().OnMatchStarted();
		GetComponent<PickupsManager>().OnMatchStarted();
		GetComponent<KillsLogManager>().OnMatchStarted();
		GetComponent<AirplaneManager>().airplaneRotation.localRotation = Quaternion.Euler(0f, airplaneRotation, 0f);
	}

	[PunRPC]
	private void MatchFinished()
	{
		roomStatus = RoomStatus.Finish;
		GameManager.Instance.OnMatchFinished();
		onMatchFinishedImmediate?.Invoke();
		CancelInvoke("InvokeMatchFinishedEvent");
		Invoke("InvokeMatchFinishedEvent", 5f);
		SendAllPlayersMessage("OnMatchFinished");
		GetComponent<DamageZoneManager>().OnMatchFinished();
		GetComponent<AirplaneManager>().OnMatchFinished();
		GetComponent<PickupsManager>().OnMatchFinished();
		GetComponent<KillsLogManager>().OnMatchFinished();
		List<CharacterMultiplayer> rankedWinners = GetRankedWinners();
		bool squad = IsSquadMatch();
		GetComponent<WinnersManager>().Show(squad, rankedWinners.ToArray());
	}

	private void InvokeMatchFinishedEvent()
	{
		onMatchFinished?.Invoke();
		GameManager.Instance.OnMatchFinishedEndScreen();
		SendAllPlayersMessage("OnMatchFinishedEndScreen");
	}

	private List<CharacterMultiplayer> GetRankedWinners()
	{
		CharacterMultiplayer.GetMainPlayer();
		List<CharacterMultiplayer> list = new List<CharacterMultiplayer>();
		if (IsSquadMatch())
		{
			if (GetComponent<WinnersManager>().GetRemainingSquadsIds().Count > 0)
			{
				int num = GetComponent<WinnersManager>().GetRemainingSquadsIds()[0];
				foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
				{
					if ((bool)character && character.SquadId == num)
					{
						list.Add(character);
					}
				}
				list = (from c in list
					orderby c.match_rank, c.kills descending
					select c).Take(5).ToList();
			}
		}
		else
		{
			list = (from c in PlayerList()
				orderby c.match_rank, c.kills descending
				select c).Take(3).ToList();
		}
		return list;
	}

	public void CheckMatchEnd()
	{
		if (!IsMasterClient() || GetRoomStatus() != RoomStatus.Playing || !(GameManager.Instance.GetPlayTime() > 60f))
		{
			return;
		}
		bool flag = false;
		if (IsSquadMatch())
		{
			if (GetComponent<WinnersManager>().GetRemainingSquadsIds().Count <= 1)
			{
				flag = true;
			}
		}
		else if (GetComponent<WinnersManager>().GetRemaining() <= 1)
		{
			flag = true;
		}
		if (flag)
		{
			RPC_MatchFinished();
		}
	}

	public void Dead(byte shooterActorId)
	{
		GameManager.Instance.OnDead();
		onDead?.Invoke();
	}

	public void Respawn()
	{
		GameManager.Instance.OnRespawn();
		onRespawn?.Invoke();
	}

	[PunRPC]
	private void UpdateDamageZone(byte actionId, byte isFirstTime, Vector3 pos, float scalex)
	{
		Debug.Log($"RPC UpdateDamageZone {actionId} {pos} {scalex}");
		DamageZoneManager component = GetComponent<DamageZoneManager>();
		switch (actionId)
		{
		case 0:
			component.OnShowAppearsInMsg();
			break;
		case 1:
			component.OnShowShrinkInMsg();
			break;
		case 2:
			component.OnShowShrinkingMsg();
			break;
		}
		component.target_damageZone.position = pos;
		component.target_damageZone.localScale = new Vector3(scalex, component.target_damageZone.localScale.y, scalex);
	}

	public void RPC_UpdateDamageZone(byte actionId, byte isFirstTime)
	{
		DamageZoneManager component = GetComponent<DamageZoneManager>();
		if (PhotonNetwork.IsMasterClient)
		{
			if (actionId == 1 && isFirstTime == 0)
			{
				component.ReduceTargetZoneCircle();
			}
			base.photonView.RPC("UpdateDamageZone", RpcTarget.All, actionId, isFirstTime, component.target_damageZone.position, component.target_damageZone.localScale.x);
		}
	}

	public CharacterMultiplayer[] PlayerList()
	{
		return CharacterMultiplayer.characters.ToArray();
	}

	private int GenerateActorNumber()
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return 0;
		}
		int num = 0;
		int num2 = 100;
		bool flag = false;
		CharacterMultiplayer[] array = PlayerList();
		for (int i = 1; i <= num2; i++)
		{
			flag = false;
			CharacterMultiplayer[] array2 = array;
			foreach (CharacterMultiplayer characterMultiplayer in array2)
			{
				if (i == characterMultiplayer.ActorNumber)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				num = i;
				break;
			}
		}
		if (num == 0)
		{
			num = UnityEngine.Random.Range(1, 9999);
		}
		return num;
	}

	[PunRPC]
	private void CreateMainLocalPlayer(string NickName, int ActorNumber, int photonActorNumber, int TeamId)
	{
		if (PhotonNetwork.LocalPlayer.NickName == NickName && PhotonNetwork.LocalPlayer.ActorNumber == photonActorNumber)
		{
			CreatePlayer(NickName, ActorNumber, TeamId, IsBot: false);
			if (!PhotonNetwork.IsMasterClient)
			{
				TryJoinSquad(requestedSquadId, NickName, ActorNumber);
			}
		}
	}

	private GameObject CreatePlayer(string NickName, int ActorNumber, int TeamId, bool IsBot)
	{
		if (!PhotonNetwork.IsMasterClient && IsBot)
		{
			return null;
		}
		object[] array = new object[6] { NickName, ActorNumber, "", TeamId, IsBot, null };
		int num = 1;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 1;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		CharacterCustomizationManager component = GetComponent<CharacterCustomizationManager>();
		DataManager component2 = GetComponent<DataManager>();
		string text = appUniqueId;
		num = component2.GetInt("cc_body_" + text, component.body_default);
		num2 = component2.GetInt("cc_head_" + text, component.head_default);
		num3 = component2.GetInt("cc_neck_" + text, component.neck_default);
		num4 = component2.GetInt("cc_glasses_" + text, component.glasses_default);
		num5 = component2.GetInt("cc_earmuffs_" + text, component.earmuffs_default);
		num6 = component2.GetInt("cc_beard_" + text, component.beard_default);
		num7 = component2.GetInt("cc_hair_" + text, component.hair_default);
		num8 = component2.GetInt("cc_facemask_" + text, component.facemask_default);
		num9 = component2.GetInt("cc_vest_" + text, component.vest_default);
		num10 = component2.GetInt("cc_bag_" + text, component.bag_default);
		num11 = component2.GetInt("cc_parachute_" + text, component.parachute_default);
		if (IsBot)
		{
			num = component.body_default;
			num2 = component.head_default;
			num3 = component.neck_default;
			num4 = component.glasses_default;
			num5 = component.earmuffs_default;
			num6 = component.beard_default;
			num7 = component.hair_default;
			num8 = component.facemask_default;
			num9 = component.vest_default;
			num10 = component.bag_default;
			num11 = component.parachute_default;
			if (UnityEngine.Random.value > 0.7f)
			{
				MenuCharacter mainMenuCharacter = GetComponent<MenuCharactersManager>().GetMainMenuCharacter();
				ItemsCollectionsSync.ItemsCollection collection = ItemsCollectionsSync.GetCollection(mainMenuCharacter.itemsCollections, "body");
				ItemsCollectionsSync.ItemsCollection collection2 = ItemsCollectionsSync.GetCollection(mainMenuCharacter.itemsCollections, "hair");
				if (bot_random_body)
				{
					num = UnityEngine.Random.Range(0, collection.items.Count);
				}
				num2 = component.head_default;
				num3 = component.neck_default;
				num4 = component.glasses_default;
				num5 = component.earmuffs_default;
				num6 = component.beard_default;
				if (bot_random_hair)
				{
					num7 = UnityEngine.Random.Range(0, collection2.items.Count);
				}
				num8 = component.facemask_default;
				num9 = component.vest_default;
				num10 = component.bag_default;
				num11 = component.parachute_default;
			}
		}
		array[5] = ItemsCollectionsSync.EncodeBodyData(num, num2, num3, num4, num5, num6, num7, num8, num9, num10, num11);
		Vector3 position = Vector3.zero;
		Quaternion rotation = Quaternion.identity;
		Transform transform = Spawns.transform.Find("0_" + ActorNumber);
		if ((bool)transform)
		{
			position = transform.transform.position;
			rotation = transform.transform.rotation;
			array[2] = transform.name;
		}
		if (!IsBot)
		{
			return PhotonNetwork.Instantiate(PlayerPrefabName, position, rotation, 0, array);
		}
		return PhotonNetwork.InstantiateRoomObject(PlayerPrefabName, position, rotation, 0, array);
	}

	public CharacterMultiplayer GetLocalPlayerController()
	{
		CharacterMultiplayer[] array = PlayerList();
		foreach (CharacterMultiplayer characterMultiplayer in array)
		{
			if (characterMultiplayer.IsLocalMainPlayer())
			{
				return characterMultiplayer;
			}
		}
		return null;
	}

	public CharacterMultiplayer GetPlayerController(string NickName, int ActorNumber)
	{
		return GetPlayerController(NickName + " " + ActorNumber);
	}

	public CharacterMultiplayer GetPlayerController(string playerName)
	{
		CharacterMultiplayer[] array = PlayerList();
		foreach (CharacterMultiplayer characterMultiplayer in array)
		{
			if (characterMultiplayer.name == playerName)
			{
				return characterMultiplayer;
			}
		}
		return null;
	}

	public CharacterMultiplayer GetPlayerController(Player player)
	{
		return GetPlayerController(player.NickName, player.ActorNumber);
	}

	public void ResetPlayersScores()
	{
		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			CharacterMultiplayer[] array = PlayerList();
			for (int i = 0; i < array.Length; i++)
			{
				_ = array[i];
			}
		}
	}

	public void SendLocalPlayersMessage(string msg)
	{
		CharacterMultiplayer[] array = PlayerList();
		foreach (CharacterMultiplayer characterMultiplayer in array)
		{
			if (characterMultiplayer.isLocal)
			{
				characterMultiplayer.gameObject.SendMessage(msg, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	public void SendAllPlayersMessage(string msg)
	{
		CharacterMultiplayer[] array = PlayerList();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SendMessage(msg, SendMessageOptions.DontRequireReceiver);
		}
	}

	public bool IsMasterClient()
	{
		return PhotonNetwork.IsMasterClient;
	}

	public void OnNicknameChanged(string n)
	{
		PlayerName = n;
		PhotonNetwork.NickName = PlayerName;
	}

	private GameObject CreateBot(string bot_name = "")
	{
		if (!AllowBots)
		{
			return null;
		}
		CharacterMultiplayer[] array = PlayerList();
		if (array != null && array.Length >= MaxPlayers)
		{
			return null;
		}
		if (bot_name == "")
		{
			bot_name = GetRandomBotName();
		}
		return CreatePlayer(bot_name, GenerateActorNumber(), 0, IsBot: true);
	}

	private string GetRandomBotName()
	{
		_ = BotName + $"{UnityEngine.Random.Range(1, 9999):0000}";
		int num = UnityEngine.Random.Range(0, BotNames.botsNames.Length);
		return BotNames.botsNames[num];
	}

	private string GetRandomRoomName()
	{
		return "match" + $"{UnityEngine.Random.Range(1, 9999):0000}";
	}

	private void BotJoin()
	{
	}
}
