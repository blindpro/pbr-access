using System.Collections.Generic;
using HardShellStudios.CompleteControl;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Hard Shell Studios/Complete Control/UI Rebind Button")]
[RequireComponent(typeof(Button))]
public class UI_RebindKey : MonoBehaviour
{
	public string uniqueName;

	public KeyTarget keyTarget;

	public Text text;

	public bool constantUpdate;

	private string originalString;

	private bool isBinding;

	private Button button;

	private bool textSettled;

	private bool enableDetection;

	private List<string> axisCodesToIgnore = new List<string>();

	private void Start()
	{
		enableDetection = false;
		originalString = text.text;
		button = GetComponent<Button>();
		button.onClick.AddListener(RebindKey);
	}

	private void Update()
	{
		if (isBinding)
		{
			if (!enableDetection)
			{
				return;
			}
			KeyCode keyCode = hInput.CurrentKeyDown();
			if (keyCode != KeyCode.None)
			{
				hInput.SetKey(uniqueName + "_keys", keyCode, keyTarget);
				isBinding = false;
				SetInteractable();
				return;
			}
			AxisCode _axis;
			TargetController _controller;
			float num = hInput.CurrentAxis(out _axis, out _controller, null, axisCodesToIgnore);
			if (num != 0f)
			{
				bool inverse = false;
				if (num > 0f && keyTarget.ToString().Contains("Negative"))
				{
					inverse = true;
				}
				if (num < 0f && keyTarget.ToString().Contains("Positive"))
				{
					inverse = true;
				}
				hInput.SetKey(uniqueName + "_axis", _axis, _controller, inverse);
				isBinding = false;
				SetInteractable();
			}
		}
		else if (!textSettled || constantUpdate)
		{
			if (originalString.Contains("{key}") || originalString.Contains("{name}"))
			{
				text.text = originalString.Replace("'{key}'", hInput.DetailsFromKey(uniqueName + "_keys", keyTarget).ToString()).Replace("'{axis}'", hInput.DetailsFromAxis(uniqueName + "_axis", keyTarget));
			}
			else
			{
				text.text = originalString;
			}
		}
	}

	public void RebindKey()
	{
		text.text = "PRESS ANY KEY";
		textSettled = false;
		isBinding = true;
		button.interactable = false;
		enableDetection = false;
		CancelInvoke("EnableKeyDetection");
		Invoke("EnableKeyDetection", 0.2f);
		hInput.CurrentAxis(out var _, out var _, axisCodesToIgnore);
	}

	private void EnableKeyDetection()
	{
		enableDetection = true;
	}

	private void SetInteractable()
	{
		button.interactable = false;
		CancelInvoke("EnableButton");
		Invoke("EnableButton", 0.3f);
	}

	private void EnableButton()
	{
		button.interactable = true;
	}
}
