using System;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class RoadBumpPathGen : MonoBehaviour
{
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

	public List<Vector3> shapePosList = new List<Vector3>();

	[HideInInspector]
	public List<Vector3> extrudePathPosList = new List<Vector3>();

	[HideInInspector]
	public float totalDistance;

	[HideInInspector]
	public List<SubPoint> distVecList = new List<SubPoint>();

	public float distanceBetweenDistVec = 0.5f;

	[HideInInspector]
	public List<SubPoint> distVecListPlusOffsetFinal = new List<SubPoint>();

	public bool showGizmos;

	private void OnDrawGizmosSelected()
	{
		if (showGizmos)
		{
			for (int i = 0; i < distVecListPlusOffsetFinal.Count; i++)
			{
				Vector3 center = distVecListPlusOffsetFinal[i].spotPos + base.transform.position;
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(center, 0.15f);
			}
		}
	}
}
