using System;
using System.Text;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Encryptor : MonoBehaviour
{
	public static bool encrypt = true;

	public static string secret = "abcd1234";

	public static string added_string = "million_dollars_2022";

	private void Awake()
	{
	}

	public static string Encrypt(string toEncrypt)
	{
		toEncrypt += added_string;
		Encoding.UTF8.GetBytes(secret);
		byte[] bytes = Encoding.UTF8.GetBytes(toEncrypt);
		byte[] array = null;
		array = bytes;
		return Convert.ToBase64String(array, 0, array.Length);
	}

	public static string Decrypt(string toDecrypt)
	{
		Encoding.UTF8.GetBytes(secret);
		byte[] array = Convert.FromBase64String(toDecrypt);
		byte[] array2 = null;
		array2 = array;
		return Encoding.UTF8.GetString(array2, 0, array2.Length).Replace(added_string, string.Empty);
	}
}
