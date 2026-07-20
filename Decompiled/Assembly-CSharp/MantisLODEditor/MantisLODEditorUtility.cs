using System;
using System.Collections.Generic;
using MantisLOD;
using UnityEngine;

namespace MantisLODEditor;

public static class MantisLODEditorUtility
{
	public static string get_uuid_from_mesh(Mesh mesh)
	{
		string text = mesh.name + "_" + mesh.vertexCount + "_" + mesh.subMeshCount;
		for (int i = 0; i < mesh.subMeshCount; i++)
		{
			text = text + "_" + mesh.GetIndexCount(i);
		}
		return text;
	}

	public static int PrepareSimplify(Mantis_Mesh[] Mantis_Meshes, bool use_origin_triangles = true)
	{
		int num = 0;
		foreach (Mantis_Mesh mantis_Mesh in Mantis_Meshes)
		{
			int num2 = mantis_Mesh.mesh.triangles.Length;
			mantis_Mesh.out_triangles = new int[num2 + mantis_Mesh.mesh.subMeshCount];
			if (use_origin_triangles)
			{
				mantis_Mesh.origin_triangles = new int[mantis_Mesh.mesh.subMeshCount][];
				for (int j = 0; j < mantis_Mesh.mesh.subMeshCount; j++)
				{
					int[] triangles = mantis_Mesh.mesh.GetTriangles(j);
					num += triangles.Length / 3;
					mantis_Mesh.origin_triangles[j] = new int[triangles.Length];
					Array.Copy(triangles, mantis_Mesh.origin_triangles[j], triangles.Length);
				}
			}
			mantis_Mesh.index = -1;
			mantis_Mesh.uuid = get_uuid_from_mesh(mantis_Mesh.mesh);
		}
		return num;
	}

	public static void Simplify(Mantis_Mesh[] Mantis_Meshes, bool protect_boundary, bool protect_detail, bool protect_symmetry, bool protect_normal, bool protect_shape, bool use_detail_map, int detail_boost)
	{
		foreach (Mantis_Mesh mantis_Mesh in Mantis_Meshes)
		{
			int num = mantis_Mesh.mesh.triangles.Length;
			Vector3[] vertices = mantis_Mesh.mesh.vertices;
			int[] array = new int[num + mantis_Mesh.mesh.subMeshCount];
			Vector3[] normals = mantis_Mesh.mesh.normals;
			Color[] colors = mantis_Mesh.mesh.colors;
			Vector2[] uv = mantis_Mesh.mesh.uv;
			int num2 = 0;
			for (int j = 0; j < mantis_Mesh.mesh.subMeshCount; j++)
			{
				int[] triangles = mantis_Mesh.mesh.GetTriangles(j);
				array[num2] = triangles.Length;
				num2++;
				Array.Copy(triangles, 0, array, num2, triangles.Length);
				num2 += triangles.Length;
			}
			mantis_Mesh.index = MantisLODSimpler.create_progressive_mesh(vertices, vertices.Length, array, num2, normals, normals.Length, colors, colors.Length, uv, uv.Length, protect_boundary ? 1 : 0, protect_detail ? 1 : 0, protect_symmetry ? 1 : 0, protect_normal ? 1 : 0, protect_shape ? 1 : 0, use_detail_map ? 1 : 0, detail_boost);
		}
	}

	public static int SetQuality(Mantis_Mesh[] Mantis_Meshes, float quality)
	{
		int num = 0;
		foreach (Mantis_Mesh mantis_Mesh in Mantis_Meshes)
		{
			if (mantis_Mesh.index == -1 || MantisLODSimpler.get_triangle_list(mantis_Mesh.index, quality, mantis_Mesh.out_triangles, ref mantis_Mesh.out_count) != 1 || mantis_Mesh.out_count <= 0)
			{
				continue;
			}
			int num2 = 0;
			int num3 = 0;
			while (num2 < mantis_Mesh.out_count)
			{
				int num4 = mantis_Mesh.out_triangles[num2];
				num2++;
				if (num4 > 0)
				{
					int[] array = new int[num4];
					Array.Copy(mantis_Mesh.out_triangles, num2, array, 0, num4);
					mantis_Mesh.mesh.SetTriangles(array, num3);
					num2 += num4;
				}
				else
				{
					mantis_Mesh.mesh.SetTriangles((int[])null, num3);
				}
				num3++;
			}
			num += mantis_Mesh.mesh.triangles.Length / 3;
		}
		return num;
	}

	public static void SaveSimplifiedMesh(Mesh mesh, string filePath)
	{
	}

	public static void FinishSimplify(Mantis_Mesh[] Mantis_Meshes, bool use_origin_triangles = true, bool unload_asset = false)
	{
		if (Mantis_Meshes == null)
		{
			return;
		}
		Mantis_Mesh[] array = Mantis_Meshes;
		foreach (Mantis_Mesh mantis_Mesh in array)
		{
			if (mantis_Mesh.index == -1)
			{
				continue;
			}
			if (use_origin_triangles)
			{
				for (int j = 0; j < mantis_Mesh.mesh.subMeshCount; j++)
				{
					mantis_Mesh.mesh.SetTriangles(mantis_Mesh.origin_triangles[j], j);
				}
			}
			if (unload_asset)
			{
				Resources.UnloadAsset(mantis_Mesh.mesh);
			}
			MantisLODSimpler.delete_progressive_mesh(mantis_Mesh.index);
			mantis_Mesh.index = -1;
		}
		Mantis_Meshes = null;
	}

	public static ProgressiveMesh MakeProgressiveMesh(Mantis_Mesh[] Mantis_Meshes, int max_lod_count)
	{
		ProgressiveMesh progressiveMesh = (ProgressiveMesh)ScriptableObject.CreateInstance(typeof(ProgressiveMesh));
		int num = 0;
		int[][][][] array = new int[max_lod_count][][][];
		num++;
		for (int i = 0; i < array.Length; i++)
		{
			float goal = 100f * (float)(array.Length - i) / (float)array.Length;
			array[i] = new int[Mantis_Meshes.Length][][];
			num++;
			int num2 = 0;
			progressiveMesh.uuids = new string[Mantis_Meshes.Length];
			foreach (Mantis_Mesh mantis_Mesh in Mantis_Meshes)
			{
				if (mantis_Mesh.index != -1 && MantisLODSimpler.get_triangle_list(mantis_Mesh.index, goal, mantis_Mesh.out_triangles, ref mantis_Mesh.out_count) == 1)
				{
					if (mantis_Mesh.out_count > 0)
					{
						int num3 = 0;
						int num4 = 0;
						array[i][num2] = new int[mantis_Mesh.mesh.subMeshCount][];
						num++;
						while (num3 < mantis_Mesh.out_count)
						{
							int num5 = mantis_Mesh.out_triangles[num3];
							num++;
							num += num5;
							num3++;
							int[] array2 = new int[num5];
							Array.Copy(mantis_Mesh.out_triangles, num3, array2, 0, num5);
							array[i][num2][num4] = array2;
							num3 += num5;
							num4++;
						}
					}
					else
					{
						array[i][num2] = new int[mantis_Mesh.mesh.subMeshCount][];
						num++;
						for (int k = 0; k < array[i][num2].Length; k++)
						{
							array[i][num2][k] = new int[0];
							num++;
						}
					}
				}
				progressiveMesh.uuids[num2] = mantis_Mesh.uuid;
				num2++;
			}
		}
		progressiveMesh.triangles = new int[num];
		num = 0;
		progressiveMesh.triangles[num] = array.Length;
		num++;
		for (int l = 0; l < array.Length; l++)
		{
			progressiveMesh.triangles[num] = array[l].Length;
			num++;
			for (int m = 0; m < array[l].Length; m++)
			{
				progressiveMesh.triangles[num] = array[l][m].Length;
				num++;
				for (int n = 0; n < array[l][m].Length; n++)
				{
					progressiveMesh.triangles[num] = array[l][m][n].Length;
					num++;
					Array.Copy(array[l][m][n], 0, progressiveMesh.triangles, num, array[l][m][n].Length);
					num += array[l][m][n].Length;
				}
			}
		}
		return progressiveMesh;
	}

	public static void SaveProgressiveMesh(ProgressiveMesh pm, string filePath)
	{
	}

	public static ProgressiveMesh LoadProgressiveMesh(string filePath)
	{
		return Resources.Load<ProgressiveMesh>(filePath);
	}

	public static int get_triangles_count_from_progressive_mesh(ProgressiveMesh pm, int _lod, int _mesh_count)
	{
		int num = 0;
		int num2 = 0;
		int num3 = pm.triangles[num2];
		num2++;
		for (int i = 0; i < num3; i++)
		{
			int num4 = pm.triangles[num2];
			num2++;
			for (int j = 0; j < num4; j++)
			{
				int num5 = pm.triangles[num2];
				num2++;
				for (int k = 0; k < num5; k++)
				{
					int num6 = pm.triangles[num2];
					num2++;
					if (i == _lod && j == _mesh_count)
					{
						num += num6;
					}
					num2 += num6;
				}
			}
		}
		return num / 3;
	}

	private static int[] get_triangles_from_progressive_mesh(ProgressiveMesh pm, int _lod, int _mesh_count, int _mat)
	{
		int num = 0;
		int num2 = pm.triangles[num];
		num++;
		for (int i = 0; i < num2; i++)
		{
			int num3 = pm.triangles[num];
			num++;
			for (int j = 0; j < num3; j++)
			{
				int num4 = pm.triangles[num];
				num++;
				for (int k = 0; k < num4; k++)
				{
					int num5 = pm.triangles[num];
					num++;
					if (i == _lod && j == _mesh_count && k == _mat)
					{
						int[] array = new int[num5];
						Array.Copy(pm.triangles, num, array, 0, num5);
						return array;
					}
					num += num5;
				}
			}
		}
		return null;
	}

	private static void set_triangles(ProgressiveMesh pm, Mesh mesh, string uuid, int lod)
	{
		int num = Array.IndexOf(pm.uuids, uuid);
		if (num != -1)
		{
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				int[] triangles = get_triangles_from_progressive_mesh(pm, lod, num, i);
				mesh.SetTriangles(triangles, i);
			}
		}
	}

	public static void GenerateRuntimeLOD(ProgressiveMesh pm, Component[] containers, bool optimize_on_the_fly)
	{
		if (pm == null)
		{
			return;
		}
		if (pm.lod_meshes_dic == null)
		{
			pm.lod_meshes_dic = new Dictionary<string, Lod_Mesh_Table>();
		}
		int num = pm.triangles[0];
		foreach (Component component in containers)
		{
			Mesh mesh = null;
			if (component is MeshFilter)
			{
				mesh = ((MeshFilter)component).sharedMesh;
			}
			else if (component is SkinnedMeshRenderer)
			{
				mesh = ((SkinnedMeshRenderer)component).sharedMesh;
			}
			if (mesh == null)
			{
				continue;
			}
			string text = get_uuid_from_mesh(mesh);
			if (!pm.lod_meshes_dic.ContainsKey(text))
			{
				if (Array.IndexOf(pm.uuids, text) == -1)
				{
					continue;
				}
				Lod_Mesh_Table lod_Mesh_Table = new Lod_Mesh_Table();
				lod_Mesh_Table.containers = new List<Component>();
				lod_Mesh_Table.containers.Add(component);
				lod_Mesh_Table.origin_mesh = mesh;
				lod_Mesh_Table.lod_meshes = new Lod_Mesh[num];
				for (int j = 0; j < num; j++)
				{
					lod_Mesh_Table.lod_meshes[j] = new Lod_Mesh();
					lod_Mesh_Table.lod_meshes[j].mesh = UnityEngine.Object.Instantiate(mesh);
					set_triangles(pm, lod_Mesh_Table.lod_meshes[j].mesh, text, j);
					if (optimize_on_the_fly)
					{
						lod_Mesh_Table.lod_meshes[j].mesh.Optimize();
					}
					lod_Mesh_Table.lod_meshes[j].triangle_count = lod_Mesh_Table.lod_meshes[j].mesh.triangles.Length;
				}
				pm.lod_meshes_dic.Add(text, lod_Mesh_Table);
			}
			else
			{
				pm.lod_meshes_dic[text].containers.Add(component);
			}
		}
	}

	public static int SwitchRuntimeLOD(ProgressiveMesh pm, int[] mesh_lod_range, int lod, Component[] Components)
	{
		int num = 0;
		if (pm == null)
		{
			return num;
		}
		if (pm.lod_meshes_dic == null)
		{
			return num;
		}
		foreach (KeyValuePair<string, Lod_Mesh_Table> item in pm.lod_meshes_dic)
		{
			int num2 = Array.IndexOf(pm.uuids, item.Key);
			if (num2 == -1)
			{
				continue;
			}
			int num3 = lod;
			if (num3 < mesh_lod_range[num2 * 2])
			{
				num3 = mesh_lod_range[num2 * 2];
			}
			if (num3 > mesh_lod_range[num2 * 2 + 1])
			{
				num3 = mesh_lod_range[num2 * 2 + 1];
			}
			foreach (Component container in item.Value.containers)
			{
				foreach (Component component in Components)
				{
					if (container == component)
					{
						if (container is MeshFilter)
						{
							((MeshFilter)container).sharedMesh = item.Value.lod_meshes[num3].mesh;
						}
						else
						{
							((SkinnedMeshRenderer)container).sharedMesh = item.Value.lod_meshes[num3].mesh;
						}
						num += item.Value.lod_meshes[num3].triangle_count;
					}
				}
			}
		}
		return num;
	}

	public static void FinishRuntimeLOD(ProgressiveMesh pm)
	{
		if (pm == null || pm.lod_meshes_dic == null)
		{
			return;
		}
		foreach (Lod_Mesh_Table value in pm.lod_meshes_dic.Values)
		{
			foreach (Component container in value.containers)
			{
				if (container is MeshFilter)
				{
					((MeshFilter)container).sharedMesh = value.origin_mesh;
				}
				else
				{
					((SkinnedMeshRenderer)container).sharedMesh = value.origin_mesh;
				}
			}
		}
		pm.lod_meshes_dic = null;
	}
}
