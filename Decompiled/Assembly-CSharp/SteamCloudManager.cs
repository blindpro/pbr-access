using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;

public static class SteamCloudManager
{
	[Serializable]
	private class Wrapper
	{
		public List<Entry> items = new List<Entry>();

		public Wrapper(Dictionary<string, object> source)
		{
			foreach (KeyValuePair<string, object> item in source)
			{
				string v = UnityWebRequest.EscapeURL(item.Value.ToString());
				items.Add(new Entry(item.Key, v));
			}
		}
	}

	[Serializable]
	private class Entry
	{
		public string key;

		public string value;

		public Entry(string k, string v)
		{
			key = k;
			value = v;
		}
	}

	public static bool USE_PLAYER_PREFS = true;

	public static string FILE_NAME = "playerdata.json";

	private static Dictionary<string, object> data = new Dictionary<string, object>();

	private static bool loaded = false;

	public static bool FORCE_DELETE_CLOUD = false;

	public static event Action OnSteamDataLoadingFinished;

	public static bool Load()
	{
		if (!SteamClient.IsValid)
		{
			Debug.LogWarning("[SteamCloud] Steam not initialized. Using PlayerPrefs only.");
			loaded = true;
			return false;
		}
		if (FORCE_DELETE_CLOUD)
		{
			Debug.LogWarning("[SteamCloud] FORCE_DELETE_CLOUD = TRUE → deleting cloud file!");
			if (SteamRemoteStorage.FileExists(FILE_NAME))
			{
				SteamRemoteStorage.FileDelete(FILE_NAME);
			}
			data.Clear();
			FORCE_DELETE_CLOUD = false;
			loaded = true;
			SteamCloudManager.OnSteamDataLoadingFinished?.Invoke();
			return true;
		}
		try
		{
			if (SteamRemoteStorage.FileExists(FILE_NAME))
			{
				byte[] array = SteamRemoteStorage.FileRead(FILE_NAME);
				if (array != null && array.Length != 0)
				{
					string text = Encoding.UTF8.GetString(array);
					ParseJson(text);
					Debug.Log("[SteamCloud] Cloud data loaded. json:" + text);
					loaded = true;
					SteamCloudManager.OnSteamDataLoadingFinished?.Invoke();
					return true;
				}
				Debug.Log("[SteamCloud] Cloud file empty.");
			}
			else
			{
				Debug.Log("[SteamCloud] No cloud file found.");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[SteamCloud] Load error: " + ex);
		}
		loaded = true;
		return false;
	}

	public static void Save(bool saveToSteam = true)
	{
		if (USE_PLAYER_PREFS)
		{
			PlayerPrefs.Save();
		}
		if (!loaded)
		{
			Debug.LogWarning("[SteamCloud] Save blocked — Steam not loaded yet.");
			return;
		}
		if (!SteamClient.IsValid || !saveToSteam)
		{
			Debug.LogWarning("[SteamCloud] Steam not initialized — saved locally only.");
			return;
		}
		string text = JsonUtility.ToJson(new Wrapper(data), prettyPrint: true);
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		try
		{
			SteamRemoteStorage.FileWrite(FILE_NAME, bytes);
			Debug.Log("[SteamCloud] Saved to Steam Cloud (" + bytes.Length + " bytes) json:" + text);
		}
		catch (Exception ex)
		{
			Debug.LogError("[SteamCloud] Cloud save FAILED. Saving locally only. Error: " + ex);
		}
	}

	private static void ParseJson(string json)
	{
		Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
		if (wrapper == null || wrapper.items == null)
		{
			Debug.LogWarning("ParseJson wrapper null or empty");
			return;
		}
		foreach (Entry item in wrapper.items)
		{
			string value = UnityWebRequest.UnEscapeURL(item.value);
			data[item.key] = value;
			Debug.Log(item.key + " = " + data[item.key]);
		}
	}

	public static void SetInt(string key, int value)
	{
		if (!string.IsNullOrEmpty(key))
		{
			data[key] = value.ToString(CultureInfo.InvariantCulture);
			if (USE_PLAYER_PREFS)
			{
				PlayerPrefs.SetInt(key, value);
			}
		}
	}

	public static void SetFloat(string key, float value)
	{
		if (!string.IsNullOrEmpty(key))
		{
			data[key] = value.ToString(CultureInfo.InvariantCulture);
			if (USE_PLAYER_PREFS)
			{
				PlayerPrefs.SetFloat(key, value);
			}
		}
	}

	public static void SetString(string key, string value)
	{
		if (!string.IsNullOrEmpty(key))
		{
			data[key] = value;
			if (USE_PLAYER_PREFS)
			{
				PlayerPrefs.SetString(key, value);
			}
		}
	}

	public static int GetInt(string key, int defaultValue = 0)
	{
		if (data.ContainsKey(key) && int.TryParse(data[key].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		if (USE_PLAYER_PREFS && PlayerPrefs.HasKey(key))
		{
			int result2 = PlayerPrefs.GetInt(key, defaultValue);
			data[key] = result2.ToString(CultureInfo.InvariantCulture);
			return result2;
		}
		return defaultValue;
	}

	public static float GetFloat(string key, float defaultValue = 0f)
	{
		if (data.ContainsKey(key) && float.TryParse(data[key].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		if (USE_PLAYER_PREFS && PlayerPrefs.HasKey(key))
		{
			float result2 = PlayerPrefs.GetFloat(key, defaultValue);
			data[key] = result2.ToString(CultureInfo.InvariantCulture);
			return result2;
		}
		return defaultValue;
	}

	public static string GetString(string key, string defaultValue = "")
	{
		if (data.ContainsKey(key))
		{
			return data[key].ToString();
		}
		if (USE_PLAYER_PREFS && PlayerPrefs.HasKey(key))
		{
			string text = PlayerPrefs.GetString(key, defaultValue);
			data[key] = text;
			return text;
		}
		return defaultValue;
	}
}
