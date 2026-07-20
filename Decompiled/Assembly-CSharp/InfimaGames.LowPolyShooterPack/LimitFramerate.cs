using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class LimitFramerate : MonoBehaviour
{
	[Tooltip("Are we limiting the framerate, or keeping it as is by default?")]
	[SerializeField]
	private bool limit;

	[Tooltip("Max frames the game can have while limited.")]
	[SerializeField]
	private int framerate = 15;

	private int defaultVSync;

	private int defaultTargetFramerate;

	private void Awake()
	{
		defaultVSync = QualitySettings.vSyncCount;
		defaultTargetFramerate = Application.targetFrameRate;
	}

	private void Update()
	{
		QualitySettings.vSyncCount = ((!limit) ? defaultVSync : 0);
		Application.targetFrameRate = (limit ? framerate : defaultTargetFramerate);
	}
}
