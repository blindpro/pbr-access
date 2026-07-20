using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public static class Log
{
	public static void wtf()
	{
		Internal_Log("Wtf", LogType.Log);
	}

	public static void wtf(object toPrint)
	{
		if (toPrint == null)
		{
			toPrint = "Null";
		}
		Internal_Log("Wtf happened: " + toPrint, LogType.Log);
	}

	public static void warn_me()
	{
		Internal_Log("You have been warned", LogType.Warning);
	}

	public static void warn_me(object warning)
	{
		if (warning == null)
		{
			warning = "Null";
		}
		_ = "You have been warned that: " + warning;
		Internal_Log("You have been warned that: " + warning, LogType.Warning);
	}

	public static void kill()
	{
		Internal_Log("I will find you, and I will kill you", LogType.Error);
	}

	public static void kill(object toKill)
	{
		if (toKill == null)
		{
			toKill = "Null";
		}
		Internal_Log("You have been warned that: " + toKill, LogType.Error);
	}

	public static void oopsie(Exception oopsie, UnityEngine.Object context = null)
	{
		Debug.LogException(oopsie, context);
	}

	public static void ReferenceError(MonoBehaviour behaviour, GameObject gameObject)
	{
		kill("Component " + behaviour.GetType().Name + " on GameObject " + gameObject.name + " has missing references, and will not correctly function. Please fix this so the component can work properly!");
	}

	private static void Internal_Log(string message, LogType type)
	{
		if (message == " ")
		{
			message = "Null";
		}
		switch (type)
		{
		case LogType.Log:
			Debug.Log(message);
			break;
		case LogType.Warning:
			Debug.LogWarning(message);
			break;
		case LogType.Error:
			Debug.LogError(message);
			break;
		default:
			throw new ArgumentOutOfRangeException(type.GetType().FullName, type, null);
		case LogType.Assert:
		case LogType.Exception:
			break;
		}
	}
}
