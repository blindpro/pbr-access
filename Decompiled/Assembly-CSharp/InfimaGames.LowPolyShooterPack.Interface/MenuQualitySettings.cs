using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class MenuQualitySettings : Element
{
	[Tooltip("Canvas to play animations on.")]
	[SerializeField]
	private GameObject animatedCanvas;

	[Tooltip("Animation played when showing this menu.")]
	[SerializeField]
	private AnimationClip animationShow;

	[Tooltip("Animation played when hiding this menu.")]
	[SerializeField]
	private AnimationClip animationHide;

	private Animation animationComponent;

	private bool menuIsEnabled;

	private PostProcessVolume postProcessingVolume;

	private PostProcessVolume postProcessingVolumeScope;

	private DepthOfField depthOfField;

	private void Start()
	{
		animatedCanvas.GetComponent<CanvasGroup>().alpha = 0f;
		animationComponent = animatedCanvas.GetComponent<Animation>();
		postProcessingVolume = GameObject.Find("Post Processing Volume")?.GetComponent<PostProcessVolume>();
		postProcessingVolumeScope = GameObject.Find("Post Processing Volume Scope")?.GetComponent<PostProcessVolume>();
		if (postProcessingVolume != null)
		{
			postProcessingVolume.profile.TryGetSettings<DepthOfField>(out depthOfField);
		}
	}

	protected override void Tick()
	{
		if (characterBehaviour.IsCursorLocked())
		{
			if (menuIsEnabled)
			{
				Hide();
			}
		}
		else if (!menuIsEnabled)
		{
			Show();
		}
	}

	private void Show()
	{
		menuIsEnabled = true;
		animationComponent.clip = animationShow;
		animationComponent.Play();
		if (depthOfField != null)
		{
			depthOfField.active = true;
		}
	}

	private void Hide()
	{
		menuIsEnabled = false;
		animationComponent.clip = animationHide;
		animationComponent.Play();
		if (depthOfField != null)
		{
			depthOfField.active = false;
		}
	}

	private void SetPostProcessingState(bool value = true)
	{
		if (postProcessingVolume != null)
		{
			postProcessingVolume.enabled = value;
		}
		if (postProcessingVolumeScope != null)
		{
			postProcessingVolumeScope.enabled = value;
		}
	}

	public void SetQualityVeryLow()
	{
		QualitySettings.SetQualityLevel(0);
		SetPostProcessingState(value: false);
	}

	public void SetQualityLow()
	{
		QualitySettings.SetQualityLevel(1);
		SetPostProcessingState(value: false);
	}

	public void SetQualityMedium()
	{
		QualitySettings.SetQualityLevel(2);
		SetPostProcessingState();
	}

	public void SetQualityHigh()
	{
		QualitySettings.SetQualityLevel(3);
		SetPostProcessingState();
	}

	public void SetQualityVeryHigh()
	{
		QualitySettings.SetQualityLevel(4);
		SetPostProcessingState();
	}

	public void SetQualityUltra()
	{
		QualitySettings.SetQualityLevel(5);
		SetPostProcessingState();
	}

	public void Restart()
	{
		SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
	}

	public void Quit()
	{
		Application.Quit();
	}
}
