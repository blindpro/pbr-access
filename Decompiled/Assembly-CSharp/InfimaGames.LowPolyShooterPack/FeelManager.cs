using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class FeelManager : MonoBehaviour
{
	[Tooltip("Feel Preset. This drives the feel of the entire project, both for weapons, and also for the camera. It is a very important object.")]
	[SerializeField]
	private FeelPreset preset;

	public FeelPreset Preset
	{
		get
		{
			return preset;
		}
		set
		{
			preset = value;
		}
	}
}
