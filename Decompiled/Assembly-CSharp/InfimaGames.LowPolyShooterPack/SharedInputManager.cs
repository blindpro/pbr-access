using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class SharedInputManager : MonoBehaviour
{
	[Tooltip("Assign your InputActionAsset (the .inputactions) here")]
	[SerializeField]
	private InputActionAsset inputActions;

	private const string RebindsKey = "rebinds";

	public static SharedInputManager Instance
	{
		get
		{
			if (GameManager.Instance == null)
			{
				return null;
			}
			return GameManager.Instance.GetComponent<SharedInputManager>();
		}
	}

	public event Action OnBindingsChanged;

	private void Awake()
	{
		LoadRebinds();
	}

	public InputActionAsset GetActions()
	{
		return inputActions;
	}

	public void SaveRebinds()
	{
		if (!(inputActions == null))
		{
			string value = inputActions.SaveBindingOverridesAsJson();
			PlayerPrefs.SetString("rebinds", value);
			PlayerPrefs.Save();
			this.OnBindingsChanged?.Invoke();
		}
	}

	public void LoadRebinds()
	{
		if (!(inputActions == null))
		{
			if (PlayerPrefs.HasKey("rebinds"))
			{
				string json = PlayerPrefs.GetString("rebinds");
				inputActions.LoadBindingOverridesFromJson(json);
			}
			this.OnBindingsChanged?.Invoke();
		}
	}

	public void ApplyTo(PlayerInput playerInput)
	{
		if (!(playerInput == null) && PlayerPrefs.HasKey("rebinds"))
		{
			string json = PlayerPrefs.GetString("rebinds");
			playerInput.actions.LoadBindingOverridesFromJson(json);
		}
	}

	public void ResetRebinds()
	{
		if (!(inputActions == null))
		{
			inputActions.RemoveAllBindingOverrides();
			SaveRebinds();
			GetComponent<SettingsManager>().ResetInputs();
		}
	}
}
