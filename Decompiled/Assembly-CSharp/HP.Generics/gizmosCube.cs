using UnityEngine;

namespace HP.Generics;

public class gizmosCube : MonoBehaviour
{
	public Color GizmoColor = new Color(0f, 0.9f, 1f, 0.5f);

	public int meshType;

	public Vector3 customScale = new Vector3(1f, 1f, 1f);

	public Vector3 customPosition = new Vector3(0f, 0f, 0f);

	private void OnDrawGizmos()
	{
		Gizmos.color = GizmoColor;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.localScale);
		F_MeshType();
	}

	private void F_MeshType()
	{
		switch (meshType)
		{
		case 0:
			Gizmos.DrawCube(customPosition, customScale);
			Gizmos.DrawWireCube(customPosition, customScale);
			break;
		case 1:
			Gizmos.DrawMesh(base.gameObject.GetComponent<MeshFilter>().sharedMesh, new Vector3(0f, base.gameObject.transform.localScale.y, 0f), Quaternion.identity, customScale);
			break;
		}
	}
}
