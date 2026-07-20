using System.Collections.Generic;

namespace MantisLOD;

internal class My_Half_trace
{
	public bool safe;

	public My_Half_vertex v_from;

	public My_Half_vertex v_to;

	public List<My_Half_face> erased_faces = new List<My_Half_face>();

	public List<My_Half_edge_index> updated_edge_indices = new List<My_Half_edge_index>();

	public My_Half_trace()
	{
		safe = true;
	}
}
