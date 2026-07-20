using System.Collections.Generic;
using UnityEngine;

namespace MantisLOD;

public static class MantisLODSimpler
{
	private static readonly List<Progressive_Mesh> simplers = new List<Progressive_Mesh>();

	public static int create_progressive_mesh(Vector3[] vertex_array, int vertex_count, int[] triangle_array, int triangle_count, Vector3[] normal_array, int normal_count, Color[] color_array, int color_count, Vector2[] uv_array, int uv_count, int protect_boundary, int protect_detail, int protect_symmetry, int protect_normal, int protect_shape, int use_detail_map, int detail_boost)
	{
		bool flag = false;
		int num = -1;
		for (int i = 0; i < simplers.Count; i++)
		{
			if (simplers[i] == null)
			{
				flag = true;
				simplers[i] = new Progressive_Mesh();
				num = i;
				break;
			}
		}
		if (!flag)
		{
			simplers.Add(new Progressive_Mesh());
			num = simplers.Count - 1;
		}
		simplers[num].create_progressive_mesh(vertex_array, vertex_count, triangle_array, triangle_count, normal_array, normal_count, color_array, color_count, uv_array, uv_count, protect_boundary, protect_detail, protect_symmetry, protect_normal, protect_shape, use_detail_map, detail_boost);
		return num;
	}

	public static int get_triangle_list(int index, float goal, int[] triangle_array, ref int triangle_count)
	{
		if (index >= 0 && index < simplers.Count && simplers[index] != null)
		{
			Progressive_Mesh progressive_Mesh = simplers[index];
			int goal2 = (int)((float)progressive_Mesh.get_trace_num() * (1f - goal * 0.01f) + 0.5f);
			progressive_Mesh.get_triangle_list(goal2, triangle_array, ref triangle_count);
			return 1;
		}
		return 0;
	}

	public static int delete_progressive_mesh(int index)
	{
		if (index >= 0 && index < simplers.Count && simplers[index] != null)
		{
			simplers[index] = null;
			return 1;
		}
		return 0;
	}
}
