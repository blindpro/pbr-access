using System;

namespace HardShellStudios.CompleteControl;

[Serializable]
public struct hInputDetails
{
	public string Name;

	public string UniqueName;

	public KeyType Type;

	public hInputBundle Positive;

	public hInputBundle Negative;

	public TargetController targetController;

	public AxisCode Axis;

	public bool Invert;

	public float Sensitivity;

	public float val;

	public string ToStringEx()
	{
		return "Name:" + Name + " -- UniqueName:" + UniqueName + " -- Type:" + Type.ToString() + " -- PositiveP:" + Positive.Primary.ToString() + " -- PositiveS:" + Positive.Secondary.ToString() + " -- NegativeP:" + Negative.Primary.ToString() + " -- NegativeS:" + Negative.Secondary.ToString() + " -- TargetController:" + targetController.ToString() + " -- AxisCode:" + Axis;
	}
}
