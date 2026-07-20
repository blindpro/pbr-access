using System.Collections.Generic;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class SettingsManager : MonoBehaviour
{
	public GameObject graphicsPanel;

	public Dropdown graphicsDrop;

	public Dropdown resolutionDrop;

	public Slider SensibbilitySlider;

	public Slider AudioSlider;

	public Slider MusicSlider;

	public Text SensibbilityTxt;

	public Text AudioTxt;

	public Text MusicTxt;

	public float sennsibilityDefault = 4f;

	public float audioVolumeDefault = 0.5f;

	public float musicVolumeDefault = 0.2f;

	public Toggle inverseMouseX;

	public Toggle inverseMouseY;

	public Dropdown languageDropdown;

	public Dropdown botsDifficultyDropdown;

	public int defaultBotsDifficulty = 2;

	private void Start()
	{
		FillResolutionList();
		FillGraphicsList();
		FillLanguage();
		SensibbilitySlider.value = PlayerPrefs.GetFloat("Sensibility", sennsibilityDefault);
		SensibbilityTxt.text = SensibbilitySlider.value.ToString("F2");
		AudioSlider.value = PlayerPrefs.GetFloat("AudioVolume", audioVolumeDefault);
		AudioTxt.text = AudioSlider.value.ToString("F2");
		MusicSlider.value = PlayerPrefs.GetFloat("MusicVolume", musicVolumeDefault);
		MusicTxt.text = MusicSlider.value.ToString("F2");
		OnAudioVolumeChange(AudioSlider.value);
		OnMusicVolumeChange(MusicSlider.value);
		inverseMouseX.isOn = PlayerPrefs.GetInt("InverseMouseX", 0) == 1;
		inverseMouseY.isOn = PlayerPrefs.GetInt("InverseMouseY", 0) == 1;
		botsDifficultyDropdown.value = PlayerPrefs.GetInt("BotsDifficulty", defaultBotsDifficulty);
	}

	private void Update()
	{
		if (GameManager.Instance.CheatCodes && graphicsPanel.activeInHierarchy && Input.GetKey(KeyCode.O) && Input.GetKeyDown(KeyCode.P))
		{
			Screen.fullScreen = false;
		}
	}

	public void FillResolutionList()
	{
		if (Application.isMobilePlatform || Screen.resolutions == null || Screen.resolutions.Length == 0)
		{
			return;
		}
		resolutionDrop.ClearOptions();
		string currentResolutionMode = DataManager.Instance.GetString("TM_resolutionMode").Replace(" ", "");
		Debug.Log("currentResolutionMode:" + currentResolutionMode);
		int num = -1;
		for (int i = 0; i < Screen.resolutions.Length; i++)
		{
			string text = Screen.resolutions[i].ToString().Replace(" ", "");
			resolutionDrop.options.Add(new Dropdown.OptionData(text));
			if (text == currentResolutionMode)
			{
				num = i;
			}
		}
		if (num >= 0 && num < Screen.resolutions.Length)
		{
			Resolution resolution = Screen.resolutions[num];
			Screen.SetResolution(resolution.width, resolution.height, fullscreen: true, resolution.refreshRate);
		}
		else
		{
			currentResolutionMode = Screen.currentResolution.ToString().Replace(" ", "");
			DataManager.Instance.SetString("TM_resolutionMode", currentResolutionMode, save: true);
		}
		resolutionDrop.value = -1;
		resolutionDrop.value = resolutionDrop.options.FindIndex((Dropdown.OptionData option) => option.text == currentResolutionMode);
	}

	public void OnResolutionChanged(int value)
	{
		if (!Application.isMobilePlatform && graphicsPanel.activeInHierarchy && Screen.resolutions != null && Screen.resolutions.Length != 0 && value >= 0 && value < Screen.resolutions.Length)
		{
			Resolution resolution = Screen.resolutions[value];
			Screen.SetResolution(resolution.width, resolution.height, fullscreen: true, resolution.refreshRate);
			string text = resolution.ToString().Replace(" ", "");
			DataManager.Instance.SetString("TM_resolutionMode", text, save: true);
			Debug.Log("OnResolutionChanged:" + text);
		}
	}

	public void FillGraphicsList()
	{
		graphicsDrop.ClearOptions();
		string currentGraphicMode = DataManager.Instance.GetString("TM_graphicsMode");
		Debug.Log("currentGraphicMode:" + currentGraphicMode);
		int num = -1;
		for (int i = 0; i < QualitySettings.names.Length; i++)
		{
			string text = QualitySettings.names[i];
			graphicsDrop.options.Add(new Dropdown.OptionData(text));
			if (text == currentGraphicMode)
			{
				num = i;
			}
		}
		if (num > -1)
		{
			QualitySettings.SetQualityLevel(num, applyExpensiveChanges: true);
		}
		else
		{
			num = QualitySettings.GetQualityLevel();
			currentGraphicMode = QualitySettings.names[num];
			DataManager.Instance.SetString("TM_graphicsMode", currentGraphicMode, save: true);
		}
		graphicsDrop.value = -1;
		graphicsDrop.value = graphicsDrop.options.FindIndex((Dropdown.OptionData option) => option.text == currentGraphicMode);
	}

	public void FillLanguage()
	{
		languageDropdown.ClearOptions();
		List<string> allLanguages = LocalizationManager.GetAllLanguages();
		languageDropdown.AddOptions(allLanguages);
		string text = PlayerPrefs.GetString("SelectedLanguage", "");
		if (!string.IsNullOrEmpty(text) && allLanguages.Contains(text))
		{
			LocalizationManager.CurrentLanguage = text;
			ForceLocalizeAll();
			languageDropdown.value = allLanguages.IndexOf(text);
		}
		else
		{
			string currentLanguage = LocalizationManager.CurrentLanguage;
			ForceLocalizeAll();
			languageDropdown.value = allLanguages.IndexOf(currentLanguage);
		}
		languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
	}

	private void OnLanguageChanged(int index)
	{
		string text = (LocalizationManager.CurrentLanguage = languageDropdown.options[index].text);
		ForceLocalizeAll();
		PlayerPrefs.SetString("SelectedLanguage", text);
		PlayerPrefs.Save();
		Debug.Log("Language switched to: " + text);
	}

	public void ForceLocalizeAll()
	{
		Localize[] array = Object.FindObjectsOfType<Localize>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnLocalize(Force: true);
		}
	}

	public void OnBotsDifficultyChanged(int index)
	{
		PlayerPrefs.SetInt("BotsDifficulty", index);
		PlayerPrefs.Save();
		Debug.Log("OnBotsDifficultyChanged switched to: " + index);
	}

	public void OnGraphicsChanged(int value)
	{
		if (graphicsPanel.activeInHierarchy)
		{
			QualitySettings.SetQualityLevel(value, applyExpensiveChanges: true);
			string text = QualitySettings.names[value];
			DataManager.Instance.SetString("TM_graphicsMode", text);
			Debug.Log("OnGraphicsChanged:" + text);
		}
	}

	public void OnSensibilityChange(float v)
	{
		PlayerPrefs.SetFloat("Sensibility", v);
		PlayerPrefs.Save();
		Debug.Log("OnSensibilityChange:" + v);
		SensibbilityTxt.text = v.ToString("F2");
	}

	public void OnAudioVolumeChange(float v)
	{
		AudioListener.volume = v;
		PlayerPrefs.SetFloat("AudioVolume", v);
		PlayerPrefs.Save();
		Debug.Log("OnAudioVolumeChange:" + v);
		AudioTxt.text = v.ToString("F2");
	}

	public void OnMusicVolumeChange(float v)
	{
		GetComponent<MusicPlayer>().globalMusicVolume = v;
		PlayerPrefs.SetFloat("MusicVolume", v);
		PlayerPrefs.Save();
		Debug.Log("OnMusicVolumeChange:" + v);
		MusicTxt.text = v.ToString("F2");
	}

	public void OnInverseMouseXChange(bool b)
	{
		PlayerPrefs.SetInt("InverseMouseX", b ? 1 : 0);
		PlayerPrefs.Save();
		Debug.Log("OnInverseMouseXChange:" + b);
	}

	public void OnInverseMouseYChange(bool b)
	{
		PlayerPrefs.SetInt("InverseMouseY", b ? 1 : 0);
		PlayerPrefs.Save();
		Debug.Log("OnInverseMouseYChange:" + b);
	}

	public void ResetInputs()
	{
		SensibbilitySlider.value = sennsibilityDefault;
		inverseMouseX.isOn = false;
		inverseMouseY.isOn = false;
	}

	public void ResetSound()
	{
		AudioSlider.value = audioVolumeDefault;
		MusicSlider.value = musicVolumeDefault;
	}
}
