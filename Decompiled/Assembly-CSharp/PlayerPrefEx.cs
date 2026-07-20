using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class PlayerPrefEx
{
	[Serializable]
	private class Wrapper
	{
		public Dictionary<string, object> dict = new Dictionary<string, object>();
	}

	private static bool UseEncryption = true;

	private static string EncryptionKey = "YourStrongKeyHere123";

	private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "playerprefex.dat");

	private static Dictionary<string, object> data;

	private static bool loaded = false;

	private static void Load()
	{
		if (loaded)
		{
			return;
		}
		if (File.Exists(SavePath))
		{
			try
			{
				string text = File.ReadAllText(SavePath);
				if (UseEncryption)
				{
					text = DecryptAES(text, EncryptionKey);
				}
				data = JsonUtility.FromJson<Wrapper>(text)?.dict;
			}
			catch
			{
				data = new Dictionary<string, object>();
			}
		}
		if (data == null)
		{
			data = new Dictionary<string, object>();
		}
		loaded = true;
	}

	public static void Save()
	{
		Load();
		string text = JsonUtility.ToJson(new Wrapper
		{
			dict = data
		}, prettyPrint: true);
		if (UseEncryption)
		{
			text = EncryptAES(text, EncryptionKey);
		}
		File.WriteAllText(SavePath, text);
	}

	public static void SetInt(string key, int value)
	{
		Load();
		data[key] = value;
	}

	public static int GetInt(string key, int defaultValue = 0)
	{
		Load();
		if (!data.ContainsKey(key))
		{
			return defaultValue;
		}
		try
		{
			return Convert.ToInt32(data[key]);
		}
		catch
		{
			return defaultValue;
		}
	}

	public static void SetFloat(string key, float value)
	{
		Load();
		data[key] = value;
	}

	public static float GetFloat(string key, float defaultValue = 0f)
	{
		Load();
		if (!data.ContainsKey(key))
		{
			return defaultValue;
		}
		try
		{
			return Convert.ToSingle(data[key]);
		}
		catch
		{
			return defaultValue;
		}
	}

	public static void SetString(string key, string value)
	{
		Load();
		data[key] = value;
	}

	public static string GetString(string key, string defaultValue = "")
	{
		Load();
		if (!data.ContainsKey(key))
		{
			return defaultValue;
		}
		try
		{
			return data[key].ToString();
		}
		catch
		{
			return defaultValue;
		}
	}

	private static string EncryptAES(string plain, string key)
	{
		byte[] iV = new byte[16];
		byte[] inArray;
		using (Aes aes = Aes.Create())
		{
			aes.Key = Encoding.UTF8.GetBytes(GetKey32(key));
			aes.IV = iV;
			ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
			using MemoryStream memoryStream = new MemoryStream();
			using CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			using StreamWriter streamWriter = new StreamWriter(cryptoStream);
			streamWriter.Write(plain);
			streamWriter.Flush();
			cryptoStream.FlushFinalBlock();
			inArray = memoryStream.ToArray();
		}
		return Convert.ToBase64String(inArray);
	}

	private static string DecryptAES(string encrypted, string key)
	{
		try
		{
			byte[] iV = new byte[16];
			byte[] buffer = Convert.FromBase64String(encrypted);
			using Aes aes = Aes.Create();
			aes.Key = Encoding.UTF8.GetBytes(GetKey32(key));
			aes.IV = iV;
			ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
			using MemoryStream stream = new MemoryStream(buffer);
			using CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
			using StreamReader streamReader = new StreamReader(stream2);
			return streamReader.ReadToEnd();
		}
		catch
		{
			return "{}";
		}
	}

	private static string GetKey32(string key)
	{
		if (key.Length >= 32)
		{
			return key.Substring(0, 32);
		}
		return key.PadRight(32, '0');
	}
}
