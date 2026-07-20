using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using InfimaGames.LowPolyShooterPack;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class SteamManager : MonoBehaviour
{
	public enum SteamAppVersion
	{
		Demo,
		Standard,
		Premium
	}

	public Text leaderboardTxt;

	public InputField playernameTxt;

	public SteamAppVersion steamAppVersion;

	public uint demo_app_id = 1000500u;

	public uint app_id = 1000500u;

	public uint premium_app_id = 1000500u;

	public bool SteamworksMustConnect = true;

	public static SteamManager instance;

	private bool init;

	private bool connectionFailedEventCalled;

	private bool initCalled;

	public bool iap_available;

	public string gameURL = "https://store.steampowered.com/app/2215270/Army_Troop/?beta=1";

	public string leaderboardURL = "https://store.steampowered.com/app/2215270/Army_Troop/?beta=1";

	public string leaderboardScores = "TopScores";

	public GameObject steamworksFailedUI;

	[Header("UI References")]
	public GameObject friendRowPrefab;

	public Transform contentParent;

	private List<SteamFriendRow> friendRows = new List<SteamFriendRow>();

	private int leaderboardScore;

	[Header("Prefab & Parent")]
	public GameObject rowPrefab;

	public Transform rowParent;

	[Header("Colors")]
	public UnityEngine.Color rank1 = new UnityEngine.Color(1f, 0.84f, 0f);

	public UnityEngine.Color rank2 = new UnityEngine.Color(0f, 1f, 0.5f);

	public UnityEngine.Color rank3 = new UnityEngine.Color(1f, 0.5f, 0f);

	public UnityEngine.Color user_rank = new UnityEngine.Color(1f, 0.5f, 0f);

	public UnityEngine.Color normal = UnityEngine.Color.white;

	public string CLOUD_FILE_NAME = "playerdata.json";

	public static event Action<string> connectionFailedEvent;

	public void OpenURL(string url)
	{
		UnityEngine.Debug.Log("url:" + url);
		SteamFriends.OpenWebOverlay(url);
	}

	public async Task FillLeaderboard()
	{
		try
		{
			OpenLeaderboard();
			await LoadLeaderboard();
		}
		catch (Exception arg)
		{
			UnityEngine.Debug.LogError($"FillLeaderboard crashed: {arg}");
		}
	}

	public async Task LoadLeaderboard()
	{
		UnityEngine.Debug.Log("LoadLeaderboard() started: " + leaderboardScores);
		ClearLeaderboard();
		Leaderboard? leaderboard = await SteamUserStats.FindLeaderboardAsync(leaderboardScores);
		if (!leaderboard.HasValue)
		{
			UnityEngine.Debug.LogError("FindLeaderboardAsync failed or leaderboard does not exist");
			return;
		}
		LeaderboardEntry[] array = await leaderboard.Value.GetScoresAroundUserAsync(-20);
		if (array == null || array.Length == 0)
		{
			UnityEngine.Debug.LogWarning("Leaderboard exists but returned no scores");
			return;
		}
		int num = 1;
		LeaderboardEntry[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			LeaderboardEntry leaderboardEntry = array2[i];
			SteamManager steamManager = this;
			int rank = num;
			Friend user = leaderboardEntry.User;
			steamManager.CreateLeaderboardRow(rank, user.Name, leaderboardEntry.Score);
			num++;
		}
		UnityEngine.Debug.Log($"Loaded {array.Length} leaderboard entries");
	}

	private void CreateLeaderboardRow(int rank, string name, int score)
	{
		GameObject obj = UnityEngine.Object.Instantiate(rowPrefab, rowParent);
		Text component = obj.transform.Find("Rank/Value").GetComponent<Text>();
		Text component2 = obj.transform.Find("Name/Value").GetComponent<Text>();
		Text component3 = obj.transform.Find("Score/Value").GetComponent<Text>();
		component.text = rank.ToString();
		component2.text = name;
		component3.text = score.ToString();
		UnityEngine.Color color;
		UnityEngine.Color color3;
		switch (rank)
		{
		case 1:
			color = (component3.color = rank1);
			color3 = (component2.color = color);
			component.color = color3;
			return;
		case 2:
			color = (component3.color = rank2);
			color3 = (component2.color = color);
			component.color = color3;
			return;
		case 3:
			color = (component3.color = rank3);
			color3 = (component2.color = color);
			component.color = color3;
			return;
		}
		color = (component3.color = normal);
		color3 = (component2.color = color);
		component.color = color3;
		if (component2.text == SteamClient.Name)
		{
			color = (component3.color = user_rank);
			color3 = (component2.color = color);
			component.color = color3;
		}
	}

	public void ClearLeaderboard()
	{
		for (int num = rowParent.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(rowParent.GetChild(num).gameObject);
		}
	}

	public void OpenLeaderboard()
	{
		OpenURL(leaderboardURL);
	}

	public void SendLeaderboardScore(int score)
	{
		leaderboardScore = score;
		SendSteamLeaderboardScore();
	}

	private async Task SendSteamLeaderboardScore()
	{
		UnityEngine.Debug.Log("steam leaderboard");
		Leaderboard? leaderboard = await SteamUserStats.FindLeaderboardAsync(leaderboardScores);
		if (leaderboard.HasValue)
		{
			UnityEngine.Debug.Log("sending leaderboard score");
			await leaderboard.Value.SubmitScoreAsync(leaderboardScore);
		}
	}

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		SteamCloudManager.FILE_NAME = CLOUD_FILE_NAME;
		Statics.SetActive(steamworksFailedUI, active: false);
		init = true;
		try
		{
			UnityEngine.Debug.Log("initializing steamworks appid:" + GetAppId());
			SteamClient.Init(GetAppId());
		}
		catch (Exception ex)
		{
			string message = ex.Message;
			UnityEngine.Debug.LogError(message);
			SteamManager.connectionFailedEvent?.Invoke(message);
			init = false;
		}
	}

	private uint GetAppId()
	{
		if (steamAppVersion == SteamAppVersion.Demo)
		{
			return demo_app_id;
		}
		if (steamAppVersion == SteamAppVersion.Premium)
		{
			return premium_app_id;
		}
		return app_id;
	}

	private void Start()
	{
		if (steamAppVersion == SteamAppVersion.Demo)
		{
			GameManager.Instance.IsDemo = true;
		}
		InitializeSteamJoinCallbacks();
	}

	private void Update()
	{
		if (!init && !connectionFailedEventCalled)
		{
			connectionFailedEventCalled = true;
			SteamManager.connectionFailedEvent?.Invoke("Could not initialize steam client!");
			if (SteamworksMustConnect && !GameManager.Instance.TestMode)
			{
				Statics.SetActive(steamworksFailedUI, active: true);
			}
		}
		if (init && !initCalled)
		{
			initCalled = true;
			playernameTxt.text = SteamClient.Name;
			playernameTxt.interactable = false;
			Statics.SetActive(steamworksFailedUI, active: false);
			RefreshFriendsList();
			SteamCloudManager.Load();
		}
	}

	public void OnDestroy()
	{
		if (instance == this)
		{
			UnityEngine.Debug.Log("shtting down steamworks");
			SteamClient.Shutdown();
		}
	}

	public void RestartGame()
	{
		if (SteamClient.IsValid)
		{
			UnityEngine.Debug.LogWarning("steam restart game");
			uint appid = SteamClient.AppId;
			SteamClient.Shutdown();
			SteamClient.RestartAppIfNecessary(appid);
		}
		else
		{
			UnityEngine.Debug.LogWarning("restart game manually");
			Process.Start(Application.dataPath.Replace("_Data", ".exe"));
			Application.Quit();
		}
	}

	public void RefreshFriendsList()
	{
		foreach (Transform item in contentParent)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		friendRows.Clear();
		if (!init)
		{
			UnityEngine.Debug.LogWarning("Steam not initialized yet");
			return;
		}
		foreach (Friend friend in SteamFriends.GetFriends())
		{
			SteamFriendRow component = UnityEngine.Object.Instantiate(friendRowPrefab, contentParent).GetComponent<SteamFriendRow>();
			component.Setup(friend);
			friendRows.Add(component);
		}
	}

	private void InitializeSteamJoinCallbacks()
	{
		SteamFriends.OnGameRichPresenceJoinRequested += OnRichPresenceJoinRequested;
		UnityEngine.Debug.Log("[SteamJoinManager] Callbacks initialized.");
	}

	public void SetRichPresence(string matchId, string squadId)
	{
		if (!init)
		{
			return;
		}
		SteamFriends.SetRichPresence("connect", "match=" + matchId + "&squad=" + squadId);
		SteamFriends.SetRichPresence("status", "In Match");
		SteamFriends.SetRichPresence("steam_display", "#Status_InMatch");
		UnityEngine.Debug.Log("[SteamJoinManager] Rich presence set: match=" + matchId + ", squad=" + squadId);
		SteamFriendRow[] array = UnityEngine.Object.FindObjectsOfType<SteamFriendRow>();
		foreach (SteamFriendRow steamFriendRow in array)
		{
			if ((bool)steamFriendRow)
			{
				steamFriendRow.connectString = "match=" + matchId + "&squad=" + squadId;
			}
		}
	}

	public void ClearRichPresence()
	{
		if (!init)
		{
			return;
		}
		SteamFriends.ClearRichPresence();
		UnityEngine.Debug.Log("[SteamJoinManager] ClearRichPresence");
		SteamFriendRow[] array = UnityEngine.Object.FindObjectsOfType<SteamFriendRow>();
		foreach (SteamFriendRow steamFriendRow in array)
		{
			if ((bool)steamFriendRow)
			{
				steamFriendRow.connectString = "";
			}
		}
	}

	public void ShowFriendsList()
	{
		if (init)
		{
			SteamFriends.OpenOverlay("Friends");
			UnityEngine.Debug.Log("[SteamJoinManager] Steam overlay opened (invite friend manually).");
		}
	}

	private void OnRichPresenceJoinRequested(Friend friend, string connectString)
	{
		if (init)
		{
			UnityEngine.Debug.Log("[SteamJoinManager] Joining the friend " + friend.Name + " with connect: " + connectString);
			HandleConnectCommand(connectString);
		}
	}

	private void HandleConnectCommand(string connectString)
	{
		string text = null;
		string text2 = null;
		string[] array = connectString.Split('&');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split('=');
			if (array2.Length == 2)
			{
				if (array2[0] == "match")
				{
					text = array2[1];
				}
				else if (array2[0] == "squad")
				{
					text2 = array2[1];
				}
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			UnityEngine.Debug.LogWarning("[SteamJoinManager] Invalid connect string: missing matchId");
			return;
		}
		UnityEngine.Debug.Log("[SteamJoinManager] Joining match=" + text + ", squad=" + text2);
		JoinPhotonMatch(text, text2);
	}

	private void JoinPhotonMatch(string matchId, string squadId)
	{
		UnityEngine.Debug.Log("[SteamJoinManager] [PHOTON] Joining Photon Room: " + matchId + " (squad: " + squadId + ")");
		if (GameManager.Instance == null)
		{
			UnityEngine.Debug.LogWarning("OnRichPresenceJoinRequested GameManager.Instance null");
			return;
		}
		MatchmakingManager component = GameManager.Instance.GetComponent<MatchmakingManager>();
		if ((bool)component)
		{
			component.friendRoom.text = matchId;
			component.squadInputField.text = squadId;
			UnityEngine.Debug.Log("[SteamJoinManager] match making Joining Room: " + component.friendRoom.text + " (squad: " + component.squadInputField.text + ")");
			component.JoinFriendRoomBtn.onClick.Invoke();
		}
		else
		{
			UnityEngine.Debug.LogWarning("matchmakingManager null");
		}
	}
}
