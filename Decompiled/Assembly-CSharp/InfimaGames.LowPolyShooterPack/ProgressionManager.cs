using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class ProgressionManager : MonoBehaviour
{
	public Text weaponUpgradedNameTxt;

	public Text weaponUpgradedGradeTxt;

	public Image weaponUpgradedGradeIcon;

	public Text playerUpgradedGradeTxt;

	public Image playerUpgradedGradeIcon;

	public CanvasGroup weaponUpgraded;

	public CanvasGroup playerUpgraded;

	public Sprite[] rankIcons;

	public Text statsTxt;

	public Image profileGradeIcon;

	public Text profileGradeTxt;

	public Text profileCoinsTxt;

	public Slider profileExp;

	public int kill_to_gp = 10;

	public static int MaxWeaponGrade = 50;

	public static int MaxPlayerGrade = 500;

	[Header("\ud83d\udd39 Level Settings")]
	public bool XPOverflows;

	public int WeaponXpStep = 5;

	public int PlayerXpStep = 50;

	[SerializeField]
	private float fadeDuration = 0.5f;

	[SerializeField]
	private float showDuration = 3f;

	public event Action<string, int> OnWeaponUpgraded;

	public event Action<int> OnPlayerUpgraded;

	public void ShowWeaponUpgraded(string weaponName)
	{
		if (!(GameManager.Instance == null))
		{
			PickupsManager component = GameManager.Instance.GetComponent<PickupsManager>();
			GameManager.Instance.GetComponent<HitCursorsManager>().PlayUpGradedSound();
			int weaponGrade = GetWeaponGrade(weaponName);
			weaponUpgradedGradeTxt.text = weaponGrade.ToString();
			weaponUpgradedNameTxt.text = weaponName;
			weaponUpgradedGradeIcon.sprite = null;
			weaponUpgradedGradeIcon.sprite = component.GetWeaponIcon(weaponName);
			StartCoroutine(FadeSequence(weaponUpgraded));
			Debug.Log("ShowWeaponUpgraded");
		}
	}

	public void ShowPlayerUpgraded()
	{
		if (!(GameManager.Instance == null))
		{
			GameManager.Instance.GetComponent<HitCursorsManager>().PlayUpGradedSound();
			int playerGrade = GetPlayerGrade();
			playerUpgradedGradeTxt.text = playerGrade.ToString();
			profileGradeIcon.sprite = null;
			profileGradeIcon.sprite = GetRankIcon(GetPlayerGrade());
			StartCoroutine(FadeSequence(playerUpgraded));
			Debug.Log("ShowPlayerUpgraded");
		}
	}

	private IEnumerator FadeSequence(CanvasGroup group)
	{
		yield return StartCoroutine(FadeCanvasGroup(group, 0f, 1f, fadeDuration));
		yield return new WaitForSeconds(showDuration);
		yield return StartCoroutine(FadeCanvasGroup(group, 1f, 0f, fadeDuration));
	}

	private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
	{
		float t = 0f;
		group.alpha = start;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			group.alpha = Mathf.Lerp(start, end, t / duration);
			yield return null;
		}
		group.alpha = end;
	}

	public void SetWeaponGrade(string weaponName, int grade)
	{
		if (grade < 1)
		{
			grade = 1;
		}
		if (grade > MaxWeaponGrade)
		{
			grade = MaxWeaponGrade;
		}
		SteamCloudManager.SetInt(weaponName + "_Grade", grade);
		SteamCloudManager.Save();
		ShowWeaponUpgraded(weaponName);
	}

	public int GetWeaponGrade(string weaponName)
	{
		return SteamCloudManager.GetInt(weaponName + "_Grade", 1);
	}

	public int GetWeaponExp(string weaponName)
	{
		return SteamCloudManager.GetInt(weaponName + "_Exp");
	}

	public int GetWeaponExpToGrade(string weaponName)
	{
		int weaponGrade = GetWeaponGrade(weaponName);
		if (weaponGrade >= MaxWeaponGrade)
		{
			return 0;
		}
		return weaponGrade * WeaponXpStep;
	}

	public void AddWeaponExp(string weaponName, int amount = 1)
	{
		int value = GetWeaponExp(weaponName) + amount;
		SteamCloudManager.SetInt(weaponName + "_Exp", value);
		ComputeWeaponGrade(weaponName);
	}

	private void ComputeWeaponGrade(string weaponName)
	{
		int num = GetWeaponGrade(weaponName);
		int weaponExp = GetWeaponExp(weaponName);
		if (num < MaxWeaponGrade)
		{
			int num2 = num * WeaponXpStep;
			if (weaponExp >= num2)
			{
				weaponExp = (XPOverflows ? (weaponExp - num2) : 0);
				num++;
				SteamCloudManager.SetInt(weaponName + "_Exp", weaponExp);
				SteamCloudManager.SetInt(weaponName + "_Grade", num);
				SteamCloudManager.Save();
				Debug.Log($"[Weapon Upgrade] {weaponName} → Grade {num}");
				this.OnWeaponUpgraded?.Invoke(weaponName, num);
				ShowWeaponUpgraded(weaponName);
			}
			else
			{
				SteamCloudManager.SetInt(weaponName + "_Grade", num);
				SteamCloudManager.SetInt(weaponName + "_Exp", weaponExp);
				SteamCloudManager.Save();
			}
			Debug.Log($"[ComputeWeaponGrade] {weaponName} → Grade {num}");
		}
	}

	public Sprite GetRankIcon(int rank)
	{
		if (rankIcons == null || rankIcons.Length == 0)
		{
			return null;
		}
		rank = Mathf.Clamp(rank, 1, MaxPlayerGrade);
		int num = Mathf.FloorToInt((float)(rank - 1) / (float)(MaxPlayerGrade - 1) * (float)(rankIcons.Length - 1));
		return rankIcons[num];
	}

	public int GetPlayerExp()
	{
		return SteamCloudManager.GetInt("PlayerExp");
	}

	public int GetPlayerGrade()
	{
		return SteamCloudManager.GetInt("PlayerGrade", 1);
	}

	public void SetPlayerGrade(int grade)
	{
		if (grade < 1)
		{
			grade = 1;
		}
		if (grade > MaxPlayerGrade)
		{
			grade = MaxPlayerGrade;
		}
		SteamCloudManager.SetInt("PlayerGrade", grade);
		SteamCloudManager.Save();
		ShowPlayerUpgraded();
		UpdateProfile();
	}

	public int GetCurrentExpToGrade()
	{
		int playerGrade = GetPlayerGrade();
		if (playerGrade >= MaxPlayerGrade)
		{
			return 0;
		}
		return playerGrade * PlayerXpStep;
	}

	public void AddPlayerExp(int amount)
	{
		int value = GetPlayerExp() + amount;
		SteamCloudManager.SetInt("PlayerExp", value);
		ComputePlayerGrade();
	}

	private void ComputePlayerGrade()
	{
		int playerGrade = GetPlayerGrade();
		int playerExp = GetPlayerExp();
		if (playerGrade < MaxPlayerGrade)
		{
			int num = playerGrade * PlayerXpStep;
			if (playerExp >= num)
			{
				playerExp = (XPOverflows ? (playerExp - num) : 0);
				playerGrade++;
				SteamCloudManager.SetInt("PlayerExp", playerExp);
				SteamCloudManager.SetInt("PlayerGrade", playerGrade);
				SteamCloudManager.Save();
				Debug.Log($"[Player Upgrade] Player → Grade {playerGrade}");
				this.OnPlayerUpgraded?.Invoke(playerGrade);
				ShowPlayerUpgraded();
			}
			else
			{
				SteamCloudManager.SetInt("PlayerExp", playerExp);
				SteamCloudManager.SetInt("PlayerGrade", playerGrade);
				SteamCloudManager.Save();
			}
			UpdateProfile();
		}
	}

	public void UpdateProfile()
	{
		profileGradeTxt.text = GetPlayerGrade().ToString();
		profileCoinsTxt.text = GetCoins().ToString();
		profileExp.value = ((GetCurrentExpToGrade() > 0) ? ((float)GetPlayerExp() / (float)GetCurrentExpToGrade()) : 1f);
		profileGradeIcon.sprite = null;
		profileGradeIcon.sprite = GetRankIcon(GetPlayerGrade());
	}

	public int GetCoins()
	{
		return SteamCloudManager.GetInt("Stats_Coins");
	}

	public void AddCoins(int amount)
	{
		int num = GetCoins() + amount;
		if (num < 0)
		{
			num = 0;
		}
		if (num > 1000000)
		{
			num = 1000000;
		}
		SteamCloudManager.SetInt("Stats_Coins", Mathf.Max(0, num));
		SteamCloudManager.Save();
		UpdateProfile();
	}

	public void SetCoins(int value)
	{
		if (value < 0)
		{
			value = 0;
		}
		if (value > 1000000)
		{
			value = 1000000;
		}
		SteamCloudManager.SetInt("Stats_Coins", Mathf.Max(0, value));
		SteamCloudManager.Save();
		UpdateProfile();
	}

	public void SaveStats(int rank, int kills, int usedHealths, int finalScore, TimeSpan timePlayed, bool matchWinned)
	{
		int num = SteamCloudManager.GetInt("Stats_TotalMatches") + 1;
		int num2 = SteamCloudManager.GetInt("Stats_TotalWins");
		float value = SteamCloudManager.GetFloat("Stats_TotalPlayTime") + (float)timePlayed.TotalSeconds;
		if (matchWinned)
		{
			num2++;
		}
		SteamCloudManager.SetInt("Stats_MaxRank", Mathf.Min(SteamCloudManager.GetInt("Stats_MaxRank", GetComponent<MatchmakingManager>().MaxPlayers), rank));
		SteamCloudManager.SetInt("Stats_MaxKills", Mathf.Max(SteamCloudManager.GetInt("Stats_MaxKills"), kills));
		SteamCloudManager.SetInt("Stats_MaxUsedHealths", Mathf.Max(SteamCloudManager.GetInt("Stats_MaxUsedHealths"), usedHealths));
		SteamCloudManager.SetInt("Stats_MaxScore", Mathf.Max(SteamCloudManager.GetInt("Stats_MaxScore"), finalScore));
		SteamCloudManager.SetInt("Stats_TotalMatches", num);
		SteamCloudManager.SetInt("Stats_TotalWins", num2);
		SteamCloudManager.SetFloat("Stats_TotalPlayTime", value);
		SteamCloudManager.Save();
		Debug.Log($"[Stats Saved] Match {num}: Rank={rank}, Kills={kills}, Score={finalScore}, Time={timePlayed}, Win={matchWinned}");
	}

	public int GetTotalMatchesPlayed()
	{
		return SteamCloudManager.GetInt("Stats_TotalMatches");
	}

	public int GetTotalMatchesWinned()
	{
		return SteamCloudManager.GetInt("Stats_TotalWins");
	}

	public int GetMaxScore()
	{
		return SteamCloudManager.GetInt("Stats_MaxScore");
	}

	public int GetMaxKills()
	{
		return SteamCloudManager.GetInt("Stats_MaxKills");
	}

	public int GetMaxUsedHealths()
	{
		return SteamCloudManager.GetInt("Stats_MaxUsedHealths");
	}

	public int GetMaxRank()
	{
		return SteamCloudManager.GetInt("Stats_MaxRank");
	}

	public TimeSpan GetTotalPlayTime()
	{
		return TimeSpan.FromSeconds(SteamCloudManager.GetFloat("Stats_TotalPlayTime"));
	}

	public float GetTotalPlayTimeHours()
	{
		return (float)GetTotalPlayTime().TotalHours;
	}

	public float GetWinRate()
	{
		if (GetTotalMatchesPlayed() != 0)
		{
			return (float)GetTotalMatchesWinned() / (float)GetTotalMatchesPlayed();
		}
		return 0f;
	}

	public void UpdateStatsText()
	{
		statsTxt.text = GetFormattedStatsEx();
	}

	public string GetFormattedStatsEx()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<b><color=#FFD700>PLAYER STATS</color></b>");
		float progress = ((GetCurrentExpToGrade() > 0) ? ((float)GetPlayerExp() / (float)GetCurrentExpToGrade()) : 1f);
		stringBuilder.AppendLine($"<color=#00FFAA>Grade:</color> {GetPlayerGrade()}");
		stringBuilder.AppendLine($"<color=#00AAFF>EXP:</color> {GetPlayerExp()} / {GetCurrentExpToGrade()}");
		stringBuilder.AppendLine(GetProgressBar(progress, 20, "#00FFAA"));
		stringBuilder.AppendLine($"<color=#FFFF00>Coins:</color> {GetCoins()}");
		stringBuilder.AppendLine($"<color=#FFFFFF>Matches Played:</color> {GetTotalMatchesPlayed()}");
		stringBuilder.AppendLine($"<color=#FFFFFF>Matches Won:</color> {GetTotalMatchesWinned()} (<color=#00FF00>{GetWinRate() * 100f:0.0}%</color>)");
		stringBuilder.AppendLine($"<color=#AAAAFF>Best Rank:</color> {GetMaxRank()}");
		stringBuilder.AppendLine($"<color=#FF6666>Best Kills:</color> {GetMaxKills()}");
		stringBuilder.AppendLine($"<color=#FF9966>Max Used Healths:</color> {GetMaxUsedHealths()}");
		stringBuilder.AppendLine($"<color=#FFD700>Max Score:</color> {GetMaxScore()}");
		stringBuilder.AppendLine($"<color=#AAAAAA>Total Play Time:</color> {GetTotalPlayTime():hh\\:mm\\:ss}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("<b><color=#FFD700>WEAPONS</color></b>");
		bool flag = false;
		PickupsManager.Item[] items = GetComponent<PickupsManager>().items;
		foreach (PickupsManager.Item item in items)
		{
			if (item != null && item.type == PickupsManager.ItemType.weapon)
			{
				string short_description = item.short_description;
				int weaponGrade = GetWeaponGrade(short_description);
				int weaponExp = GetWeaponExp(short_description);
				int weaponExpToGrade = GetWeaponExpToGrade(short_description);
				float progress2 = ((weaponExpToGrade > 0) ? ((float)weaponExp / (float)weaponExpToGrade) : 1f);
				flag = true;
				stringBuilder.AppendLine($"<color=#00FFFF>{short_description}</color> → Grade <b>{weaponGrade}</b>");
				stringBuilder.AppendLine(GetProgressBar(progress2, 15, "#00FFFF"));
				stringBuilder.AppendLine($"EXP: {weaponExp}/{weaponExpToGrade}");
				stringBuilder.AppendLine();
			}
		}
		if (!flag)
		{
			stringBuilder.AppendLine("<color=#888888>No weapon data yet.</color>");
		}
		return stringBuilder.ToString();
	}

	private string GetProgressBar(float progress, int barLength = 10, string colorHex = "#FFFFFF")
	{
		progress = Mathf.Clamp01(progress);
		int num = Mathf.RoundToInt(progress * (float)barLength);
		int count = barLength - num;
		string arg = new string('█', num) + new string('░', count);
		return $"<color={colorHex}>{arg}</color> {progress * 100f:0}%";
	}

	[ContextMenu("Debug Print All Stats")]
	public void DebugPrintAll()
	{
		Debug.Log(GetFormattedStatsEx());
	}

	private string[] PlayerPrefsKeys()
	{
		return new string[0];
	}

	private void Start()
	{
		weaponUpgraded.gameObject.SetActive(value: true);
		playerUpgraded.gameObject.SetActive(value: true);
		weaponUpgraded.alpha = 0f;
		playerUpgraded.alpha = 0f;
		UpdateProfile();
	}

	private void Update()
	{
		if (GameManager.Instance.CheatCodes && Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.C))
		{
			SetPlayerGrade(GetPlayerGrade() + 1);
		}
		if (GameManager.Instance.CheatCodes && Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.C))
		{
			SetPlayerGrade(GetPlayerGrade() - 1);
		}
		if (GameManager.Instance.CheatCodes && Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.C))
		{
			SetCoins(GetCoins() - 100);
		}
		if (GameManager.Instance.CheatCodes && Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.C))
		{
			SetCoins(GetCoins() + 100);
		}
		if (GameManager.Instance.CheatCodes && Input.GetKeyDown(KeyCode.RightArrow) && Input.GetKey(KeyCode.K))
		{
			GameManager.Instance.GetComponent<HitCursorsManager>().PlayHitSound();
		}
	}
}
