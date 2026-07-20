using System.Collections.Generic;
using HardShellStudios.CompleteControl;
using UnityEngine;

public static class hInput
{
	public static KeyCode RebindRemovalKey => hManager.Active().rebindRemoveKey;

	public static bool GetButton(string buttonName)
	{
		return hManager.Active().GetButton(buttonName);
	}

	public static bool GetButtonDown(string buttonName)
	{
		return hManager.Active().GetButtonDown(buttonName);
	}

	public static bool GetButtonUp(string buttonName)
	{
		return hManager.Active().GetButtonUp(buttonName);
	}

	public static float GetAxis(string buttonName)
	{
		return hManager.Active().GetAxis(buttonName);
	}

	public static void SetKey(string uniqueKeyName, KeyCode keyCode, KeyTarget keyTarget = KeyTarget.PositivePrimary)
	{
		hManager.Active().SetKey(uniqueKeyName, keyCode, keyTarget);
	}

	public static void SetKey(string uniqueKeyName, MouseAxis mouseAxis)
	{
		hManager.Active().SetKey(uniqueKeyName, mouseAxis);
	}

	public static void SetKey(string uniqueKeyName, AxisCode joystickAxis, TargetController targetController = TargetController.All, bool inverse = false)
	{
		hManager.Active().SetKey(uniqueKeyName, joystickAxis, targetController, inverse);
	}

	public static void SetKeySensitivity(string uniqueKeyName, float sensitivity)
	{
		hManager.Active().SetKeySensitivity(uniqueKeyName, sensitivity);
	}

	public static KeyCode CurrentKeyDown()
	{
		return hManager.Active().CurrentKeyDown();
	}

	public static float CurrentAxis(out AxisCode _axis, out TargetController _controller, List<string> axisCodesFill = null, List<string> axisCodesIgnore = null)
	{
		return hManager.Active().CurrentAxis(out _axis, out _controller, axisCodesFill, axisCodesIgnore);
	}

	public static KeyCode DetailsFromKey(string uniqueKeyName, KeyTarget keyTarget)
	{
		return hManager.Active().DetailsFromKey(uniqueKeyName, keyTarget);
	}

	public static string DetailsFromAxis(string uniqueKeyName, KeyTarget keyTarget)
	{
		return hManager.Active().DetailsFromAxis(uniqueKeyName, keyTarget);
	}

	public static void ResetAll()
	{
		hManager.Active().ResetAllKeys();
	}

	public static void DebugInputs()
	{
		hManager.Active().DebugInputs();
	}
}
