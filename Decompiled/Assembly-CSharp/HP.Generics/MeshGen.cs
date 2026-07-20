using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class MeshGen : MonoBehaviour
{
	public enum ColliderType
	{
		SameAsMesh,
		Special,
		NoCollider
	}

	public bool seeInspector = true;

	public bool moreOptions;

	public InstantiateObjectUsingBezierCurve instantiateObjectUsingBezierCurve;

	public List<Vector3> shapePosList = new List<Vector3>();

	[HideInInspector]
	public List<Vector3> tmpShapePosList = new List<Vector3>();

	[HideInInspector]
	public List<Vector3> pathPosList = new List<Vector3>();

	public int interval = 10;

	public int startPathPos;

	public int endPathPos;

	public bool flipTexture;

	public bool flipMesh;

	[HideInInspector]
	public Transform overrideStartPos;

	[HideInInspector]
	public Transform overrideEndPos;

	[Header("UV Face and Back")]
	public bool flatFront;

	public int smoothFlatFront = 1;

	public bool flatBack;

	public int smoothFlatBack = 1;

	public List<int> startFaceList = new List<int>();

	public bool enableUVPosition = true;

	public Vector3 uvOffset;

	public float atlasFaceSize = 10f;

	public Vector2 flipFaceUVFront = Vector2.one;

	public Vector2 flipFaceUVBack = new Vector2(-1f, 1f);

	public List<Vector2> uvPos = new List<Vector2>();

	public bool isNormalsDisplayed;

	public bool isTangentsDisplayed;

	public float tileSizeZ = 3f;

	public bool generateOnlyASpecialCollider;

	public List<Vector3> shapeColliderPosList = new List<Vector3>();

	public List<int> colliderStartFaceList = new List<int>();

	public ColliderType colliderType = ColliderType.NoCollider;

	private void OnDrawGizmosSelected()
	{
		if (AllowUvModification())
		{
			DisplayUVSquareInSceneView();
			ReturnVerticesPositionInUV();
		}
		if (isNormalsDisplayed)
		{
			ShowNormals();
		}
		if (isTangentsDisplayed)
		{
			ShowTangents();
		}
	}

	private bool AllowUvModification()
	{
		if (enableUVPosition && pathPosList.Count > 1 && shapePosList.Count > 0)
		{
			return true;
		}
		return false;
	}

	public void DisplayUVSquareInSceneView()
	{
		Vector3 normalized = Vector3.Cross((pathPosList[1] - pathPosList[0]).normalized, Vector3.up).normalized;
		Vector3 vector = pathPosList[0] + shapePosList[0].y * Vector3.up - shapePosList[0].x * normalized;
		Vector3 vector2 = uvOffset.x * normalized + uvOffset.y * Vector3.up;
		Vector3 vector3 = vector + vector2 + normalized * atlasFaceSize * 0.5f - Vector3.up * atlasFaceSize * 0.5f;
		Vector3 vector4 = vector + vector2 - normalized * atlasFaceSize * 0.5f - Vector3.up * atlasFaceSize * 0.5f;
		Vector3 vector5 = vector + vector2 + normalized * atlasFaceSize * 0.5f + Vector3.up * atlasFaceSize * 0.5f;
		Vector3 vector6 = vector + vector2 - normalized * atlasFaceSize * 0.5f + Vector3.up * atlasFaceSize * 0.5f;
		Gizmos.DrawLine(vector3, vector4);
		Gizmos.DrawLine(vector4, vector6);
		Gizmos.DrawLine(vector6, vector5);
		Gizmos.DrawLine(vector5, vector3);
	}

	public List<Vector2> ReturnVerticesPositionInUV()
	{
		uvPos.Clear();
		float num = uvOffset.x / atlasFaceSize;
		float num2 = (0f - uvOffset.y) / atlasFaceSize;
		float x = 0.5f + num;
		float y = 0.5f + num2;
		Vector2 vector = new Vector2(x, y);
		uvPos.Add(vector);
		for (int i = 1; i < shapePosList.Count; i++)
		{
			float x2 = (shapePosList[i].x - shapePosList[0].x) / atlasFaceSize;
			float y2 = (shapePosList[i].y - shapePosList[0].y) / atlasFaceSize;
			Vector2 item = vector + new Vector2(x2, y2);
			uvPos.Add(item);
		}
		return uvPos;
	}

	private void ShowNormals()
	{
		Mesh sharedMesh = GetComponent<MeshFilter>().sharedMesh;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.localScale);
		if ((bool)sharedMesh)
		{
			for (int i = 0; i < sharedMesh.vertexCount; i++)
			{
				Gizmos.DrawRay(sharedMesh.vertices[i], sharedMesh.normals[i] * 0.5f);
			}
		}
	}

	private void ShowTangents()
	{
		Mesh sharedMesh = GetComponent<MeshFilter>().sharedMesh;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.localScale);
		if ((bool)sharedMesh)
		{
			for (int i = 0; i < sharedMesh.vertexCount; i++)
			{
				Gizmos.DrawRay(sharedMesh.vertices[i], sharedMesh.tangents[i] * 0.5f);
			}
		}
	}
}
