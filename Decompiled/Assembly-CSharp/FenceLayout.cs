using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FenceLayout : MonoBehaviour
{
	public enum EditMode
	{
		None,
		Add,
		Delete
	}

	public List<Vector3> FencePoints = new List<Vector3>();

	public Transform[] FencePrefabs = new Transform[1];

	public Transform PostPrefab;

	public Vector3 FenceEndOffset;

	public float FenceRotation;

	public float FenceLength = 2f;

	public Vector3 FenceScale = new Vector3(1f, 1f, 1f);

	public bool CompleteLoop;

	public bool ObjectMode;

	public Vector2 PositionVariation = new Vector2(0f, 0f);

	public Vector2 RotationVariation = new Vector2(-180f, 180f);

	public Vector2 ScaleVariation = new Vector2(1f, 1f);

	public bool UseShear = true;

	public bool UseRaycast;

	public float RaycastOffsetMax = 1f;

	public float RaycastOffsetMin = -1f;

	public int LayerMask = -5;

	private EditMode m_currentEditMode;

	private Vector3 m_mousePos;

	private void OnDrawGizmosSelected()
	{
		Vector3 vector = default(Vector3);
		bool flag = false;
		int num = ((m_currentEditMode == EditMode.Delete) ? GetDeletePointIdx() : (-1));
		for (int i = 0; i < FencePoints.Count; i++)
		{
			Vector3 vector2 = FencePoints[i] + base.transform.position;
			if (m_currentEditMode == EditMode.Delete && i == num)
			{
				Gizmos.color = new Color(1f, 0f, 0f);
				Gizmos.DrawSphere(vector2, 0.5f);
			}
			else
			{
				Gizmos.color = ((m_currentEditMode == EditMode.Add) ? new Color(1f, 1f, 1f) : new Color(0.2f, 1f, 0.2f));
				Gizmos.DrawSphere(vector2, 0.3f);
			}
			if (flag)
			{
				Gizmos.color = new Color(0.2f, 1f, 0.2f);
				Gizmos.DrawLine(vector, vector2);
			}
			vector = vector2;
			flag = true;
		}
		if (m_currentEditMode != EditMode.Add)
		{
			return;
		}
		Gizmos.color = new Color(1f, 0f, 0f);
		Gizmos.DrawSphere(m_mousePos, 0.3f);
		Gizmos.color = new Color(1f, 1f, 0f);
		if (FencePoints.Count > 0)
		{
			int addPointIdx = GetAddPointIdx();
			if (addPointIdx > 0)
			{
				Gizmos.DrawLine(m_mousePos, FencePoints[addPointIdx - 1] + base.transform.position);
			}
			if (addPointIdx < FencePoints.Count)
			{
				Gizmos.DrawLine(m_mousePos, FencePoints[addPointIdx] + base.transform.position);
			}
		}
	}

	public void SetEditMode(EditMode mode, Vector3 mousePos)
	{
		m_currentEditMode = mode;
		m_mousePos = mousePos;
	}

	public int GetAddPointIdx()
	{
		if (FencePoints.Count == 0)
		{
			return 0;
		}
		if (FencePoints.Count == 1)
		{
			return 1;
		}
		Vector3 vector = m_mousePos - base.transform.position;
		int num = 0;
		float num2 = float.MaxValue;
		for (int i = 0; i < FencePoints.Count; i++)
		{
			float sqrMagnitude = (FencePoints[i] - vector).sqrMagnitude;
			if (sqrMagnitude < num2)
			{
				num = i;
				num2 = sqrMagnitude;
			}
		}
		if (num == 0)
		{
			if (Vector3.Dot(vector - FencePoints[0], FencePoints[1] - FencePoints[0]) > 0f)
			{
				return 1;
			}
			return 0;
		}
		if (num == FencePoints.Count - 1)
		{
			if (Vector3.Dot(vector - FencePoints[FencePoints.Count - 1], FencePoints[FencePoints.Count - 2] - FencePoints[FencePoints.Count - 1]) > 0f)
			{
				return FencePoints.Count - 1;
			}
			return FencePoints.Count;
		}
		Vector3 normalized = (FencePoints[num - 1] - FencePoints[num]).normalized;
		Vector3 normalized2 = (FencePoints[num + 1] - FencePoints[num]).normalized;
		Vector3 normalized3 = (vector - FencePoints[num]).normalized;
		if (Vector3.Dot(normalized3, normalized) > Vector3.Dot(normalized3, normalized2))
		{
			return num;
		}
		return num + 1;
	}

	public int GetDeletePointIdx()
	{
		Vector3 vector = m_mousePos - base.transform.position;
		int result = -1;
		float num = 10f;
		for (int i = 0; i < FencePoints.Count; i++)
		{
			float sqrMagnitude = (FencePoints[i] - vector).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				result = i;
				num = sqrMagnitude;
			}
		}
		return result;
	}
}
