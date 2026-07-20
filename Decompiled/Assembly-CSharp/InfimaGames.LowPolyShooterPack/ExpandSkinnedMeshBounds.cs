using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class ExpandSkinnedMeshBounds : MonoBehaviour
{
	[Tooltip("How much to multiply the X size of each bounding box.")]
	public float xScaleFactor = 1.5f;

	public bool showBoxes;

	private SkinnedMeshRenderer[] skinnedMeshes;

	private void Start()
	{
		skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		SkinnedMeshRenderer[] array = skinnedMeshes;
		foreach (SkinnedMeshRenderer obj in array)
		{
			Bounds localBounds = obj.localBounds;
			float num = Mathf.Max(localBounds.size.x, Mathf.Max(localBounds.size.y, localBounds.size.z)) * xScaleFactor;
			Bounds localBounds2 = new Bounds(size: new Vector3(num, num, num), center: localBounds.center);
			obj.localBounds = localBounds2;
		}
	}
}
