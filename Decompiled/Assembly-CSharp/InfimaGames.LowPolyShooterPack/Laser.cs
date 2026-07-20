using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Laser : LaserBehaviour
{
	[Tooltip("Sprite. Displayed on the player's interface.")]
	[SerializeField]
	private Sprite sprite;

	[Tooltip("Type of laser.")]
	[SerializeField]
	private LaserType laserType;

	[Tooltip("True if the lasersight should start active.")]
	[SerializeField]
	private bool active = true;

	[Tooltip("If true, the laser will be turned off automatically while the character is running.")]
	[SerializeField]
	private bool turnOffWhileRunning = true;

	[Tooltip("If true, the laser will be turned off automatically while the character is aiming.")]
	[SerializeField]
	private bool turnOffWhileAiming = true;

	[Tooltip("The AudioClip played when toggling the laser.")]
	[SerializeField]
	private AudioClip toggleClip;

	[Tooltip("The AudioSettings used for the toggleClip.")]
	[SerializeField]
	private AudioSettings toggleAudioSettings;

	[Tooltip("Transform of the laser.")]
	[SerializeField]
	private Transform laserTransform;

	[Tooltip("Determines how thick the laser beam is.")]
	[SerializeField]
	private float beamThickness = 1.2f;

	[Tooltip("Maximum distance for tracing the laser beam.")]
	[SerializeField]
	private float beamMaxDistance = 500f;

	private Transform beamParent;

	public override Sprite GetSprite()
	{
		return sprite;
	}

	public override bool GetTurnOffWhileRunning()
	{
		return turnOffWhileRunning;
	}

	public override bool GetTurnOffWhileAiming()
	{
		return turnOffWhileAiming;
	}

	public override void Toggle()
	{
		active = !active;
		Reapply();
		if (toggleClip != null)
		{
			ServiceLocator.Current.Get<IAudioManagerService>().PlayOneShot3D(toggleClip, toggleAudioSettings, laserTransform);
		}
	}

	public override void Reapply()
	{
		if (laserTransform != null)
		{
			laserTransform.gameObject.SetActive(active);
		}
	}

	public override void Hide()
	{
		if (laserTransform != null)
		{
			laserTransform.gameObject.SetActive(value: false);
		}
	}

	private void Awake()
	{
		if (!(laserTransform == null))
		{
			beamParent = laserTransform.parent;
		}
	}

	private void Update()
	{
		if (!(laserTransform == null))
		{
			float z = beamMaxDistance;
			if (Physics.Raycast(new Ray(laserTransform.position, beamParent.forward), out var hitInfo, beamMaxDistance))
			{
				z = hitInfo.distance * 5f;
			}
			beamParent.localScale = new Vector3(beamThickness, beamThickness, z);
		}
	}
}
