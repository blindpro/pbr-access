using System;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class InstantiateObjectUsingBezierCurve : MonoBehaviour
{
	[Serializable]
	public class RandomizedObjectParams
	{
		public int proba = 1;

		public GameObject obj;
	}

	[Serializable]
	public class SubPoint
	{
		public Vector3 spotPos;

		public int firstSpot;

		public float distanceFromSpot;

		public float segLength;

		public SubPoint(Vector3 sp, int fS, float dFS)
		{
			spotPos = sp;
			firstSpot = fS;
			distanceFromSpot = dFS;
		}
	}

	public enum PrefabRotation
	{
		Vertical,
		FollowPathRotation,
		LookAtNextPrefab
	}

	public bool seeInspector = true;

	public bool moreOptions;

	public bool advanced;

	public List<GameObject> objsList = new List<GameObject>();

	public int percentageProba = 20;

	public List<RandomizedObjectParams> objsRandomList = new List<RandomizedObjectParams>();

	[Space]
	public Vector3 objExtraOffset = Vector3.zero;

	public Vector3 objExtraRotation = Vector3.zero;

	[HideInInspector]
	public List<PointDescription> pointsList = new List<PointDescription>();

	[HideInInspector]
	public float totalDistance;

	[HideInInspector]
	public List<SubPoint> distVecList = new List<SubPoint>();

	public float distanceBetweenDistVec = 0.5f;

	public float distVecOffset = 6f;

	[HideInInspector]
	public List<SubPoint> distVecListPlusOffsetFinal = new List<SubPoint>();

	public int interval = 10;

	[HideInInspector]
	public int startPathPos;

	[HideInInspector]
	public int endPathPos;

	public bool showGizmo;

	[HideInInspector]
	public List<Terrain> terrList = new List<Terrain>();

	public PrefabRotation prefabRotation;

	public bool createFolderInside;

	public GameObject grpThatContainInstantiateObjects;

	private void OnDrawGizmosSelected()
	{
		if (showGizmo)
		{
			for (int i = 0; i < distVecList.Count; i++)
			{
				Gizmos.DrawSphere(distVecList[i].spotPos + base.transform.position, 0.15f);
			}
			for (int j = 0; j < distVecListPlusOffsetFinal.Count; j++)
			{
				Vector3 center = distVecListPlusOffsetFinal[j].spotPos + base.transform.position;
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(center, 0.15f);
			}
		}
	}
}
