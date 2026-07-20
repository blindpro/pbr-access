using System;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class RoadCross : MonoBehaviour
{
	public enum Direction
	{
		Start,
		End
	}

	[Serializable]
	public class RoadList
	{
		public Bezier road;

		public Direction roadStartPoint;
	}

	[HideInInspector]
	public bool seeInspector;

	public RoadData roadData;

	public int roadID;

	public List<Transform> anchorList = new List<Transform>();

	public List<RoadList> roadList = new List<RoadList>();

	public List<Transform> crossRoadBorders = new List<Transform>();

	public float borderWidth = 6f;

	public float borderSize = 2f;

	public float borderSlopeSize = 6f;

	public float coverOffset = 3f;

	public GameObject grpDecal;

	public GameObject objCollider;

	[HideInInspector]
	public int currentroadCrossGroundPreset;

	[HideInInspector]
	public int indexRoadSection;

	[HideInInspector]
	public bool showTerrainBorderParams;

	[HideInInspector]
	public int indexCrossRoadGroundPrefab;

	public float anchorDistWhenNewPointCreated = 10f;

	public int roadSubdivisionWhenGenerated = 3;

	private void OnDrawGizmos()
	{
		for (int i = 0; i < anchorList.Count; i++)
		{
			if (i == 0)
			{
				Gizmos.color = Color.blue;
			}
			if (i == 1)
			{
				Gizmos.color = Color.yellow;
			}
			if (i == 2)
			{
				Gizmos.color = Color.green;
			}
			if (i == 3)
			{
				Gizmos.color = Color.magenta;
			}
			if (anchorList[i] != null)
			{
				Gizmos.DrawSphere(anchorList[i].position, 0.3f);
			}
		}
	}
}
