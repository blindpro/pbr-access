namespace MantisLOD;

internal class MinHeap : BinaryHeap
{
	public MinHeap()
	{
	}

	public MinHeap(int capacity)
		: base(capacity)
	{
	}

	protected override bool Compare(My_Half_edge current, My_Half_edge other)
	{
		return other.CompareTo(current) < 0;
	}
}
