using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public abstract class ItemAnimationDataBehaviour : MonoBehaviour
{
	public abstract RecoilData GetCameraRecoilData();

	public abstract RecoilData GetWeaponRecoilData();

	public abstract RecoilData GetRecoilData(MotionType motionType);

	public abstract LowerData GetLowerData();

	public abstract LeaningData GetLeaningData();

	public abstract ItemOffsets GetItemOffsets();
}
