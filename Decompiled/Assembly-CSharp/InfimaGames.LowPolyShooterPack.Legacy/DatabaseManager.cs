using System;
using System.Collections;
using System.IO;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class DatabaseManager : MonoBehaviour
{
	public string MainPHP_URL = "https://amonga99.com/php-scripts/php-mysql-query.php";

	public string SendMailPHP_URL = "https://amonga99.com/php-scripts/send-mail.php";

	public string Server = "localhost";

	public string Username = "amonga99_user";

	public string Password = "wpX678@lmfgXp3";

	public string Database = "amonga99_wp_database";

	public string SqlQuery = "SELECT * FROM wp_table_users";

	public string SqlFunction = "GetDataArrayJson";

	public bool TestConnectionOnAwake = true;

	public bool debug = true;

	public int timeout = 15;

	private int affected_rows;

	private string response = "";

	private JSONNode data;

	private bool isCallFinished = true;

	private bool isCallSuccessed;

	private string errorText;

	private bool jsonLineBreakSupport;

	private Action<bool> OnPHPRequest_callback;

	public static DatabaseManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			if (Application.isEditor && TestConnectionOnAwake)
			{
				SELECT();
			}
			debug = DataManager.Instance.DebugGameDataItems;
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public JSONNode GetData()
	{
		return data["data"];
	}

	public string GetErrorText()
	{
		return errorText;
	}

	public string GetResponseText()
	{
		return response;
	}

	public int GetNumAffectedRows()
	{
		return affected_rows;
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

	public void SELECT(string Table = "wp_table_users", string[] Fields = null, string[] Values = null, Action<bool> OnPHPRequest_callback = null)
	{
		string text = "SELECT * FROM " + Table;
		if (Fields != null && Values != null)
		{
			text += " WHERE ";
			for (int i = 0; i < Fields.Length; i++)
			{
				if (i > 0)
				{
					text += " AND ";
				}
				text = text + Fields[i] + " = '" + Values[i] + "'";
			}
		}
		CallPHPWhenFinished(text, "GetDataArrayJson", OnPHPRequest_callback);
	}

	public void INSERT(string Table, string[] Fields = null, string[] Values = null, Action<bool> OnPHPRequest_callback = null)
	{
		string text = "INSERT INTO " + Table;
		string text2 = "(";
		string text3 = "(";
		for (int i = 0; i < Fields.Length; i++)
		{
			if (i > 0)
			{
				text2 += " , ";
				text3 += " , ";
			}
			text2 += Fields[i];
			text3 = text3 + "'" + Values[i] + "'";
		}
		text2 += ")";
		text3 += ")";
		text = text + text2 + " VALUES" + text3;
		CallPHPWhenFinished(text, "RunSQL", OnPHPRequest_callback);
	}

	public void UPDATE(string Table, string[] Fields = null, string[] Values = null, string WhereField = "", string WhereValue = "", Action<bool> OnPHPRequest_callback = null)
	{
		string text = "UPDATE " + Table + " SET ";
		for (int i = 0; i < Fields.Length; i++)
		{
			if (i > 0)
			{
				text += " , ";
			}
			text = text + Fields[i] + " = '" + Values[i] + "'";
		}
		text = text + " WHERE " + WhereField + " = '" + WhereValue + "'";
		CallPHPWhenFinished(text, "RunSQL", OnPHPRequest_callback);
	}

	public void DELETE(string Table, string WhereField = "", string WhereValue = "", Action<bool> OnPHPRequest_callback = null)
	{
		string text = "DELETE FROM " + Table;
		text = text + " WHERE " + WhereField + " = '" + WhereValue + "'";
		CallPHPWhenFinished(text, "RunSQL", OnPHPRequest_callback);
	}

	public void CallPHPWhenFinished(string sqlQuery, string sqlFunction = "GetDataArrayJson", Action<bool> OnPHPRequest_callback = null, bool jsonLineBreakSupport = false)
	{
		StartCoroutine(RunPHPWhenFinished(sqlQuery, sqlFunction, OnPHPRequest_callback, jsonLineBreakSupport));
	}

	private IEnumerator RunPHPWhenFinished(string sqlQuery, string sqlFunction, Action<bool> OnPHPRequest_callback, bool jsonLineBreakSupport = false)
	{
		while (!CallPHP(sqlQuery, sqlFunction, OnPHPRequest_callback, jsonLineBreakSupport))
		{
			yield return null;
		}
	}

	private bool CallPHP(string sqlQuery = "SELECT * FROM User", string sqlFunction = "GetDataArrayJson", Action<bool> OnPHPRequest_callback = null, bool jsonLineBreakSupport = false)
	{
		if (!isCallFinished)
		{
			return false;
		}
		isCallFinished = false;
		SqlQuery = sqlQuery;
		SqlFunction = sqlFunction;
		this.OnPHPRequest_callback = OnPHPRequest_callback;
		this.jsonLineBreakSupport = jsonLineBreakSupport;
		if ((bool)data)
		{
			data = null;
		}
		response = "";
		errorText = "";
		StartCoroutine(RunPHP());
		return true;
	}

	private IEnumerator RunPHP()
	{
		if (debug)
		{
			Debug.Log("Calling PHP query{" + SqlQuery + "}  function{" + SqlFunction + "}");
		}
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("server_mainname", Server);
		wWWForm.AddField("server_username", Username);
		wWWForm.AddField("server_password", Password);
		wWWForm.AddField("server_database", Database);
		wWWForm.AddField("query", SqlQuery, Encoding.UTF8);
		wWWForm.AddField("function", SqlFunction);
		UnityWebRequest www = UnityWebRequest.Post(MainPHP_URL, wWWForm);
		www.timeout = timeout;
		yield return www.SendWebRequest();
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
			OnPHPRequest_callback?.Invoke(obj: false);
			yield break;
		}
		isCallSuccessed = true;
		affected_rows = 0;
		if (SqlFunction == "GetDataArrayJson")
		{
			if (jsonLineBreakSupport)
			{
				data = JSON.ParseEx(response);
			}
			else
			{
				data = JSON.Parse(response);
			}
			if (data != null)
			{
				affected_rows = StringToInt(data["num_affected_rows"]);
				if (debug)
				{
					Debug.Log("json:" + data.ToString());
				}
			}
		}
		else
		{
			if ((bool)data)
			{
				data = null;
			}
			affected_rows = StringToInt(response);
		}
		OnPHPRequest_callback?.Invoke(affected_rows > 0);
	}

	public bool UploadPHP(string filePath = "", string info = "", Action<bool> OnPHPRequest_callback = null, string sqlQuery = "INSERT INTO File (id,file) VALUES('',$file_content)", string sqlFunction = "UploadFile")
	{
		if (!isCallFinished)
		{
			return false;
		}
		SqlQuery = sqlQuery;
		SqlFunction = sqlFunction;
		this.OnPHPRequest_callback = OnPHPRequest_callback;
		if ((bool)data)
		{
			data = null;
		}
		response = "";
		errorText = "";
		StartCoroutine(UploadFilePHP(filePath, info));
		return true;
	}

	public static string ByteArrayToHexString(byte[] bytes)
	{
		return "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLower();
	}

	public IEnumerator UploadFilePHP(string filePath = "", string info = "")
	{
		isCallFinished = false;
		WWWForm wWWForm = new WWWForm();
		if (File.Exists(filePath))
		{
			FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader binaryReader = new BinaryReader(fileStream);
			string text = ByteArrayToHexString(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));
			if (debug)
			{
				Debug.Log("file content " + text);
			}
			wWWForm.AddField("file_content", text);
			fileStream.Close();
		}
		else
		{
			Debug.LogError("file does not exist " + filePath);
		}
		wWWForm.AddField("file_info", "");
		wWWForm.AddField("file_name", "");
		wWWForm.AddField("server_mainname", Server);
		wWWForm.AddField("server_username", Username);
		wWWForm.AddField("server_password", Password);
		wWWForm.AddField("server_database", Database);
		wWWForm.AddField("query", SqlQuery, Encoding.UTF8);
		wWWForm.AddField("function", SqlFunction);
		if (debug)
		{
			Debug.Log("Calling PHP query{" + SqlQuery + "}  function{" + SqlFunction + "}");
		}
		UnityWebRequest www = UnityWebRequest.Post(MainPHP_URL, wWWForm);
		yield return www.SendWebRequest();
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
			Debug.LogError("error:" + errorText);
			OnPHPRequest_callback?.Invoke(obj: false);
			yield break;
		}
		isCallSuccessed = true;
		affected_rows = 0;
		if ((bool)data)
		{
			data = null;
		}
		affected_rows = StringToInt(response);
		OnPHPRequest_callback?.Invoke(affected_rows > 0);
	}

	public void SendMail(string mail_to, string mail_from, string mail_subject, string mail_message)
	{
		StartCoroutine(SendMailPHP(mail_to, mail_from, mail_subject, mail_message));
	}

	private IEnumerator SendMailPHP(string mail_to, string mail_from, string mail_subject, string mail_message)
	{
		if (debug)
		{
			Debug.Log("sending email from:" + mail_from + " to:" + mail_to + " subject:" + mail_subject + " msg:" + mail_message);
		}
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("to", mail_to);
		wWWForm.AddField("from", mail_from);
		wWWForm.AddField("subject", mail_subject);
		wWWForm.AddField("message", mail_message);
		UnityWebRequest www = UnityWebRequest.Post(SendMailPHP_URL, wWWForm);
		www.timeout = timeout;
		yield return www.SendWebRequest();
		response = www.downloadHandler.text;
		if (debug)
		{
			Debug.Log("SendMailPHP Response:" + response);
		}
		if (www.isNetworkError || www.isHttpError)
		{
			errorText = www.error;
			Debug.LogError("SendMailPHP error:" + errorText);
		}
		else
		{
			Debug.Log("SendMailPHP email sent");
		}
	}
}
