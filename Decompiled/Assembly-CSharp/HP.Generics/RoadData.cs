using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

[CreateAssetMenu(fileName = "RoadData", menuName = "HP/RoadData")]
public class RoadData : ScriptableObject
{
	public bool MoreOptions;

	public bool HelpBox;

	public int currentelectedDatas;

	public GameObject crossRoadprefab;

	public int iD;

	public float groundOffset = 0.03f;

	public bool isGizmosDisplayed = true;

	public int currentPrefabSelected;

	public bool isRoadPrefabShown = true;

	public List<GameObject> roadPrefabList = new List<GameObject>();

	public List<GameObject> crossRoadGroundPrefabList = new List<GameObject>();

	public List<PointDescription> pointsList = new List<PointDescription>();

	public Vector3 curvePosRef;

	public int howManyPointToCopy;

	public int currentProcedualGD;
}
