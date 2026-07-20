using System.Collections.Generic;

namespace MantisLOD;

internal abstract class BinaryHeap
{
	private const int rootIndex = 1;

	private readonly List<My_Half_edge> collection;

	private int LastNodeIndex => collection.Count - 1;

	public BinaryHeap()
	{
		collection = new List<My_Half_edge>();
		collection.Add(new My_Half_edge());
	}

	public BinaryHeap(int capacity)
	{
		collection = new List<My_Half_edge>(capacity);
		collection.Add(new My_Half_edge());
	}

	public void Push(My_Half_edge item)
	{
		collection.Add(item);
		item.pqIndex = LastNodeIndex;
		BubbleUp(LastNodeIndex);
	}

	public My_Half_edge Pop()
	{
		if (LastNodeIndex == 0)
		{
			return null;
		}
		My_Half_edge result = collection[1];
		collection[1].pqIndex = -1;
		collection[1] = collection[LastNodeIndex];
		collection[1].pqIndex = 1;
		BubbleDown(1);
		collection.RemoveAt(LastNodeIndex);
		return result;
	}

	public bool Remove(int index)
	{
		if (LastNodeIndex == 0)
		{
			return false;
		}
		collection[index].pqIndex = -1;
		collection[index] = collection[LastNodeIndex];
		collection[index].pqIndex = index;
		BubbleDown(index);
		collection.RemoveAt(LastNodeIndex);
		return true;
	}

	public int Size()
	{
		return collection.Count - 1;
	}

	public My_Half_edge Top()
	{
		if (LastNodeIndex == 0)
		{
			return null;
		}
		return collection[1];
	}

	protected abstract bool Compare(My_Half_edge current, My_Half_edge other);

	private void BubbleUp(int index)
	{
		int num = index;
		int num2 = index / 2;
		My_Half_edge my_Half_edge = collection[num];
		while (num > 1 && Compare(collection[num2], my_Half_edge))
		{
			collection[num] = collection[num2];
			collection[num].pqIndex = num;
			num = num2;
			num2 /= 2;
		}
		collection[num] = my_Half_edge;
		collection[num].pqIndex = num;
	}

	private void BubbleDown(int index)
	{
		int num = index;
		int num2 = index * 2;
		My_Half_edge my_Half_edge = collection[num];
		while (num2 <= LastNodeIndex)
		{
			if (num2 < LastNodeIndex && Compare(collection[num2], collection[num2 + 1]))
			{
				num2++;
			}
			if (!Compare(my_Half_edge, collection[num2]))
			{
				break;
			}
			collection[num] = collection[num2];
			collection[num].pqIndex = num;
			num = num2;
			num2 *= 2;
		}
		collection[num] = my_Half_edge;
		collection[num].pqIndex = num;
	}
}
