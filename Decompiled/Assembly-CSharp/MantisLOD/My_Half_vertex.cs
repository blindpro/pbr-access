using System.Collections.Generic;
using UnityEngine;

namespace MantisLOD;

internal class My_Half_vertex
{
	public bool alive;

	public bool on_boundary;

	public bool on_symmetry;

	public Vector3 position;

	public List<My_Half_edge> edges = new List<My_Half_edge>();

	public My_Half_vertex()
	{
		alive = true;
		on_boundary = false;
		on_symmetry = false;
	}
}
