using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class CrossRoadPreset : MonoBehaviour
{
	public List<Vector3> anchorPosList = new List<Vector3>();

	public Vector3 colliderTransformScale = Vector3.zero;

	public int roadTypeCreatedByDefault;

	public float anchorDistWhenNewPointCreated = 10f;
}
