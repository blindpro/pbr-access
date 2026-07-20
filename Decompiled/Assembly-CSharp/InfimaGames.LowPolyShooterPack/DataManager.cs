using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class DataManager : MonoBehaviour
{
	public bool editor_ResetData;

	public Dictionary<string, string> GameData = new Dictionary<string, string>();

	public static DataManager Instance;

	public static int LoggedUserId = -1;

	public bool DebugGameDataItems;

	public static bool IsLoggedIn()
	{
		return LoggedUserId > 0;
	}

	public void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void Start()
	{
	}

	public void SetString(string key, string value, bool save = false, string log = "Saving data online...")
	{
		SteamCloudManager.SetString(key, value);
		if (save)
		{
			Save(log);
		}
	}

	public void SetInt(string key, int value, bool save = false, string log = "Saving data online...")
	{
		SteamCloudManager.SetInt(key, value);
		if (save)
		{
			Save(log);
		}
	}

	public void SetFloat(string key, float value, bool save = false, string log = "Saving data online...")
	{
		SteamCloudManager.SetFloat(key, value);
		if (save)
		{
			Save(log);
		}
	}

	public string GetString(string key, string value = "")
	{
		return SteamCloudManager.GetString(key, value);
	}

	public int GetInt(string key, int value = 0)
	{
		return SteamCloudManager.GetInt(key, value);
	}

	public float GetFloat(string key, float value = 0f)
	{
		return SteamCloudManager.GetFloat(key, value);
	}

	public void Save(string log = "Saving data online...", bool save_local = true, bool save_online = true)
	{
		SteamCloudManager.Save();
	}

	public string GetData()
	{
		string text = "";
		foreach (KeyValuePair<string, string> gameDatum in GameData)
		{
			if (gameDatum.Value != "" && gameDatum.Key != "")
			{
				text = text + "\"" + gameDatum.Key + "\":\"" + gameDatum.Value + "\",";
			}
		}
		text = "{" + text + "}";
		return text.Replace(",}", "}");
	}

	public void SetData(JSONNode data)
	{
		if ((Application.isEditor && editor_ResetData) || data == null || data.IsNull)
		{
			return;
		}
		foreach (KeyValuePair<string, JSONNode> item in (JSONObject)data)
		{
			string value = item.Value;
			SetString(item.Key, value);
		}
		SendMessage("UpdateFromGameData", SendMessageOptions.DontRequireReceiver);
	}

	private void OnSaveCallBack(bool success)
	{
		if (success)
		{
			Debug.Log("Data saved online.");
		}
		else
		{
			Debug.LogWarning(WPDatabaseManager.Instance.GetErrorText());
		}
	}
}
