using UnityEngine;

public class LODGlobalOptimizer : MonoBehaviour
{
	[Range(1f, 5f)]
	public float multiplier = 2f;

	public float minLastLOD = 0.1f;

	private void Start()
	{
		LODGroup[] componentsInChildren = GetComponentsInChildren<LODGroup>(includeInactive: true);
		foreach (LODGroup lODGroup in componentsInChildren)
		{
			LOD[] lODs = lODGroup.GetLODs();
			for (int j = 0; j < lODs.Length; j++)
			{
				lODs[j].screenRelativeTransitionHeight *= multiplier;
				if (j == lODs.Length - 1)
				{
					lODs[j].screenRelativeTransitionHeight = Mathf.Max(lODs[j].screenRelativeTransitionHeight, minLastLOD);
				}
			}
			lODGroup.SetLODs(lODs);
			lODGroup.RecalculateBounds();
		}
		Debug.Log("LOD distances adjusted correctly");
	}
}
