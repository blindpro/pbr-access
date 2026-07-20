using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

public class CombineSkinnedMeshes : MonoBehaviour
{
	[ContextMenu("Combine Skinned Meshes")]
	public SkinnedMeshRenderer Combine()
	{
		SkinnedMeshRenderer[] componentsInChildren = GetComponentsInChildren<SkinnedMeshRenderer>();
		if (componentsInChildren.Length == 0)
		{
			return null;
		}
		Transform rootBone = componentsInChildren[0].rootBone;
		Transform[] componentsInChildren2 = rootBone.GetComponentsInChildren<Transform>(includeInactive: true);
		Dictionary<Transform, int> dictionary = new Dictionary<Transform, int>();
		List<Transform> list = new List<Transform>();
		Transform[] array = componentsInChildren2;
		foreach (Transform transform in array)
		{
			dictionary[transform] = list.Count;
			list.Add(transform);
		}
		Matrix4x4[] array2 = new Matrix4x4[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			Matrix4x4 matrix4x = Matrix4x4.TRS(rootBone.position, Quaternion.identity, rootBone.lossyScale);
			array2[j] = list[j].worldToLocalMatrix * matrix4x;
		}
		List<Vector3> list2 = new List<Vector3>();
		List<Vector3> list3 = new List<Vector3>();
		List<Vector4> list4 = new List<Vector4>();
		List<Vector2> list5 = new List<Vector2>();
		List<BoneWeight> list6 = new List<BoneWeight>();
		List<Material> list7 = new List<Material>();
		List<List<int>> list8 = new List<List<int>>();
		int num = 0;
		SkinnedMeshRenderer[] array3 = componentsInChildren;
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in array3)
		{
			Mesh sharedMesh = skinnedMeshRenderer.sharedMesh;
			if (sharedMesh == null)
			{
				continue;
			}
			int[] array4 = new int[skinnedMeshRenderer.bones.Length];
			for (int k = 0; k < skinnedMeshRenderer.bones.Length; k++)
			{
				Transform transform2 = skinnedMeshRenderer.bones[k];
				if (transform2 != null && dictionary.TryGetValue(transform2, out var value))
				{
					array4[k] = value;
				}
				else
				{
					array4[k] = 0;
				}
			}
			list2.AddRange(sharedMesh.vertices);
			list3.AddRange(sharedMesh.normals);
			list4.AddRange(sharedMesh.tangents);
			list5.AddRange(sharedMesh.uv);
			BoneWeight[] boneWeights = sharedMesh.boneWeights;
			for (int l = 0; l < boneWeights.Length; l++)
			{
				BoneWeight boneWeight = boneWeights[l];
				list6.Add(new BoneWeight
				{
					boneIndex0 = array4[boneWeight.boneIndex0],
					boneIndex1 = array4[boneWeight.boneIndex1],
					boneIndex2 = array4[boneWeight.boneIndex2],
					boneIndex3 = array4[boneWeight.boneIndex3],
					weight0 = boneWeight.weight0,
					weight1 = boneWeight.weight1,
					weight2 = boneWeight.weight2,
					weight3 = boneWeight.weight3
				});
			}
			for (int m = 0; m < sharedMesh.subMeshCount; m++)
			{
				list7.Add(skinnedMeshRenderer.sharedMaterials[m]);
				int[] triangles = sharedMesh.GetTriangles(m);
				while (list8.Count < list7.Count)
				{
					list8.Add(new List<int>());
				}
				for (int n = 0; n < triangles.Length; n++)
				{
					triangles[n] += num;
				}
				list8[list7.Count - 1].AddRange(triangles);
			}
			num += sharedMesh.vertexCount;
		}
		Mesh mesh = new Mesh();
		mesh.indexFormat = IndexFormat.UInt32;
		mesh.SetVertices(list2);
		mesh.SetNormals(list3);
		mesh.SetTangents(list4);
		mesh.SetUVs(0, list5);
		mesh.boneWeights = list6.ToArray();
		mesh.subMeshCount = list8.Count;
		for (int num2 = 0; num2 < list8.Count; num2++)
		{
			mesh.SetTriangles(list8[num2], num2);
		}
		mesh.bindposes = array2;
		mesh.RecalculateBounds();
		GameObject obj = new GameObject("CombinedMesh");
		obj.transform.SetParent(base.transform, worldPositionStays: false);
		SkinnedMeshRenderer skinnedMeshRenderer2 = obj.AddComponent<SkinnedMeshRenderer>();
		skinnedMeshRenderer2.sharedMesh = mesh;
		skinnedMeshRenderer2.bones = list.ToArray();
		skinnedMeshRenderer2.rootBone = rootBone;
		skinnedMeshRenderer2.materials = list7.ToArray();
		skinnedMeshRenderer2.updateWhenOffscreen = true;
		array3 = componentsInChildren;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].enabled = false;
		}
		Debug.Log("✅ COMBINE WITH CORRECT BONE REMAP");
		return skinnedMeshRenderer2;
	}

	public Mesh SimplifySkinnedMesh(Mesh sourceMesh, float quality)
	{
		quality = Mathf.Clamp01(quality);
		MeshSimplifier meshSimplifier = new MeshSimplifier();
		meshSimplifier.Initialize(sourceMesh);
		meshSimplifier.SimplificationOptions = new SimplificationOptions
		{
			PreserveBorderEdges = false,
			PreserveUVSeamEdges = false,
			PreserveUVFoldoverEdges = false,
			MaxIterationCount = 100,
			Agressiveness = 7.0
		};
		meshSimplifier.SimplifyMesh(quality);
		Mesh mesh = meshSimplifier.ToMesh();
		mesh.bindposes = sourceMesh.bindposes;
		mesh.boneWeights = ((mesh.boneWeights.Length == 0) ? sourceMesh.boneWeights : mesh.boneWeights);
		mesh.RecalculateBounds();
		return mesh;
	}

	public void RecomputeBindposes()
	{
		SkinnedMeshRenderer[] componentsInChildren = GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren)
		{
			if (skinnedMeshRenderer.sharedMesh == null || skinnedMeshRenderer.rootBone == null)
			{
				continue;
			}
			Transform[] bones = skinnedMeshRenderer.bones;
			Transform rootBone = skinnedMeshRenderer.rootBone;
			Mesh mesh = Object.Instantiate(skinnedMeshRenderer.sharedMesh);
			Matrix4x4[] array = new Matrix4x4[bones.Length];
			for (int j = 0; j < bones.Length; j++)
			{
				if (bones[j] == null)
				{
					array[j] = Matrix4x4.identity;
					continue;
				}
				Matrix4x4 matrix4x = Matrix4x4.TRS(rootBone.position, Quaternion.identity, rootBone.lossyScale);
				array[j] = bones[j].worldToLocalMatrix * matrix4x;
			}
			mesh.bindposes = array;
			skinnedMeshRenderer.sharedMesh = mesh;
			skinnedMeshRenderer.updateWhenOffscreen = true;
		}
		Debug.Log("✅ Bindposes recomputed from skeleton");
	}

	public void AddSkinnedMeshLodGroup(SkinnedMeshRenderer baseSMR, float globalMultiplier = 1f)
	{
		if (baseSMR == null)
		{
			Debug.LogWarning("No SkinnedMeshRenderer found.");
			return;
		}
		Transform parent = baseSMR.transform;
		Debug.Log("Creating LOD1");
		Mesh mesh = SimplifySkinnedMesh(baseSMR.sharedMesh, 0.4f);
		mesh.bindposes = baseSMR.sharedMesh.bindposes;
		GameObject obj = new GameObject("LOD1");
		obj.transform.SetParent(parent, worldPositionStays: false);
		SkinnedMeshRenderer skinnedMeshRenderer = obj.AddComponent<SkinnedMeshRenderer>();
		skinnedMeshRenderer.sharedMesh = mesh;
		skinnedMeshRenderer.bones = baseSMR.bones;
		skinnedMeshRenderer.rootBone = baseSMR.rootBone;
		skinnedMeshRenderer.materials = baseSMR.materials;
		Debug.Log("Creating LOD2");
		Mesh mesh2 = SimplifySkinnedMesh(baseSMR.sharedMesh, 0.1f);
		mesh2.bindposes = baseSMR.sharedMesh.bindposes;
		GameObject obj2 = new GameObject("LOD2");
		obj2.transform.SetParent(parent, worldPositionStays: false);
		SkinnedMeshRenderer skinnedMeshRenderer2 = obj2.AddComponent<SkinnedMeshRenderer>();
		skinnedMeshRenderer2.sharedMesh = mesh2;
		skinnedMeshRenderer2.bones = baseSMR.bones;
		skinnedMeshRenderer2.rootBone = baseSMR.rootBone;
		skinnedMeshRenderer2.materials = baseSMR.materials;
		LODGroup lODGroup = baseSMR.gameObject.GetComponent<LODGroup>();
		if (lODGroup == null)
		{
			lODGroup = baseSMR.gameObject.AddComponent<LODGroup>();
		}
		float num = Mathf.Max(0.01f, globalMultiplier);
		lODGroup.SetLODs(new LOD[3]
		{
			new LOD(0.6f * num, new Renderer[1] { baseSMR }),
			new LOD(0.3f * num, new Renderer[1] { skinnedMeshRenderer }),
			new LOD(0.05f * num, new Renderer[1] { skinnedMeshRenderer2 })
		});
		lODGroup.RecalculateBounds();
		Debug.Log("✅ LODGroup CREATED");
	}

	private void Start()
	{
		SkinnedMeshRenderer baseSMR = Combine();
		AddSkinnedMeshLodGroup(baseSMR);
	}
}
