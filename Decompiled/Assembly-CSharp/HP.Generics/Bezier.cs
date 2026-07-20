using System;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class Bezier : MonoBehaviour
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

	public enum CrossDirection
	{
		Blue,
		Yellow,
		Green,
		Majenta
	}

	[Serializable]
	public class BorderInfo
	{
		[HideInInspector]
		public float borderLeftStart;

		public float borderLeftStop = 0.5f;

		public float borderLeftSlopeSize = 1f;

		[HideInInspector]
		public float borderRightStart;

		public float borderRightStop = 0.5f;

		public float borderRightSlopeSize = 1f;

		public bool bShowRoad = true;

		public bool bShowBorder = true;

		public bool bShowSlope = true;
	}

	public bool seeInspector;

	public bool seeCustomEditor;

	[HideInInspector]
	public int toolbarIndex;

	public RoadData roadData;

	public int roadID;

	public List<PointDescription> pointsList = new List<PointDescription>();

	public bool linkControlPoints = true;

	public bool loop;

	public float tileSize = 1f;

	public float roadSize = 8f;

	[HideInInspector]
	public float totalDistance;

	public List<SubPoint> distVecList = new List<SubPoint>();

	public Transform crossRoadpoint;

	public Transform crossRoadpointIn;

	public CrossDirection crossDirection;

	public CrossDirection crossDirectionIn;

	public float lastAnchorDist = 30f;

	public float anchorDistWhenNewPointCreated = 10f;

	public List<Color> colorList = new List<Color>();

	public bool isChangeDone = true;

	public int smoothRotSteps = 15;

	public bool autoDisableColliderBorders = true;

	public bool bSelection = true;

	public int selectStart;

	public int selectStop = 20;

	public BorderInfo selectBorderInfo = new BorderInfo();

	public List<Transform> selectBorderTrans = new List<Transform>();

	[HideInInspector]
	public int hotControlID;

	[HideInInspector]
	public int closestPoint;

	public List<int> closestPointList = new List<int>();

	[HideInInspector]
	public int indexCreatorCrossRoad;

	[HideInInspector]
	public int indexCrossRoadTab;

	[HideInInspector]
	public int indexConnectionCrossRoadTab;

	[HideInInspector]
	public int indexCrossRoadGroundPrefab;

	public bool isRoadMeshUpdated = true;

	public int vert;

	public int roadSubdivisionWhenGenerated = 3;

	public void Reset()
	{
		loop = false;
		tileSize = 20f;
		roadSize = 12f;
		totalDistance = 10f;
		crossRoadpoint = null;
		crossRoadpointIn = null;
		lastAnchorDist = 30f;
		smoothRotSteps = 15;
		GetComponent<MeshFilter>().sharedMesh = null;
		GetComponent<MeshCollider>().sharedMesh = null;
		for (int i = 0; i < 5; i++)
		{
			selectBorderTrans.Add(base.transform.GetChild(1).GetChild(i).transform);
		}
		for (int j = 0; j < selectBorderTrans.Count; j++)
		{
			selectBorderTrans[j].GetComponent<MeshFilter>().sharedMesh = null;
		}
		for (int k = 0; k < selectBorderTrans.Count; k++)
		{
			selectBorderTrans[k].GetComponent<MeshCollider>().sharedMesh = null;
		}
	}

	private void OnDrawGizmosSelected()
	{
		DisplayRoadBorders();
	}

	private void DisplayRoadBorders()
	{
		if (!roadData.isGizmosDisplayed)
		{
			return;
		}
		int num = 0;
		int num2 = smoothRotSteps;
		for (int i = 0; i < distVecList.Count - 2; i++)
		{
			int firstSpot = distVecList[i].firstSpot;
			Vector3 rhs = Vector3.up;
			if ((bool)crossRoadpoint && i < smoothRotSteps)
			{
				num2--;
				float num3 = (float)num2 / (float)smoothRotSteps;
				rhs = Vector3.up * (1f - num3) + crossRoadpoint.up * num3;
			}
			if ((bool)crossRoadpointIn && distVecList.Count - smoothRotSteps < i)
			{
				num++;
				float num4 = (float)num / (float)smoothRotSteps;
				rhs = Vector3.up * (1f - num4) + crossRoadpointIn.up * num4;
			}
			Vector3 pointPosition = RoadCreation.GetPointPosition(firstSpot, firstSpot + 1, firstSpot + 2, firstSpot + 3, distVecList[i].distanceFromSpot, pointsList);
			Vector3 vector = pointPosition + 45f * RoadCreation.GetVelocity(firstSpot, firstSpot + 1, firstSpot + 2, firstSpot + 3, distVecList[i].distanceFromSpot, pointsList).normalized;
			Vector3 lhs = pointPosition - vector;
			int firstSpot2 = distVecList[i + 1].firstSpot;
			Vector3 pointPosition2 = RoadCreation.GetPointPosition(firstSpot2, firstSpot2 + 1, firstSpot2 + 2, firstSpot2 + 3, distVecList[i + 1].distanceFromSpot, pointsList);
			Vector3 vector2 = pointPosition2 + 45f * RoadCreation.GetVelocity(firstSpot2, firstSpot2 + 1, firstSpot2 + 2, firstSpot2 + 3, distVecList[i + 1].distanceFromSpot, pointsList).normalized;
			Vector3 lhs2 = pointPosition2 - vector2;
			Vector3 vector3 = Vector3.zero;
			Vector3 vector4 = Vector3.zero;
			Vector3 vector5 = Vector3.zero;
			Vector3 vector6 = Vector3.zero;
			for (int j = 0; j < 6; j++)
			{
				float num5 = 0f - selectBorderInfo.borderLeftStart;
				switch (j)
				{
				case 1:
					num5 = 0f - selectBorderInfo.borderLeftStop;
					break;
				case 2:
					num5 = 0f - selectBorderInfo.borderLeftStop - selectBorderInfo.borderLeftSlopeSize;
					break;
				case 3:
					num5 = selectBorderInfo.borderRightStart;
					break;
				case 4:
					num5 = selectBorderInfo.borderRightStop;
					break;
				case 5:
					num5 = selectBorderInfo.borderRightStop + selectBorderInfo.borderRightSlopeSize;
					break;
				}
				Vector3 vector7 = Vector3.zero;
				Vector3 vector8 = Vector3.zero;
				switch (j)
				{
				case 0:
				case 1:
				case 2:
					vector7 = (num5 - 0.5f) * roadSize * Vector3.Cross(lhs, rhs).normalized;
					vector8 = (num5 - 0.5f) * roadSize * Vector3.Cross(lhs2, rhs).normalized;
					break;
				case 3:
				case 4:
				case 5:
					vector7 = (num5 + 0.5f) * roadSize * Vector3.Cross(lhs, rhs).normalized;
					vector8 = (num5 + 0.5f) * roadSize * Vector3.Cross(lhs2, rhs).normalized;
					break;
				}
				if (!bSelection || i <= 1)
				{
					continue;
				}
				if (selectStop > distVecList.Count - 1)
				{
					selectStop = distVecList.Count - 1;
				}
				switch (j)
				{
				case 1:
					num5 = 0f - selectBorderInfo.borderLeftStop;
					break;
				case 4:
					num5 = selectBorderInfo.borderRightStop;
					break;
				}
				switch (j)
				{
				case 2:
					num5 = 0f - selectBorderInfo.borderLeftStop - selectBorderInfo.borderLeftSlopeSize;
					break;
				case 5:
					num5 = selectBorderInfo.borderRightStop + selectBorderInfo.borderRightSlopeSize;
					break;
				}
				switch (j)
				{
				case 1:
				case 4:
					if (j == 1)
					{
						vector7 = (num5 - 0.5f) * roadSize * Vector3.Cross(lhs, rhs).normalized;
						vector8 = (num5 - 0.5f) * roadSize * Vector3.Cross(lhs2, rhs).normalized;
						vector3 = vector7;
					}
					if (j == 4)
					{
						vector7 = (num5 + 0.5f) * roadSize * Vector3.Cross(lhs, rhs).normalized;
						vector8 = (num5 + 0.5f) * roadSize * Vector3.Cross(lhs2, rhs).normalized;
						vector5 = vector7;
					}
					break;
				case 2:
				case 5:
					if (j == 2)
					{
						vector7 = (num5 - 0.5f) * roadSize * Vector3.Cross(lhs, rhs).normalized;
						vector8 = (num5 - 0.5f) * roadSize * Vector3.Cross(lhs2, rhs).normalized;
						vector4 = vector7;
					}
					if (j == 5)
					{
						vector7 = (num5 + 0.5f) * roadSize * Vector3.Cross(lhs, rhs).normalized;
						vector8 = (num5 + 0.5f) * roadSize * Vector3.Cross(lhs2, rhs).normalized;
						vector6 = vector7;
					}
					break;
				}
				if (i >= selectStart && i < selectStop - 1)
				{
					if (j == 4)
					{
						if (!isChangeDone)
						{
							Gizmos.color = colorList[0];
						}
						else
						{
							Gizmos.color = colorList[2];
						}
					}
					if (j == 5)
					{
						if (!isChangeDone)
						{
							Gizmos.color = colorList[1];
						}
						else
						{
							Gizmos.color = colorList[3];
						}
					}
					Gizmos.DrawLine(pointPosition + vector7 + base.transform.position, pointPosition2 + vector8 + base.transform.position);
				}
				if (i < selectStart || i >= selectStop)
				{
					continue;
				}
				if (j == 4)
				{
					if (!isChangeDone)
					{
						Gizmos.color = colorList[0];
					}
					else
					{
						Gizmos.color = colorList[2];
					}
				}
				if (j == 5)
				{
					if (!isChangeDone)
					{
						Gizmos.color = colorList[1];
					}
					else
					{
						Gizmos.color = colorList[3];
					}
				}
				if (i == selectStart || i == selectStop - 1)
				{
					if (j == 5)
					{
						Gizmos.DrawLine(pointPosition + vector3 + base.transform.position, pointPosition + vector4 + base.transform.position);
						Gizmos.DrawLine(pointPosition + vector5 + base.transform.position, pointPosition + vector6 + base.transform.position);
					}
					if (j == 4)
					{
						Gizmos.DrawLine(pointPosition - 0.5f * roadSize * Vector3.Cross(lhs, rhs).normalized + base.transform.position, pointPosition + vector3 + base.transform.position);
						Gizmos.DrawLine(pointPosition + 0.5f * roadSize * Vector3.Cross(lhs, rhs).normalized + base.transform.position, pointPosition + vector5 + base.transform.position);
					}
				}
			}
		}
	}
}
