using System;

namespace MantisLOD;

internal class My_Half_edge : IComparable
{
	public bool alive;

	public int pqIndex;

	public My_Half_vertex vertex;

	public int index;

	public My_Half_face face;

	public My_Half_edge next;

	public float cost;

	public My_Half_edge()
	{
		alive = true;
	}

	public int CompareTo(object obj)
	{
		return cost.CompareTo((obj as My_Half_edge).cost);
	}
}
