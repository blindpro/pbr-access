using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HP.Generics;

public class TimeTransition : MonoBehaviour
{
	[Serializable]
	public class Params
	{
		[Header("Sun Transition")]
		public float newValue = 10f;

		[Header("Sun Emission Color")]
		public Color sunEmissionColor;

		public float sunEmissionIntensity;

		[Header("Skybox")]
		public float sunSize;

		public float sunSizeConvergence;

		public float atmosphereThickness;

		public float exposure;

		[Header("Fog")]
		public float density = 0.001f;

		public Color fogColor = Color.black;

		[Header("Bloom")]
		public float bloomThreshold;

		public float bloomIntensity;

		public float bloomScatter;

		[Header("Color Adjustement")]
		public int colorAdjustContrast;

		public int colorAdjustSaturatin;

		[Header("Color Lookup")]
		public float colorLookupContribution;
	}

	public GameObject sun;

	private Light sunLight;

	public bool isTransitionAllowed = true;

	public int selectedPreset;

	public List<Params> presets = new List<Params>();

	public float transitionDuration = 2f;

	private Material runtimeSkybox;

	public KeyCode key = KeyCode.U;

	public string sceneName;

	public float firstPartDuration = 10f;

	public float timer = 10f;

	private float currentTimer;

	public float howManyDivision = 5f;

	private float currentDivision;

	private float ratio;

	private bool autoMode;

	public float bypassAutoModeDuration = 15f;

	private void Start()
	{
	}

	private void Init()
	{
	}

	private IEnumerator DayTimeRoutinePart1()
	{
		yield return null;
	}

	private IEnumerator DayTimeRoutinePart2()
	{
		while (currentTimer < timer)
		{
			if (!PauseManager.instance.Bool_IsGamePaused)
			{
				currentTimer += Time.deltaTime;
			}
			yield return null;
		}
		currentDivision += 1f;
		currentDivision %= howManyDivision + 1f;
		ratio = currentDivision / howManyDivision;
		if (currentDivision == 0f)
		{
			currentDivision = 0f;
			selectedPreset++;
			selectedPreset %= presets.Count;
			StartCoroutine(DayTimeRoutinePart1());
		}
		else
		{
			currentTimer = 0f;
			UpdateTransition();
			StartCoroutine(DayTimeRoutinePart2());
		}
		yield return null;
	}

	private void Update()
	{
	}

	private IEnumerator EnableAutoModeRoutine()
	{
		float t = 0f;
		while (t < bypassAutoModeDuration)
		{
			if (!PauseManager.instance.Bool_IsGamePaused)
			{
				t += Time.deltaTime;
			}
			yield return null;
		}
		autoMode = true;
		StartCoroutine(DayTimeRoutinePart1());
		yield return null;
	}

	private void UpdateTransition()
	{
		SunTransition();
		SkyboxMaterialTransition();
		FogModeTransition();
		PostFxTransition();
	}

	private void SunTransition()
	{
		StopCoroutine(SunTransitionRoutine());
		StartCoroutine(SunTransitionRoutine());
	}

	private IEnumerator SunTransitionRoutine()
	{
		isTransitionAllowed = false;
		float t = 0f;
		float duration = transitionDuration;
		float currentSunRotationX = sun.transform.localEulerAngles.x;
		Color currentSunColor = sunLight.color;
		float currentSunIntensity = sunLight.intensity;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float b = Mathf.Lerp(presets[selectedPreset].newValue, presets[(selectedPreset + 1) % presets.Count].newValue, ratio);
			sun.transform.localEulerAngles = new Vector3(Mathf.Lerp(currentSunRotationX, b, t), sun.transform.localEulerAngles.y, sun.transform.localEulerAngles.z);
			Color b2 = Color.Lerp(presets[selectedPreset].sunEmissionColor, presets[(selectedPreset + 1) % presets.Count].sunEmissionColor, ratio);
			sunLight.color = Color.Lerp(currentSunColor, b2, t);
			float b3 = Mathf.Lerp(presets[selectedPreset].sunEmissionIntensity, presets[(selectedPreset + 1) % presets.Count].sunEmissionIntensity, ratio);
			sunLight.intensity = Mathf.Lerp(currentSunIntensity, b3, t);
			yield return null;
		}
		isTransitionAllowed = true;
		yield return null;
	}

	private void SkyboxMaterialTransition()
	{
		StopCoroutine(SkyboxMaterialTransitionRoutine());
		StartCoroutine(SkyboxMaterialTransitionRoutine());
	}

	private void CloneSkybox()
	{
		StopCoroutine(CloneSkyboxRoutine());
		StartCoroutine(CloneSkyboxRoutine());
	}

	private IEnumerator CloneSkyboxRoutine()
	{
		yield return new WaitUntil(() => SceneManager.GetActiveScene() == SceneManager.GetSceneByName(sceneName));
		Material skybox = RenderSettings.skybox;
		runtimeSkybox = UnityEngine.Object.Instantiate(skybox);
		RenderSettings.skybox = runtimeSkybox;
		yield return null;
	}

	private void InitSun()
	{
		if ((bool)sun)
		{
			sunLight = sun.GetComponent<Light>();
		}
	}

	private IEnumerator SkyboxMaterialTransitionRoutine()
	{
		float t = 0f;
		float duration = transitionDuration;
		float sunSize = runtimeSkybox.GetFloat("_SunSize");
		float exposure = runtimeSkybox.GetFloat("_Exposure");
		float sunSizeConvergence = runtimeSkybox.GetFloat("_SunSizeConvergence");
		float atmosphereThickness = runtimeSkybox.GetFloat("_AtmosphereThickness");
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float b = Mathf.Lerp(presets[selectedPreset].sunSize, presets[(selectedPreset + 1) % presets.Count].sunSize, ratio);
			float b2 = Mathf.Lerp(presets[selectedPreset].exposure, presets[(selectedPreset + 1) % presets.Count].exposure, ratio);
			runtimeSkybox.SetFloat("_SunSize", Mathf.Lerp(sunSize, b, t));
			runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(exposure, b2, t));
			float b3 = Mathf.Lerp(presets[selectedPreset].sunSizeConvergence, presets[(selectedPreset + 1) % presets.Count].sunSizeConvergence, ratio);
			float b4 = Mathf.Lerp(presets[selectedPreset].atmosphereThickness, presets[(selectedPreset + 1) % presets.Count].atmosphereThickness, ratio);
			runtimeSkybox.SetFloat("_SunSizeConvergence", Mathf.Lerp(sunSizeConvergence, b3, t));
			runtimeSkybox.SetFloat("_AtmosphereThickness", Mathf.Lerp(atmosphereThickness, b4, t));
			yield return null;
		}
		yield return null;
	}

	private void FogModeTransition()
	{
		StopCoroutine(FogModeTransitionRoutine());
		StartCoroutine(FogModeTransitionRoutine());
	}

	private IEnumerator FogModeTransitionRoutine()
	{
		float t = 0f;
		float duration = transitionDuration;
		float currentFogDensity = RenderSettings.fogDensity;
		Color currentFogColor = RenderSettings.fogColor;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float b = Mathf.Lerp(presets[selectedPreset].density, presets[(selectedPreset + 1) % presets.Count].density, ratio);
			Color b2 = Color.Lerp(presets[selectedPreset].fogColor, presets[(selectedPreset + 1) % presets.Count].fogColor, ratio);
			RenderSettings.fogDensity = Mathf.Lerp(currentFogDensity, b, t);
			RenderSettings.fogColor = Color.Lerp(currentFogColor, b2, t);
			yield return null;
		}
		yield return null;
	}

	private void InitPostEffect()
	{
	}

	private void PostFxTransition()
	{
		StopCoroutine(PostFxTransitionRoutine());
		StartCoroutine(PostFxTransitionRoutine());
	}

	private IEnumerator PostFxTransitionRoutine()
	{
		yield return null;
	}
}
