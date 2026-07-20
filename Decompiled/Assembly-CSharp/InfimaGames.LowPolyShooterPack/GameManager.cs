using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class GameManager : MonoBehaviour
{
	public bool IsDemo = true;

	public bool TestMode = true;

	public bool TestModeCleanPrefs = true;

	public bool CheatCodes = true;

	public static GameManager Instance;

	public Text LogTxt;

	public GameObject InGameMenu;

	public UnityEvent onShowInGameMenu;

	public UnityEvent onHideInGameMenu;

	public Text mainPlayerNameTxt;

	public Slider mainPlayerHealth;

	public Slider[] squadHealths;

	public Text[] squadNamesTxt;

	public Text spectatingPlayerNameTxt;

	public GameObject audioSourcesObj;

	public GameObject audioSourcesObjFar;

	public Button InGameMapButton;

	public Button InGameInventoryButton;

	public Image HealingImg;

	public Button[] demoDisabledButtons;

	public GameObject[] demoObjs;

	public AudioSource gameplayWind;

	public Text rewardRankTxt;

	public Text rewardKillsTxt;

	public Text rewardTimeTxt;

	public Text rewardHealthsTxt;

	public Text rewardExpTxt;

	public Text rewardGPTxt;

	public Text scoreTxt;

	public Text weaponsExpTxt;

	public Text cashTxt;

	public Material[] minimapBigPlaneScenesMaterials;

	public Material[] minimapSmallPlaneScenesMaterials;

	public MeshRenderer minimapBigPlane;

	public MeshRenderer minimapSmallPlane;

	public Transform homeSquad;

	public Transform winnersSquad;

	public Camera bigMapCamera;

	public RawImage compassImage;

	private float compassWidth = 360f;

	public Vector3 compassYWH = new Vector3(0.18f, 0.4f, 0.5f);

	public float compassYawOffset;

	public Transform reviewUI;

	private AudioSource[] audioSources;

	private AudioSource[] audioSourcesFar;

	private int currentAudioSource;

	private int currentAudioSourceFar;

	public bool isSpectating;

	private int frameId;

	private int frameHardComputingBotsCount = 10;

	private int frameMaxPlayers = 50;

	public int usedHealths;

	private DateTime startTime;

	private AirplaneManager airplaneManager;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			LogTxt.text = "";
			audioSources = audioSourcesObj.GetComponentsInChildren<AudioSource>();
			audioSourcesFar = audioSourcesObjFar.GetComponentsInChildren<AudioSource>();
			Statics.SetActive(GetComponent<SteamManager>().steamworksFailedUI, active: false);
			airplaneManager = GetComponent<AirplaneManager>();
			InputSystem.onDeviceChange += delegate(InputDevice device, InputDeviceChange change)
			{
				if (device is Gamepad && (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected))
				{
					Debug.Log("[Input] Re-initializing: " + device.displayName);
					InputSystem.ResetDevice(device);
				}
			};
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		NavMesh.pathfindingIterationsPerFrame = 500;
		Application.targetFrameRate = 300;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		bool interactable = true;
		if (IsDemo && !TestMode)
		{
			interactable = false;
		}
		Button[] array = demoDisabledButtons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].interactable = interactable;
		}
		interactable = false;
		if (IsDemo && !TestMode)
		{
			interactable = true;
		}
		GameObject[] array2 = demoObjs;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetActive(interactable);
		}
		reviewUI.gameObject.SetActive(value: false);
		if (GetComponent<ProgressionManager>().GetTotalPlayTime() > TimeSpan.FromMinutes(10.0) && !IsDemo)
		{
			reviewUI.gameObject.SetActive(value: true);
		}
		int num = SceneManager.GetActiveScene().buildIndex - 1;
		minimapBigPlane.material = minimapBigPlaneScenesMaterials[num];
		minimapSmallPlane.material = minimapSmallPlaneScenesMaterials[num];
		GameObject gameObject = GameObject.Find("MapHelpers/HomeSquadOverrides");
		GameObject gameObject2 = GameObject.Find("MapHelpers/WinnersSquadOverrides");
		GameObject gameObject3 = GameObject.Find("MapHelpers/BigMapCameraOverrides");
		if ((bool)gameObject)
		{
			homeSquad.localPosition = gameObject.transform.localPosition;
			homeSquad.localRotation = gameObject.transform.localRotation;
		}
		if ((bool)gameObject2)
		{
			winnersSquad.localPosition = gameObject2.transform.localPosition;
			winnersSquad.localRotation = gameObject2.transform.localRotation;
		}
		if ((bool)gameObject3)
		{
			bigMapCamera.transform.localPosition = gameObject3.transform.localPosition;
			bigMapCamera.orthographicSize = gameObject3.transform.localScale.x;
		}
	}

	private void Update()
	{
		UpdateGameplayAudio();
		UpdateHealths();
		UpdateSpectating();
		UpdateBots();
	}

	private void UpdateGameplayAudio()
	{
		bool flag = false;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			ThirdPerson component = mainPlayer.GetComponent<ThirdPerson>();
			if (component.fps_camera.enabled || component.tps_camera.enabled)
			{
				flag = true;
			}
		}
		if (flag && !gameplayWind.isPlaying)
		{
			gameplayWind.Play();
		}
		if (!flag && gameplayWind.isPlaying)
		{
			gameplayWind.Stop();
		}
	}

	private void UpdateBots()
	{
		int count = CharacterMultiplayer.characters.Count;
		int num = frameId * frameHardComputingBotsCount;
		int num2 = Mathf.Min(num + frameHardComputingBotsCount, count);
		for (int i = 0; i < count; i++)
		{
			CharacterMultiplayer characterMultiplayer = CharacterMultiplayer.characters[i];
			if ((bool)characterMultiplayer)
			{
				bool canDoHardComputingThisFrame = i >= num && i < num2;
				characterMultiplayer.GetComponent<CharacterBot>().canDoHardComputingThisFrame = canDoHardComputingThisFrame;
			}
		}
		frameId++;
		if (frameId * frameHardComputingBotsCount >= count)
		{
			frameId = 0;
		}
	}

	public void OnLockCursor(InputAction.CallbackContext context)
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!(mainPlayer == null) && context.phase == InputActionPhase.Performed)
		{
			if (mainPlayer.GetComponent<Character>().IsCursorLocked() && !mainPlayer.IsDead())
			{
				onShowInGameMenu?.Invoke();
			}
			if (!mainPlayer.GetComponent<Character>().IsCursorLocked() && !mainPlayer.IsDead())
			{
				onHideInGameMenu?.Invoke();
			}
		}
	}

	public void OnShowInGameMenu()
	{
		if (!(CharacterMultiplayer.GetMainPlayer() == null))
		{
			onShowInGameMenu?.Invoke();
		}
	}

	public void ShowSteamLeaderboard()
	{
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.OpenLeaderboard();
		}
	}

	public void FillSteamLeaderboard()
	{
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.FillLeaderboard();
		}
	}

	public void OnMap(InputAction.CallbackContext context)
	{
		if (!(CharacterMultiplayer.GetMainPlayer() == null) && context.phase == InputActionPhase.Performed)
		{
			OnLockCursor(context);
			if (InGameMenu.activeSelf)
			{
				InGameMapButton.onClick.Invoke();
				ShowCursor();
			}
			else
			{
				HideCursor();
			}
		}
	}

	public void OnInventory(InputAction.CallbackContext context)
	{
		if (!(CharacterMultiplayer.GetMainPlayer() == null) && context.phase == InputActionPhase.Performed)
		{
			OnLockCursor(context);
			if (InGameMenu.activeSelf)
			{
				InGameInventoryButton.onClick.Invoke();
				ShowCursor();
			}
			else
			{
				HideCursor();
			}
		}
	}

	public void HideCursor()
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!(mainPlayer == null))
		{
			mainPlayer.GetComponent<Character>().ShowCursor(show: false);
		}
	}

	public void ShowCursor()
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!(mainPlayer == null))
		{
			mainPlayer.GetComponent<Character>().ShowCursor(show: true);
		}
	}

	public void Spectate()
	{
		isSpectating = true;
	}

	public void OnMatchStarted()
	{
		isSpectating = false;
		startTime = DateTime.Now;
		usedHealths = 0;
	}

	public void OnMatchFinished()
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer && !mainPlayer.IsDead())
		{
			ComputeRewards(isMatchWinned: true);
		}
	}

	public void OnMatchFinishedEndScreen()
	{
		isSpectating = false;
	}

	public void OnDead()
	{
		isSpectating = false;
		ComputeRewards(isMatchWinned: false);
	}

	public void OnRespawn()
	{
		isSpectating = false;
	}

	private void UpdateSpectating()
	{
		CharacterMultiplayer[] array = CharacterMultiplayer.characters.ToArray();
		if (array == null)
		{
			return;
		}
		CharacterMultiplayer[] array2 = array;
		foreach (CharacterMultiplayer characterMultiplayer in array2)
		{
			if ((bool)characterMultiplayer)
			{
				characterMultiplayer.isSpectating = false;
			}
		}
		if (!isSpectating)
		{
			return;
		}
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (mainPlayer == null)
		{
			return;
		}
		foreach (CharacterMultiplayer item in mainPlayer.squad)
		{
			if ((bool)item && !item.IsDead() && !item.isBot)
			{
				item.isSpectating = true;
				spectatingPlayerNameTxt.text = item.Nickname;
				return;
			}
		}
		array2 = array;
		foreach (CharacterMultiplayer characterMultiplayer2 in array2)
		{
			if ((bool)characterMultiplayer2 && characterMultiplayer2 != mainPlayer && !characterMultiplayer2.IsDead() && !characterMultiplayer2.isBot)
			{
				characterMultiplayer2.isSpectating = true;
				spectatingPlayerNameTxt.text = characterMultiplayer2.Nickname;
				return;
			}
		}
		if (!TestMode)
		{
			return;
		}
		array2 = array;
		foreach (CharacterMultiplayer characterMultiplayer3 in array2)
		{
			if ((bool)characterMultiplayer3 && characterMultiplayer3 != mainPlayer && !characterMultiplayer3.IsDead())
			{
				characterMultiplayer3.isSpectating = true;
				spectatingPlayerNameTxt.text = characterMultiplayer3.Nickname;
				break;
			}
		}
	}

	private void UpdateHealths()
	{
		int num = -1;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			float y = mainPlayer.transform.eulerAngles.y;
			if (airplaneManager.AirplaneCamera.gameObject.activeInHierarchy)
			{
				y = airplaneManager.AirplaneCamera.transform.eulerAngles.y;
			}
			float x = Mathf.Repeat(0f - y + compassYawOffset, 360f) / 360f - 0.5f * compassYWH.y;
			compassImage.uvRect = new Rect(x, compassYWH.x, compassYWH.y, compassYWH.z);
			mainPlayerNameTxt.text = mainPlayer.Nickname;
			mainPlayerHealth.value = (float)(int)mainPlayer.health / 255f;
			if (mainPlayer.squad != null)
			{
				bool active = !mainPlayer.IsDead() && MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing;
				for (int i = 0; i < mainPlayer.squad.Count; i++)
				{
					int num2 = i;
					CharacterMultiplayer characterMultiplayer = mainPlayer.squad[i];
					if ((bool)characterMultiplayer)
					{
						Statics.SetActive(squadHealths[num2].gameObject, active);
						squadHealths[num2].value = (float)(int)characterMultiplayer.health / 255f;
						squadNamesTxt[num2].text = characterMultiplayer.Nickname;
						num = num2;
					}
					else
					{
						Statics.SetActive(squadHealths[num2].gameObject, active: false);
					}
				}
			}
		}
		for (int j = num + 1; j < squadHealths.Length; j++)
		{
			Statics.SetActive(squadHealths[j].gameObject, active: false);
		}
	}

	public void ResetPlaytime()
	{
		startTime = DateTime.Now;
	}

	public float GetPlayTime()
	{
		return (float)(DateTime.Now - startTime).TotalSeconds;
	}

	private void ComputeRewards(bool isMatchWinned)
	{
		TimeSpan timeSpan = DateTime.Now - startTime;
		string text = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
		TimeSpan maxPossibleTime = TimeSpan.FromMinutes(30.0);
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!mainPlayer)
		{
			return;
		}
		int num = 0;
		foreach (PickupsManager.Item item in mainPlayer.GetComponent<CharacterInventory>().inventory)
		{
			if (item != null && item.type == PickupsManager.ItemType.money)
			{
				num += item.value;
			}
		}
		int amount = mainPlayer.kills * GetComponent<ProgressionManager>().kill_to_gp + num;
		int num2 = (int)ComputeScore(mainPlayer.match_rank, mainPlayer.kills, timeSpan, usedHealths, maxPossibleTime);
		rewardRankTxt.text = "#" + mainPlayer.match_rank;
		rewardKillsTxt.text = mainPlayer.kills.ToString();
		rewardTimeTxt.text = text;
		rewardHealthsTxt.text = usedHealths.ToString();
		rewardExpTxt.text = num2.ToString();
		rewardGPTxt.text = amount.ToString();
		scoreTxt.text = num2.ToString();
		weaponsExpTxt.text = mainPlayer.kills.ToString();
		cashTxt.text = num.ToString();
		GetComponent<ProgressionManager>().AddPlayerExp(num2);
		GetComponent<ProgressionManager>().AddCoins(amount);
		GetComponent<ProgressionManager>().SaveStats(mainPlayer.match_rank, mainPlayer.kills, usedHealths, num2, timeSpan, isMatchWinned);
		if ((bool)SteamManager.instance)
		{
			SteamManager.instance.SendLeaderboardScore(num2);
		}
	}

	public static float ComputeScore(int rank, int kills, TimeSpan elapsedTime, int usedHealths, TimeSpan maxPossibleTime)
	{
		float num = Mathf.Clamp01((float)(50 - rank) / 49f);
		float num2 = 40f;
		float num3 = Mathf.Clamp01((float)kills / num2);
		float num4 = Mathf.Clamp01((float)(elapsedTime.TotalSeconds / maxPossibleTime.TotalSeconds));
		float num5 = Mathf.Min((float)usedHealths * 0.03f, 0.15f);
		return Mathf.Round((num * 0.55f + num3 * 0.3f + num4 * 0.15f) * (1f - num5) * 100f);
	}

	public void Log(string log)
	{
		LogTxt.text = log;
	}

	public AudioSource GetNextAudioSource()
	{
		AudioSource obj = audioSources[currentAudioSource];
		obj.Stop();
		currentAudioSource++;
		if (currentAudioSource >= audioSources.Length)
		{
			currentAudioSource = 0;
		}
		return obj;
	}

	public AudioSource GetNextAudioSourceFar()
	{
		AudioSource obj = audioSourcesFar[currentAudioSourceFar];
		obj.Stop();
		currentAudioSourceFar++;
		if (currentAudioSourceFar >= audioSourcesFar.Length)
		{
			currentAudioSourceFar = 0;
		}
		return obj;
	}

	public void Quit()
	{
		Application.Quit();
	}

	public void ChangeMap(string map)
	{
		Debug.Log("change map to " + map);
		PlayerPrefs.SetString("RestartScene", map);
		PlayerPrefs.Save();
		if (MatchmakingManager.Instance.IsConnected() && MatchmakingManager.Instance.IsInRoom())
		{
			MatchmakingManager.Instance.LeaveRoom();
			return;
		}
		MatchmakingManager.Instance.Reconnect();
		Debug.Log("MatchmakingManager.Instance.Reconnect");
	}
}
