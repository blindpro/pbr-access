using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class WPDatabaseManager : MonoBehaviour
{
	public Transform LoadingPanel;

	public Text Log;

	public string RegisterPHP_URL = "https://amonga99.com/php-scripts/wp-register.php";

	public string GetUserPHP_URL = "https://amonga99.com/php-scripts/wp-get-user.php";

	public string GetUserDataPHP_URL = "https://amonga99.com/php-scripts/wp-get-user-data.php";

	public string SetUserDataPHP_URL = "https://amonga99.com/php-scripts/wp-set-user-data.php";

	public int user_id = 6;

	public string user_login = "ninja-x";

	public string user_pass = "opengl";

	public string user_email = "";

	public string user_data_key = "POLYGON_BIT_BR_DATA";

	public string user_data_value = "";

	public string first_name = "";

	public string last_name = "";

	public int timeout = 15;

	public bool TestConnectionOnAwake = true;

	public bool debug = true;

	private string response = "";

	private JSONNode data;

	private bool isCallFinished = true;

	private bool isCallSuccessed;

	private string errorText;

	private string url;

	private Action<bool> OnPHPRequest_callback;

	public static WPDatabaseManager Instance;

	public const string MatchEmailPattern = "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$";

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			if (Application.isEditor && TestConnectionOnAwake)
			{
				CallPHPWhenFinished(GetUserPHP_URL);
			}
			debug = DataManager.Instance.DebugGameDataItems;
			if ((bool)LoadingPanel)
			{
				LoadingPanel.gameObject.SetActive(value: false);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public JSONNode GetData()
	{
		return data["USER_DATA"];
	}

	public string GetErrorText()
	{
		return errorText;
	}

	public string GetResponseText()
	{
		return response;
	}

	public bool IsCallFinished()
	{
		return isCallFinished;
	}

	public bool IsCallSuccessed()
	{
		return isCallSuccessed;
	}

	private int StringToInt(string s)
	{
		if (int.TryParse(s, out var result))
		{
			return result;
		}
		return 0;
	}

	public void CallPHPWhenFinished(string url, Action<bool> OnPHPRequest_callback = null, string log = "")
	{
		StartCoroutine(RunPHPWhenFinished(url, OnPHPRequest_callback, log));
	}

	private IEnumerator RunPHPWhenFinished(string url, Action<bool> OnPHPRequest_callback, string log)
	{
		while (!CallPHP(url, OnPHPRequest_callback, log))
		{
			yield return null;
		}
	}

	private bool CallPHP(string url, Action<bool> OnPHPRequest_callback = null, string log = "")
	{
		if (!isCallFinished)
		{
			return false;
		}
		isCallFinished = false;
		this.url = url;
		this.OnPHPRequest_callback = OnPHPRequest_callback;
		if ((bool)data)
		{
			data = null;
		}
		response = "";
		errorText = "";
		StartCoroutine(RunPHP(url, log));
		return true;
	}

	private IEnumerator RunPHP(string url, string log)
	{
		if (debug)
		{
			Debug.Log("Running PHP url:" + url);
			Debug.Log(log);
		}
		user_data_value = DataManager.Instance.GetData();
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("user_id", user_id);
		wWWForm.AddField("user_login", user_login);
		wWWForm.AddField("user_pass", user_pass);
		wWWForm.AddField("user_email", user_email);
		wWWForm.AddField("first_name", first_name);
		wWWForm.AddField("last_name", last_name);
		wWWForm.AddField("user_data_key", user_data_key);
		wWWForm.AddField("user_data_value", user_data_value, Encoding.UTF8);
		UnityWebRequest www = UnityWebRequest.Post(url, wWWForm);
		www.timeout = timeout;
		if ((bool)LoadingPanel)
		{
			LoadingPanel.gameObject.SetActive(value: true);
		}
		if ((bool)Log)
		{
			Log.text = log;
		}
		yield return www.SendWebRequest();
		if ((bool)LoadingPanel)
		{
			LoadingPanel.gameObject.SetActive(value: false);
		}
		if ((bool)Log)
		{
			Log.text = "";
		}
		isCallFinished = true;
		response = www.downloadHandler.text;
		if (debug)
		{
			Debug.Log("Response:" + response);
		}
		if (www.isNetworkError || www.isHttpError)
		{
			isCallSuccessed = false;
			errorText = www.error;
			Debug.LogWarning("error:" + errorText);
			Debug.LogWarning("user_id:" + user_id + " user_login: " + user_login + " user_pass: " + user_pass + " user_email: " + user_email + " user_data_key: " + user_data_key + " user_data_value: " + user_data_value);
			OnPHPRequest_callback?.Invoke(obj: false);
			yield break;
		}
		isCallSuccessed = true;
		if (response.Contains("ERROR:"))
		{
			isCallSuccessed = false;
			errorText = response.Replace("ERROR:", "");
			Debug.LogWarning("error:" + errorText);
			Debug.LogWarning("user_id:" + user_id + " user_login: " + user_login + " user_pass: " + user_pass + " user_email: " + user_email + " user_data_key: " + user_data_key + " user_data_value: " + user_data_value);
			OnPHPRequest_callback?.Invoke(obj: false);
			yield break;
		}
		data = JSON.Parse(response);
		if (data != null && !data.IsNull)
		{
			user_id = StringToInt(data["USER_ID"]);
			DataManager.Instance.SetData(GetData());
			OnPHPRequest_callback?.Invoke(obj: true);
			if (debug)
			{
				Debug.Log("user_id:" + user_id);
				Debug.Log("user_data:" + GetData().ToString());
				Debug.Log("full json:" + data.ToString());
			}
		}
		else
		{
			OnPHPRequest_callback?.Invoke(obj: false);
		}
	}

	public static bool IsEmail(string email)
	{
		if (email != null)
		{
			return Regex.IsMatch(email, "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$");
		}
		return false;
	}
}
