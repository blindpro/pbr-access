using System.Runtime.CompilerServices;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[CreateAssetMenu(fileName = "SO_Feel_Preset", menuName = "Infima Games/Low Poly Shooter Pack/Feel Preset", order = 0)]
public class FeelPreset : ScriptableObject
{
	[Tooltip("Camera Feel. Holds the values relating to how the camera feels when playing.")]
	[SerializeField]
	private Feel cameraFeel;

	[Tooltip("Item Feel. Holds the values relating to how the items feels when playing.")]
	[SerializeField]
	private Feel itemFeel;

	public Feel GetFeel(MotionType motionType)
	{
		return motionType switch
		{
			MotionType.Camera => cameraFeel, 
			MotionType.Item => itemFeel, 
			_ => throw new SwitchExpressionException(motionType), 
		};
	}
}
