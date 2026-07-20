using System;
using UnityEngine;

public class RenderResolutionQuality : MonoBehaviour
{
	[Serializable]
	public class RenderQuality
	{
		public string qualityName = "";

		public float resolutionQuality = 1f;

		public void Apply(RenderTexture renderTexture, Camera cam)
		{
			if (!cam)
			{
				Debug.LogError("apply to renderTexture camera null");
				return;
			}
			if (!renderTexture)
			{
				Debug.LogError("apply to renderTexture null");
				return;
			}
			renderTexture.Release();
			renderTexture.height = (int)((float)Screen.height * resolutionQuality);
			renderTexture.width = (int)((float)Screen.width * resolutionQuality);
			renderTexture.Create();
			Debug.Log("ResizeRenderTexture " + renderTexture.width + " * " + renderTexture.height);
			cam.targetTexture = renderTexture;
			cam.aspect = (float)Screen.width / (float)Screen.height;
			Debug.Log(qualityName + " renderTexture qualitty applied to camera " + cam.name);
		}
	}

	public RenderQuality[] qualities;

	public RenderTexture renderTexture;

	private Camera cam;

	private string currentRenderQuality = "";

	private int currentRenderWidth;

	private int currentRenderHeight;

	private void OnEnable()
	{
		cam = GetComponent<Camera>();
	}

	private void Start()
	{
		if (cam.enabled)
		{
			currentRenderQuality = QualitySettings.names[QualitySettings.GetQualityLevel()];
			currentRenderWidth = Screen.width;
			currentRenderHeight = Screen.height;
			ApplyQuality(currentRenderQuality);
		}
	}

	private void OnDisable()
	{
	}

	private void Update()
	{
		if (cam.enabled)
		{
			string text = QualitySettings.names[QualitySettings.GetQualityLevel()];
			if (text != currentRenderQuality || currentRenderWidth != Screen.width || currentRenderHeight != Screen.height)
			{
				ApplyQuality(text);
				currentRenderQuality = text;
				currentRenderWidth = Screen.width;
				currentRenderHeight = Screen.height;
			}
		}
	}

	private void ApplyQuality(string renderQuality)
	{
		RenderQuality[] array = qualities;
		foreach (RenderQuality renderQuality2 in array)
		{
			if (renderQuality2.qualityName == renderQuality)
			{
				renderQuality2.Apply(renderTexture, cam);
				break;
			}
		}
	}
}
