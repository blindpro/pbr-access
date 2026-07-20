using System;
using System.Collections.Generic;
using UnityEngine;

namespace MantisLOD;

internal class Progressive_Mesh
{
	private readonly List<My_Half_vertex> vertices = new List<My_Half_vertex>();

	private readonly List<My_Half_face> faces = new List<My_Half_face>();

	private readonly List<My_Half_edge> edges = new List<My_Half_edge>();

	private readonly List<My_Half_trace> contract_trace = new List<My_Half_trace>();

	private readonly List<Vector3> normals = new List<Vector3>();

	private readonly List<Vector4> colors = new List<Vector4>();

	private readonly List<Vector2> uvs = new List<Vector2>();

	private int current_trace_position;

	private readonly MinHeap pq = new MinHeap();

	private Vector3 MAX;

	private Vector3 MIN;

	private float max_square_length_of_mesh;

	private int face_count;

	private int mat_count;

	private bool lock_boundary;

	private bool lock_detail;

	private bool lock_symmetry;

	private bool lock_normal;

	private bool lock_shape;

	private bool use_detail_map;

	private int detail_boost;

	public Progressive_Mesh()
	{
		lock_boundary = true;
		lock_detail = false;
		lock_symmetry = false;
		lock_normal = false;
		lock_shape = false;
		face_count = 0;
	}

	public int get_trace_num()
	{
		return contract_trace.Count;
	}

	public void create_progressive_mesh(Vector3[] vertex_array, int vertex_count, int[] triangle_array, int triangle_count, Vector3[] normal_array, int normal_count, Color[] color_array, int color_count, Vector2[] uv_array, int uv_count, int protect_boundary, int protect_detail, int protect_symmetry, int protect_normal, int protect_shape, int use_detail_map, int detail_boost)
	{
		if (contract_trace.Count == 0)
		{
			lock_boundary = protect_boundary == 1;
			lock_detail = protect_detail == 1;
			lock_symmetry = protect_symmetry == 1;
			lock_normal = protect_normal == 1;
			lock_shape = protect_shape == 1;
			this.use_detail_map = use_detail_map == 1;
			this.detail_boost = detail_boost;
			load_mesh_from_array(vertex_array, vertex_count, triangle_array, triangle_count, normal_array, normal_count, color_array, color_count, uv_array, uv_count);
			calculate_cost_of_edges();
			contract_edges();
			trace_to(0);
		}
	}

	public void get_triangle_list(int goal, int[] triangle_array, ref int triangle_count)
	{
		if (contract_trace.Count == 0)
		{
			triangle_count = 0;
			return;
		}
		goal = Math.Max(Math.Min(goal, contract_trace.Count), 0);
		goal = trace_to(goal);
		int num = contract_trace.Count;
		List<List<int>> list = new List<List<int>>(mat_count);
		for (int i = 0; i < mat_count; i++)
		{
			list.Add(new List<int>());
		}
		int num2 = contract_trace.Count - 1;
		while (num2 >= 0 && num != goal)
		{
			foreach (My_Half_face erased_face in contract_trace[num2].erased_faces)
			{
				list[erased_face.edge.face.mat].Add(erased_face.edge.index);
				list[erased_face.edge.next.face.mat].Add(erased_face.edge.next.index);
				list[erased_face.edge.next.next.face.mat].Add(erased_face.edge.next.next.index);
			}
			num--;
			num2--;
		}
		num = 0;
		for (int j = 0; j < mat_count; j++)
		{
			int num3 = (triangle_array[num] = list[j].Count);
			num++;
			if (num3 > 0)
			{
				list[j].CopyTo(triangle_array, num);
				num += num3;
			}
		}
		triangle_count = num;
	}

	private void load_mesh_from_array(Vector3[] vertex_array, int vertex_count, int[] triangle_array, int triangle_count, Vector3[] normal_array, int normal_count, Color[] color_array, int color_count, Vector2[] uv_array, int uv_count)
	{
		Dictionary<Vector3, int> dictionary = new Dictionary<Vector3, int>(new Vector3Comparer());
		List<int> list = new List<int>();
		for (int i = 0; i < vertex_count; i++)
		{
			if (!dictionary.ContainsKey(vertex_array[i]))
			{
				My_Half_vertex my_Half_vertex = new My_Half_vertex
				{
					position = vertex_array[i]
				};
				if (my_Half_vertex.position.x > MAX.x)
				{
					MAX.x = my_Half_vertex.position.x;
				}
				if (my_Half_vertex.position.y > MAX.y)
				{
					MAX.y = my_Half_vertex.position.y;
				}
				if (my_Half_vertex.position.z > MAX.z)
				{
					MAX.z = my_Half_vertex.position.z;
				}
				if (my_Half_vertex.position.x < MIN.x)
				{
					MIN.x = my_Half_vertex.position.x;
				}
				if (my_Half_vertex.position.y < MIN.y)
				{
					MIN.y = my_Half_vertex.position.y;
				}
				if (my_Half_vertex.position.z < MIN.z)
				{
					MIN.z = my_Half_vertex.position.z;
				}
				int count = vertices.Count;
				dictionary.Add(vertex_array[i], count);
				list.Add(count);
				vertices.Add(my_Half_vertex);
			}
			else
			{
				list.Add(dictionary[vertex_array[i]]);
			}
		}
		int num = 0;
		int num2 = 0;
		while (num2 < triangle_count)
		{
			int num3 = triangle_array[num2];
			num2++;
			for (int j = 0; j < num3; j += 3)
			{
				int num4 = list[triangle_array[num2 + j]];
				int num5 = list[triangle_array[num2 + j + 1]];
				int num6 = list[triangle_array[num2 + j + 2]];
				if (num4 != num5 && num5 != num6 && num6 != num4)
				{
					My_Half_face my_Half_face = new My_Half_face();
					My_Half_edge[] array = new My_Half_edge[3]
					{
						new My_Half_edge(),
						new My_Half_edge(),
						new My_Half_edge()
					};
					for (int k = 0; k < 3; k++)
					{
						array[k].next = array[(k + 1) % 3];
						array[k].face = my_Half_face;
						int index = list[triangle_array[num2 + j + k]];
						array[k].vertex = vertices[index];
						array[k].index = triangle_array[num2 + j + k];
						vertices[index].edges.Add(array[k]);
						edges.Add(array[k]);
					}
					my_Half_face.edge = array[0];
					my_Half_face.mat = num;
					faces.Add(my_Half_face);
				}
			}
			num2 += num3;
			num++;
		}
		mat_count = num;
		for (int l = 0; l < normal_count; l++)
		{
			Vector3 item = normal_array[l];
			normals.Add(item);
		}
		for (int m = 0; m < color_count; m++)
		{
			Vector4 item2 = color_array[m];
			colors.Add(item2);
		}
		for (int n = 0; n < uv_count; n++)
		{
			Vector2 item3 = uv_array[n];
			uvs.Add(item3);
		}
		max_square_length_of_mesh = (MAX - MIN).sqrMagnitude;
		face_count = faces.Count;
	}

	private void calculate_face_normal(My_Half_face one_face)
	{
		one_face.n = Vector3.Cross(one_face.edge.next.vertex.position - one_face.edge.vertex.position, one_face.edge.next.next.vertex.position - one_face.edge.vertex.position);
		one_face.n.Normalize();
	}

	private void calculate_face_normals()
	{
		int num = 0;
		foreach (My_Half_face face in faces)
		{
			calculate_face_normal(face);
			num++;
		}
	}

	private bool is_boundary_edge(My_Half_edge edge)
	{
		My_Half_vertex vertex = edge.vertex;
		My_Half_vertex vertex2 = edge.next.vertex;
		int num = 0;
		foreach (My_Half_edge edge2 in vertex.edges)
		{
			foreach (My_Half_edge edge3 in vertex2.edges)
			{
				if (edge2.face == edge3.face)
				{
					num++;
					break;
				}
			}
		}
		return num == 1;
	}

	private void detect_and_mark_boundaries()
	{
		int num = 0;
		foreach (My_Half_edge edge in edges)
		{
			if (is_boundary_edge(edge))
			{
				edge.vertex.on_boundary = true;
				edge.next.vertex.on_boundary = true;
				num++;
			}
		}
	}

	private bool is_symmetry_edge(My_Half_edge edge)
	{
		My_Half_vertex vertex = edge.vertex;
		My_Half_vertex vertex2 = edge.next.vertex;
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		foreach (My_Half_edge edge2 in vertex.edges)
		{
			if (edge2.next.vertex == vertex2)
			{
				list.Add(edge2.next.next.index);
			}
		}
		foreach (My_Half_edge edge3 in vertex2.edges)
		{
			if (edge3.next.vertex == vertex)
			{
				list2.Add(edge3.next.next.index);
			}
		}
		if (list.Count != list2.Count)
		{
			return false;
		}
		bool flag = false;
		foreach (int item in list)
		{
			if (flag)
			{
				continue;
			}
			bool flag2 = false;
			foreach (int item2 in list2)
			{
				if (item != item2 && uvs.Count > 0 && uvs[item] == uvs[item2])
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				flag = true;
			}
		}
		return !flag;
	}

	private void detect_and_mark_symmetries()
	{
		int num = 0;
		foreach (My_Half_edge edge in edges)
		{
			if (is_symmetry_edge(edge))
			{
				edge.vertex.on_symmetry = true;
				edge.next.vertex.on_symmetry = true;
				num++;
			}
		}
	}

	private float cost_of_edge(My_Half_edge edge)
	{
		My_Half_vertex vertex = edge.vertex;
		My_Half_vertex vertex2 = edge.next.vertex;
		float sqrMagnitude = (vertex2.position - vertex.position).sqrMagnitude;
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		List<My_Half_face> list3 = new List<My_Half_face>();
		foreach (My_Half_edge edge2 in vertex.edges)
		{
			foreach (My_Half_edge edge3 in vertex2.edges)
			{
				if (edge2.face == edge3.face)
				{
					list.Add(edge2.index);
					list2.Add(edge2.face.mat);
					list3.Add(edge2.face);
					break;
				}
			}
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		float num = float.MinValue;
		float num2 = 0f;
		foreach (My_Half_edge edge4 in vertex.edges)
		{
			bool flag5 = false;
			float num3 = float.MaxValue;
			foreach (My_Half_face item in list3)
			{
				float num4 = (1f - Vector3.Dot(edge4.face.n, item.n)) * 0.5f;
				if (num4 < num3)
				{
					num3 = num4;
				}
				if (edge4.face == item)
				{
					flag5 = true;
				}
			}
			if (num3 > num)
			{
				num = num3;
			}
			if (flag5)
			{
				continue;
			}
			if (lock_shape)
			{
				My_Half_vertex my_Half_vertex = vertex2;
				My_Half_vertex vertex3 = edge4.next.vertex;
				My_Half_vertex vertex4 = edge4.next.next.vertex;
				Vector3 normalized = (vertex3.position - my_Half_vertex.position).normalized;
				Vector3 normalized2 = (vertex4.position - vertex3.position).normalized;
				Vector3 normalized3 = (my_Half_vertex.position - vertex4.position).normalized;
				float val = Vector3.Dot(normalized3, normalized);
				float val2 = Vector3.Dot(normalized, normalized2);
				float val3 = Vector3.Dot(normalized2, normalized3);
				float num5 = Math.Min(val, Math.Min(val2, val3));
				float num6 = (Math.Max(val, Math.Max(val2, val3)) - num5) * 0.5f;
				if (num6 > num2)
				{
					num2 = num6;
				}
			}
			if (lock_normal && !flag)
			{
				bool flag6 = false;
				foreach (int item2 in list)
				{
					if (normals.Count == 0 || normals[edge4.index] == normals[item2])
					{
						flag6 = true;
						break;
					}
				}
				if (!flag6)
				{
					flag = true;
				}
			}
			if (!use_detail_map && !flag2)
			{
				bool flag7 = false;
				foreach (int item3 in list)
				{
					if (colors.Count == 0 || colors[edge4.index] == colors[item3])
					{
						flag7 = true;
						break;
					}
				}
				if (!flag7)
				{
					flag2 = true;
				}
			}
			if (!flag3)
			{
				bool flag8 = false;
				foreach (int item4 in list)
				{
					if (uvs.Count == 0 || uvs[edge4.index] == uvs[item4])
					{
						flag8 = true;
						break;
					}
				}
				if (!flag8)
				{
					flag3 = true;
				}
			}
			if (flag4)
			{
				continue;
			}
			bool flag9 = false;
			foreach (int item5 in list2)
			{
				if (edge4.face.mat == item5)
				{
					flag9 = true;
					break;
				}
			}
			if (!flag9)
			{
				flag4 = true;
			}
		}
		float num7 = (flag ? max_square_length_of_mesh : 0f);
		float num8 = (flag2 ? max_square_length_of_mesh : 0f);
		float num9 = (flag3 ? max_square_length_of_mesh : 0f);
		float num10 = (flag4 ? max_square_length_of_mesh : 0f);
		float num11 = 0f;
		if (lock_symmetry)
		{
			num11 = ((vertex.on_symmetry && !vertex2.on_symmetry) ? max_square_length_of_mesh : 0f);
		}
		float num12 = 0f;
		float num13 = 0f;
		if (lock_boundary)
		{
			if (vertex.on_boundary || vertex2.on_boundary)
			{
				num12 = max_square_length_of_mesh;
			}
		}
		else if (vertex.on_boundary)
		{
			if (vertex2.on_boundary)
			{
				foreach (My_Half_edge edge5 in vertex.edges)
				{
					if (is_boundary_edge(edge5.next.next))
					{
						Vector3 position = edge5.next.next.vertex.position;
						Vector3 position2 = vertex.position;
						Vector3 position3 = vertex2.position;
						float num14 = (1f - Vector3.Dot((position2 - position).normalized, (position3 - position2).normalized)) * 0.5f;
						if (num14 > num13)
						{
							num13 = num14;
						}
					}
				}
			}
			else
			{
				num12 = max_square_length_of_mesh;
			}
		}
		if (lock_detail)
		{
			num *= num;
		}
		double num15 = ((use_detail_map && colors.Count > 0) ? (1.0 + (double)((float)detail_boost * colors[edge.index].x)) : 1.0);
		if (num < 1E-06f)
		{
			num = 1E-06f;
		}
		return (float)((double)sqrMagnitude * num15 * (((double)num * 20.0 + (double)num13 * 20.0 + (double)num2 * 1.0) / 41.0) + (double)num12 + (double)num7 + (double)num8 + (double)num9 + (double)num11 + (double)num10);
	}

	private void calculate_cost_of_edges()
	{
		calculate_face_normals();
		detect_and_mark_boundaries();
		detect_and_mark_symmetries();
		foreach (My_Half_edge edge in edges)
		{
			edge.cost = cost_of_edge(edge);
			pq.Push(edge);
		}
	}

	private bool contract_edge(My_Half_edge edge)
	{
		My_Half_vertex vertex = edge.vertex;
		My_Half_vertex vertex2 = edge.next.vertex;
		List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
		List<My_Half_face> list2 = new List<My_Half_face>();
		foreach (My_Half_edge edge3 in vertex.edges)
		{
			foreach (My_Half_edge edge4 in vertex2.edges)
			{
				if (edge3.face == edge4.face)
				{
					if (edge3.next.vertex == vertex2)
					{
						list.Add(new KeyValuePair<int, int>(edge3.index, edge3.next.index));
					}
					else if (edge3.next.next.vertex == vertex2)
					{
						list.Add(new KeyValuePair<int, int>(edge3.index, edge3.next.next.index));
					}
					list2.Add(edge3.face);
					break;
				}
			}
		}
		My_Half_trace my_Half_trace = new My_Half_trace();
		List<My_Half_edge> list3 = new List<My_Half_edge>();
		foreach (My_Half_edge edge5 in vertex.edges)
		{
			bool flag = false;
			foreach (My_Half_face item2 in list2)
			{
				if (edge5.face == item2)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			edge5.vertex = vertex2;
			int value = list[list.Count - 1].Value;
			foreach (KeyValuePair<int, int> item3 in list)
			{
				if (uvs.Count == 0 || uvs[edge5.index] == uvs[item3.Key])
				{
					value = item3.Value;
					break;
				}
			}
			My_Half_edge_index item = new My_Half_edge_index
			{
				edge = edge5,
				index_from = edge5.index,
				index_to = value
			};
			my_Half_trace.updated_edge_indices.Add(item);
			edge5.index = value;
			list3.Add(edge5);
		}
		foreach (My_Half_edge edge6 in vertex2.edges)
		{
			bool flag2 = false;
			foreach (My_Half_face item4 in list2)
			{
				if (edge6.face == item4)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				list3.Add(edge6);
			}
		}
		vertex2.edges = list3;
		foreach (My_Half_edge edge7 in vertex2.edges)
		{
			edge7.cost = cost_of_edge(edge7);
			edge7.next.cost = cost_of_edge(edge7.next);
			edge7.next.next.cost = cost_of_edge(edge7.next.next);
			pq.Remove(edge7.pqIndex);
			pq.Push(edge7);
			pq.Remove(edge7.next.pqIndex);
			pq.Push(edge7.next);
			pq.Remove(edge7.next.next.pqIndex);
			pq.Push(edge7.next.next);
			calculate_face_normal(edge7.face);
		}
		foreach (My_Half_face item5 in list2)
		{
			item5.alive = false;
			face_count--;
			my_Half_trace.erased_faces.Add(item5);
			item5.edge.alive = false;
			item5.edge.next.alive = false;
			item5.edge.next.next.alive = false;
			pq.Remove(item5.edge.pqIndex);
			pq.Remove(item5.edge.next.pqIndex);
			pq.Remove(item5.edge.next.next.pqIndex);
			My_Half_edge edge2 = item5.edge;
			My_Half_edge my_Half_edge = edge2;
			do
			{
				if (my_Half_edge.vertex != vertex && my_Half_edge.vertex != vertex2)
				{
					my_Half_edge.vertex.edges.Remove(my_Half_edge);
					break;
				}
				my_Half_edge = my_Half_edge.next;
			}
			while (my_Half_edge != edge2);
		}
		vertex.alive = false;
		my_Half_trace.v_from = vertex;
		my_Half_trace.v_to = vertex2;
		if (edge.cost >= max_square_length_of_mesh)
		{
			my_Half_trace.safe = false;
		}
		contract_trace.Add(my_Half_trace);
		return true;
	}

	private void contract_edges()
	{
		int num = face_count;
		while (face_count > 0 && pq.Top() != null)
		{
			contract_edge(pq.Top());
			if (num > face_count + 2500)
			{
				num = face_count;
			}
		}
		current_trace_position = contract_trace.Count;
	}

	private int trace_to(int goal)
	{
		while (current_trace_position != goal)
		{
			if (current_trace_position > goal)
			{
				current_trace_position--;
				foreach (My_Half_edge_index updated_edge_index in contract_trace[current_trace_position].updated_edge_indices)
				{
					updated_edge_index.edge.vertex = contract_trace[current_trace_position].v_from;
					updated_edge_index.edge.index = updated_edge_index.index_from;
				}
				continue;
			}
			if (!contract_trace[current_trace_position].safe)
			{
				break;
			}
			foreach (My_Half_edge_index updated_edge_index2 in contract_trace[current_trace_position].updated_edge_indices)
			{
				updated_edge_index2.edge.vertex = contract_trace[current_trace_position].v_to;
				updated_edge_index2.edge.index = updated_edge_index2.index_to;
			}
			current_trace_position++;
		}
		return current_trace_position;
	}
}
