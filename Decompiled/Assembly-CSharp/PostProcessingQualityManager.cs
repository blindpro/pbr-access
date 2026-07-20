using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PostProcessingQualityManager : MonoBehaviour
{
	[Serializable]
	public class PostProcessingQuality
	{
		public string qualityName = "";

		public bool postProcessingEnabled = true;

		public void Apply(PostProcessVolume volume)
		{
			if (!volume)
			{
				Debug.LogError("Post-process volume is null");
				return;
			}
			volume.enabled = postProcessingEnabled;
			Debug.Log("Post-processing " + (postProcessingEnabled ? "enabled" : "disabled") + " for quality: " + qualityName);
		}

		public void Get(PostProcessVolume volume)
		{
			if (!volume)
			{
				Debug.LogError("Post-process volume is null");
			}
			else
			{
				postProcessingEnabled = volume.enabled;
			}
		}
	}

	public PostProcessingQuality[] qualities;

	private PostProcessingQuality original = new PostProcessingQuality();

	private PostProcessVolume volume;

	private string currentRenderQuality = "";

	private void OnEnable()
	{
		volume = GetComponent<PostProcessVolume>();
		if ((bool)volume)
		{
			original.Get(volume);
		}
	}

	private void Start()
	{
		currentRenderQuality = QualitySettings.names[QualitySettings.GetQualityLevel()];
		ApplyQuality(currentRenderQuality);
	}

	private void Update()
	{
		string text = QualitySettings.names[QualitySettings.GetQualityLevel()];
		if (text != currentRenderQuality)
		{
			ApplyQuality(text);
			currentRenderQuality = text;
		}
	}

	private void OnDisable()
	{
		if ((bool)volume)
		{
			original.Apply(volume);
		}
	}

	private void ApplyQuality(string renderQuality)
	{
		PostProcessingQuality[] array = qualities;
		foreach (PostProcessingQuality postProcessingQuality in array)
		{
			if (postProcessingQuality.qualityName == renderQuality)
			{
				postProcessingQuality.Apply(volume);
				break;
			}
		}
	}
}
