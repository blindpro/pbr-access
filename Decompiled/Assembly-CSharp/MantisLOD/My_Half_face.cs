using UnityEngine;

namespace MantisLOD;

internal class My_Half_face
{
	public bool alive;

	public int mat;

	public My_Half_edge edge;

	public Vector3 n;

	public My_Half_face()
	{
		alive = true;
	}
}
