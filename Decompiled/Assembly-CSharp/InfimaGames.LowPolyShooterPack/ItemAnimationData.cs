using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class ItemAnimationData : ItemAnimationDataBehaviour
{
	[Tooltip("The object that contains all offset data for this item.")]
	[SerializeField]
	private ItemOffsets itemOffsets;

	[Tooltip("This object contains all the data needed for us to set the lowered pose of this weapon.")]
	[SerializeField]
	private LowerData lowerData;

	[Tooltip("LeaningData. Contains all the information on what this weapon should do while the character is leaning.")]
	[SerializeField]
	private LeaningData leaningData;

	[Tooltip("Weapon Recoil Data Asset. Used to get some camera recoil values, usually for weapons.")]
	[SerializeField]
	private RecoilData cameraRecoilData;

	[Tooltip("Weapon Recoil Data Asset. Used to get some recoil values, usually for weapons.")]
	[SerializeField]
	private RecoilData weaponRecoilData;

	public override RecoilData GetCameraRecoilData()
	{
		return cameraRecoilData;
	}

	public override RecoilData GetWeaponRecoilData()
	{
		return weaponRecoilData;
	}

	public override RecoilData GetRecoilData(MotionType motionType)
	{
		if (motionType != MotionType.Item)
		{
			return GetCameraRecoilData();
		}
		return GetWeaponRecoilData();
	}

	public override LowerData GetLowerData()
	{
		return lowerData;
	}

	public override LeaningData GetLeaningData()
	{
		return leaningData;
	}

	public override ItemOffsets GetItemOffsets()
	{
		return itemOffsets;
	}
}
