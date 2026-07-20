using System;
using System.Collections.Generic;
using UnityEngine;

namespace HardShellStudios.CompleteControl;

public class hManager
{
	private static hManager main;

	private hInputDetails[] inputs;

	private bool resetInEditor;

	public KeyCode rebindRemoveKey;

	private float[,] inputAxis;

	private int lastFrame;

	private float lastTime;

	private float difference;

	private float timeDifference
	{
		get
		{
			if (lastFrame != Time.frameCount)
			{
				difference = Time.time - lastTime;
				lastTime = Time.time;
				lastFrame = Time.frameCount;
				return difference;
			}
			return difference;
		}
		set
		{
			lastTime = value;
		}
	}

	public static hManager Active()
	{
		if (main == null)
		{
			main = new hManager();
		}
		return main;
	}

	public hManager()
	{
		LoadDefaultScheme();
		if (!resetInEditor || !Application.isEditor)
		{
			LoadPersonal();
		}
	}

	public void LoadDefaultScheme()
	{
		hScheme defaultScheme = hUtility.GetDefaultScheme();
		resetInEditor = defaultScheme.forceResetInEditor;
		rebindRemoveKey = defaultScheme.rebindRemoveKey;
		inputs = new hInputDetails[defaultScheme.inputs.Length];
		for (int i = 0; i < defaultScheme.inputs.Length; i++)
		{
			inputs[i] = defaultScheme.inputs[i];
		}
		inputAxis = new float[inputs.Length, 3];
	}

	public void LoadPersonal()
	{
		hInputDetails[] array = hUtility.LoadBindings(ref inputs);
		if (array != null)
		{
			inputs = array;
			if (hUtility.LoadedSchemeName != hUtility.DefaultSchemeName)
			{
				Debug.LogWarning("Input loaded scheme name " + hUtility.LoadedSchemeName + " does not match default scheme name " + hUtility.DefaultSchemeName + ", inputs will be reset and saved");
				ResetAllKeys();
			}
		}
	}

	public bool GetButton(string keyName)
	{
		for (int i = 0; i < inputs.Length; i++)
		{
			if (!(inputs[i].Name == keyName))
			{
				continue;
			}
			if (inputs[i].Type == KeyType.KeyPress)
			{
				if (Input.GetKey(inputs[i].Positive.Primary) || Input.GetKey(inputs[i].Positive.Secondary) || Input.GetKey(inputs[i].Negative.Primary) || Input.GetKey(inputs[i].Negative.Secondary))
				{
					return true;
				}
			}
			else if (inputs[i].Type == KeyType.ControllerAxis)
			{
				float axis = GetAxis(keyName, useInvert: false);
				if ((axis > 0.5f && !inputs[i].Invert) || (axis < -0.5f && inputs[i].Invert))
				{
					return true;
				}
				return false;
			}
		}
		return false;
	}

	public bool GetButtonDown(string keyName)
	{
		for (int i = 0; i < inputs.Length; i++)
		{
			if (!(inputs[i].Name == keyName))
			{
				continue;
			}
			if (inputs[i].Type == KeyType.KeyPress)
			{
				if (Input.GetKeyDown(inputs[i].Positive.Primary) || Input.GetKeyDown(inputs[i].Positive.Secondary) || Input.GetKeyDown(inputs[i].Negative.Primary) || Input.GetKeyDown(inputs[i].Negative.Secondary))
				{
					return true;
				}
			}
			else if (inputs[i].Type == KeyType.ControllerAxis)
			{
				float axis = GetAxis(keyName, useInvert: false);
				if ((axis > 0.5f && !inputs[i].Invert) || (axis < -0.5f && inputs[i].Invert))
				{
					return true;
				}
				return false;
			}
		}
		return false;
	}

	public bool GetButtonUp(string keyName)
	{
		for (int i = 0; i < inputs.Length; i++)
		{
			if (inputs[i].Name == keyName && inputs[i].Type == KeyType.KeyPress && (Input.GetKeyUp(inputs[i].Positive.Primary) || Input.GetKeyUp(inputs[i].Positive.Secondary) || Input.GetKeyUp(inputs[i].Negative.Primary) || Input.GetKeyUp(inputs[i].Negative.Secondary)))
			{
				return true;
			}
		}
		return false;
	}

	private float CompareAxis(float first, float second)
	{
		if (first > 0f)
		{
			if (second > first)
			{
				return second;
			}
			return first;
		}
		if (first < 0f)
		{
			if (second < first)
			{
				return second;
			}
			return first;
		}
		return second;
	}

	public float GetAxis(string keyName, bool useInvert = true)
	{
		bool flag = Input.GetJoystickNames() != null && Input.GetJoystickNames().Length != 0;
		float num = 0f;
		for (int i = 0; i < inputs.Length; i++)
		{
			if (!(inputs[i].Name == keyName))
			{
				continue;
			}
			if (inputs[i].Type == KeyType.MouseAxis)
			{
				num = CompareAxis(num, Input.GetAxis($"Mouse Axis-{inputs[i].Axis.ToString()}"));
				num *= inputs[i].Sensitivity;
			}
			else if (inputs[i].Type == KeyType.ControllerAxis)
			{
				if (flag)
				{
					num = CompareAxis(num, Input.GetAxis($"Controller Axis-{inputs[i].targetController.ToString()}-{inputs[i].Axis.ToString()}"));
					num = Mathf.Clamp(num * inputs[i].Sensitivity, -1f, 1f);
				}
			}
			else if (inputs[i].Type == KeyType.KeyPress)
			{
				num = CompareAxis(num, GetAxisFromKey(inputs[i], i));
			}
			if (useInvert && inputs[i].Invert && num != 0f)
			{
				num *= -1f;
			}
		}
		return num;
	}

	private float GetAxisFromKey(hInputDetails details, int i)
	{
		if ((float)Time.frameCount > inputAxis[i, 0])
		{
			inputAxis[i, 1] = inputAxis[i, 2];
			float num = 0f;
			if (Input.GetKey(inputs[i].Positive.Primary) || Input.GetKey(inputs[i].Positive.Secondary))
			{
				num += 1f;
			}
			if (Input.GetKey(inputs[i].Negative.Primary) || Input.GetKey(inputs[i].Negative.Secondary))
			{
				num -= 1f;
			}
			float num2 = 1f;
			if (num == 0f || Mathf.Sign(inputAxis[i, 2]) != Mathf.Sign(num))
			{
				num2 = 3f;
			}
			inputAxis[i, 2] = Mathf.Clamp(Mathf.MoveTowards(inputAxis[i, 2], num, inputs[i].Sensitivity * timeDifference * num2), -1f, 1f);
			inputAxis[i, 0] = lastFrame;
			return inputAxis[i, 2];
		}
		return inputAxis[i, 2];
	}

	private int GetUniqueIndex(string uniqueKeyName)
	{
		for (int i = 0; i < inputs.Length; i++)
		{
			if (inputs[i].UniqueName == uniqueKeyName)
			{
				return i;
			}
		}
		return -1;
	}

	public void SetKey(string uniqueKeyName, KeyCode keyCode, KeyTarget keyTarget = KeyTarget.PositivePrimary)
	{
		int uniqueIndex = GetUniqueIndex(uniqueKeyName);
		if (uniqueIndex != -1)
		{
			inputs[uniqueIndex].Type = KeyType.KeyPress;
			switch (keyTarget)
			{
			case KeyTarget.PositivePrimary:
				inputs[uniqueIndex].Positive.Primary = keyCode;
				break;
			case KeyTarget.PositiveSecondary:
				inputs[uniqueIndex].Positive.Secondary = keyCode;
				break;
			case KeyTarget.NegativePrimary:
				inputs[uniqueIndex].Negative.Primary = keyCode;
				break;
			case KeyTarget.NegativeSecondary:
				inputs[uniqueIndex].Negative.Secondary = keyCode;
				break;
			}
			hUtility.SaveBinings(inputs);
		}
	}

	public void SetKey(string uniqueKeyName, MouseAxis mouseAxis)
	{
		int uniqueIndex = GetUniqueIndex(uniqueKeyName);
		if (uniqueIndex != -1)
		{
			inputs[uniqueIndex].Type = KeyType.MouseAxis;
			inputs[uniqueIndex].Axis = (AxisCode)mouseAxis;
			hUtility.SaveBinings(inputs);
		}
	}

	public void SetKey(string uniqueKeyName, AxisCode joystickAxis, TargetController targetController = TargetController.All, bool inverse = false)
	{
		int uniqueIndex = GetUniqueIndex(uniqueKeyName);
		if (uniqueIndex == -1)
		{
			uniqueIndex = GetUniqueIndex(uniqueKeyName.Replace("_axis", "_keys"));
		}
		if (uniqueIndex != -1)
		{
			inputs[uniqueIndex].Type = KeyType.ControllerAxis;
			inputs[uniqueIndex].Axis = joystickAxis;
			inputs[uniqueIndex].targetController = targetController;
			inputs[uniqueIndex].Invert = inverse;
			hUtility.SaveBinings(inputs);
		}
	}

	public void SetKeySensitivity(string uniqueKeyName, float Sensitivity)
	{
		int uniqueIndex = GetUniqueIndex(uniqueKeyName);
		if (uniqueIndex != -1)
		{
			inputs[uniqueIndex].Sensitivity = Sensitivity;
			hUtility.SaveBinings(inputs);
		}
	}

	public void ResetKey(string uniqueKeyName)
	{
		hScheme defaultScheme = hUtility.GetDefaultScheme();
		int uniqueIndex = GetUniqueIndex(uniqueKeyName);
		inputs[uniqueIndex] = defaultScheme.inputs[uniqueIndex];
		hUtility.SaveBinings(inputs);
	}

	public void ResetAllKeys()
	{
		LoadDefaultScheme();
		hUtility.SaveBinings(inputs);
	}

	public KeyCode CurrentKeyDown()
	{
		if (Input.GetJoystickNames().Length > 1)
		{
			foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
			{
				if (Input.GetKeyDown(value) && !value.ToString().Contains("JoystickButton"))
				{
					return value;
				}
			}
		}
		foreach (KeyCode value2 in Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKeyDown(value2))
			{
				return value2;
			}
		}
		return KeyCode.None;
	}

	public float CurrentAxis(out AxisCode _axis, out TargetController _controller, List<string> axisCodesFill = null, List<string> axisCodesIgnore = null)
	{
		_axis = AxisCode.Axis1;
		_controller = TargetController.All;
		axisCodesFill?.Clear();
		int num = 0;
		Array values = Enum.GetValues(typeof(AxisCode));
		Array.Reverse(values);
		if (Input.GetJoystickNames().Length > 1)
		{
			foreach (AxisCode item in values)
			{
				num = 0;
				foreach (TargetController value in Enum.GetValues(typeof(TargetController)))
				{
					if (value == TargetController.All)
					{
						continue;
					}
					string text = $"Controller Axis-{value.ToString()}-{item.ToString()}";
					float axis = Input.GetAxis(text);
					if (axis > 0.5f || axis < -0.5f)
					{
						if (axisCodesIgnore != null && axisCodesIgnore.Contains(text))
						{
							continue;
						}
						_axis = item;
						_controller = value;
						if (axisCodesFill == null)
						{
							return axis;
						}
						axisCodesFill.Add(text);
					}
					num++;
				}
			}
		}
		foreach (AxisCode item2 in values)
		{
			num = 0;
			foreach (TargetController value2 in Enum.GetValues(typeof(TargetController)))
			{
				string text2 = $"Controller Axis-{value2.ToString()}-{item2.ToString()}";
				float axis2 = Input.GetAxis(text2);
				if (axis2 > 0.5f || axis2 < -0.5f)
				{
					if (axisCodesIgnore != null && axisCodesIgnore.Contains(text2))
					{
						continue;
					}
					_axis = item2;
					_controller = value2;
					if (axisCodesFill == null)
					{
						return axis2;
					}
					axisCodesFill.Add(text2);
				}
				num++;
			}
		}
		return 0f;
	}

	public KeyCode DetailsFromKey(string uniqueKeyCode, KeyTarget keyTarget)
	{
		int uniqueIndex = GetUniqueIndex(uniqueKeyCode);
		if (uniqueIndex == -1)
		{
			return KeyCode.None;
		}
		return keyTarget switch
		{
			KeyTarget.PositiveSecondary => inputs[uniqueIndex].Positive.Secondary, 
			KeyTarget.NegativePrimary => inputs[uniqueIndex].Negative.Primary, 
			KeyTarget.NegativeSecondary => inputs[uniqueIndex].Negative.Secondary, 
			_ => inputs[uniqueIndex].Positive.Primary, 
		};
	}

	public string DetailsFromAxis(string uniqueKeyCode, KeyTarget keyTarget)
	{
		int uniqueIndex = GetUniqueIndex(uniqueKeyCode);
		if (uniqueIndex == -1)
		{
			uniqueIndex = GetUniqueIndex(uniqueKeyCode.Replace("_axis", "_keys"));
		}
		if (uniqueIndex == -1)
		{
			return "";
		}
		if (Input.GetJoystickNames().Length == 0)
		{
			return "";
		}
		string arg = inputs[uniqueIndex].targetController.ToString().Replace("All", "").Replace("Joystick", "");
		string arg2 = inputs[uniqueIndex].Axis.ToString().Replace("Axis", "");
		string arg3 = "(+)";
		if (keyTarget.ToString().Contains("Negative"))
		{
			arg3 = "(-)";
		}
		return $"Joy{arg}Axis{arg2}{arg3}";
	}

	public void DebugInputs()
	{
		for (int i = 0; i < inputs.Length; i++)
		{
			Debug.Log(inputs[i].ToStringEx());
		}
	}
}
