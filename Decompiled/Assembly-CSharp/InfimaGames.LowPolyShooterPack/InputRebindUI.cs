using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class InputRebindUI : MonoBehaviour
{
	[Header("Binding setup")]
	[Tooltip("Exact action name from your Input Action Asset (e.g. 'Jump' or 'Move')")]
	[SerializeField]
	private string actionName;

	[Tooltip("Leave empty for simple actions. For composites use: Up/Down/Left/Right or positive/negative")]
	[SerializeField]
	private string compositePartName = "";

	[Header("UI")]
	[SerializeField]
	private Text bindingLabel;

	[SerializeField]
	private Button rebindButton;

	[SerializeField]
	private Button resetButton;

	private InputAction action;

	private int bindingIndex = -1;

	private void Start()
	{
		Setup();
	}

	private void OnEnable()
	{
		if (SharedInputManager.Instance != null)
		{
			SharedInputManager.Instance.OnBindingsChanged += OnGlobalBindingsChanged;
		}
	}

	private void OnDisable()
	{
		if (SharedInputManager.Instance != null)
		{
			SharedInputManager.Instance.OnBindingsChanged -= OnGlobalBindingsChanged;
		}
	}

	private void Setup()
	{
		if (SharedInputManager.Instance == null)
		{
			Debug.LogError("[InputRebindUI] SharedInputManager instance not found in scene.");
			return;
		}
		InputActionAsset actions = SharedInputManager.Instance.GetActions();
		if (actions == null)
		{
			Debug.LogError("[InputRebindUI] SharedInputManager has no InputActionAsset assigned.");
			return;
		}
		action = actions.FindAction(actionName, throwIfNotFound: true);
		if (action == null)
		{
			Debug.LogError("[InputRebindUI] Action '" + actionName + "' not found in InputActionAsset.");
			return;
		}
		bindingIndex = FindBindingIndex(action, compositePartName);
		if (rebindButton != null)
		{
			rebindButton.onClick.AddListener(StartRebind);
		}
		if (resetButton != null)
		{
			resetButton.onClick.AddListener(ResetBinding);
		}
		UpdateLabel();
	}

	private void OnGlobalBindingsChanged()
	{
		if (action != null)
		{
			bindingIndex = FindBindingIndex(action, compositePartName);
			UpdateLabel();
		}
	}

	private static int FindBindingIndex(InputAction action, string partName)
	{
		if (action == null)
		{
			return -1;
		}
		if (string.IsNullOrEmpty(partName))
		{
			for (int i = 0; i < action.bindings.Count; i++)
			{
				InputBinding inputBinding = action.bindings[i];
				if (!inputBinding.isComposite && !inputBinding.isPartOfComposite)
				{
					return i;
				}
			}
			return -1;
		}
		for (int j = 0; j < action.bindings.Count; j++)
		{
			InputBinding inputBinding2 = action.bindings[j];
			if (inputBinding2.isPartOfComposite && string.Equals(inputBinding2.name, partName, StringComparison.OrdinalIgnoreCase))
			{
				return j;
			}
		}
		return -1;
	}

	private void UpdateLabel()
	{
		if (!(bindingLabel == null))
		{
			if (action == null)
			{
				bindingLabel.text = "--";
			}
			else if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
			{
				string effectivePath = action.bindings[bindingIndex].effectivePath;
				bindingLabel.text = InputControlPath.ToHumanReadableString(effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
			}
			else if (action.bindings.Count > 0)
			{
				bindingLabel.text = action.GetBindingDisplayString(0);
			}
			else
			{
				bindingLabel.text = "Unbound";
			}
		}
	}

	public void StartRebind()
	{
		if (action == null)
		{
			Debug.LogWarning("[InputRebindUI] Action not configured.");
			return;
		}
		if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
		{
			Debug.LogWarning("[InputRebindUI] Binding index invalid for action: " + actionName);
			return;
		}
		bindingLabel.text = "Press a key...";
		action.Disable();
		action.PerformInteractiveRebinding(bindingIndex).WithControlsExcluding("Mouse/position").WithControlsExcluding("Mouse/delta")
			.OnMatchWaitForAnother(0.1f)
			.OnComplete(delegate(InputActionRebindingExtensions.RebindingOperation operation)
			{
				operation.Dispose();
				action.Enable();
				SharedInputManager.Instance.SaveRebinds();
				bindingIndex = FindBindingIndex(action, compositePartName);
				UpdateLabel();
			})
			.Start();
	}

	public void ResetBinding()
	{
		if (action == null)
		{
			Debug.LogWarning("[InputRebindUI] Action not configured.");
			return;
		}
		if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
		{
			action.RemoveBindingOverride(bindingIndex);
		}
		else
		{
			action.RemoveAllBindingOverrides();
		}
		SharedInputManager.Instance.SaveRebinds();
		bindingIndex = FindBindingIndex(action, compositePartName);
		UpdateLabel();
	}
}
