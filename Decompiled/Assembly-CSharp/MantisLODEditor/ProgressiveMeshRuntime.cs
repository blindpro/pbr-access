using System;
using UnityEngine;
using UnityEngine.UI;

namespace MantisLODEditor;

public class ProgressiveMeshRuntime : MonoBehaviour
{
	public ProgressiveMesh progressiveMesh;

	public Text fpsHint;

	public Text lodHint;

	public Text triangleHint;

	[HideInInspector]
	public bool optimize_on_the_fly = true;

	[HideInInspector]
	public int[] mesh_lod_range;

	[HideInInspector]
	public bool never_cull = true;

	[HideInInspector]
	public int lod_strategy = 1;

	[HideInInspector]
	public float cull_ratio = 0.1f;

	[HideInInspector]
	public float disappear_distance = 250f;

	[HideInInspector]
	public float updateInterval = 0.25f;

	private int current_lod = -1;

	private Component[] allBasicRenderers;

	private float currentTimeToInterval;

	private bool culled;

	private bool working;

	private void Awake()
	{
		get_all_meshes();
	}

	private void Start()
	{
	}

	private float ratio_of_screen()
	{
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		Component[] array = allBasicRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			Renderer obj = (Renderer)array[i];
			Vector3 center = obj.bounds.center;
			float magnitude = obj.bounds.extents.magnitude;
			Vector3[] array2 = new Vector3[6]
			{
				Camera.main.WorldToScreenPoint(new Vector3(center.x - magnitude, center.y, center.z)),
				Camera.main.WorldToScreenPoint(new Vector3(center.x + magnitude, center.y, center.z)),
				Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y - magnitude, center.z)),
				Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y + magnitude, center.z)),
				Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y, center.z - magnitude)),
				Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y, center.z + magnitude))
			};
			for (int j = 0; j < array2.Length; j++)
			{
				Vector3 vector3 = array2[j];
				if (vector3.x < vector.x)
				{
					vector.x = vector3.x;
				}
				if (vector3.y < vector.y)
				{
					vector.y = vector3.y;
				}
				if (vector3.x > vector2.x)
				{
					vector2.x = vector3.x;
				}
				if (vector3.y > vector2.y)
				{
					vector2.y = vector3.y;
				}
			}
		}
		float num = (vector2.x - vector.x) / (float)Camera.main.pixelWidth;
		float num2 = (vector2.y - vector.y) / (float)Camera.main.pixelHeight;
		float num3 = ((num > num2) ? num : num2);
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		return num3;
	}

	private float ratio_of_distance(float distance0)
	{
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		Component[] array = allBasicRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			Renderer obj = (Renderer)array[i];
			Vector3 center = obj.bounds.center;
			float magnitude = obj.bounds.extents.magnitude;
			Vector3[] array2 = new Vector3[6]
			{
				new Vector3(center.x - magnitude, center.y, center.z),
				new Vector3(center.x + magnitude, center.y, center.z),
				new Vector3(center.x, center.y - magnitude, center.z),
				new Vector3(center.x, center.y + magnitude, center.z),
				new Vector3(center.x, center.y, center.z - magnitude),
				new Vector3(center.x, center.y, center.z + magnitude)
			};
			for (int j = 0; j < array2.Length; j++)
			{
				Vector3 vector3 = array2[j];
				if (vector3.x < vector.x)
				{
					vector.x = vector3.x;
				}
				if (vector3.y < vector.y)
				{
					vector.y = vector3.y;
				}
				if (vector3.z < vector.z)
				{
					vector.z = vector3.z;
				}
				if (vector3.x > vector2.x)
				{
					vector2.x = vector3.x;
				}
				if (vector3.y > vector2.y)
				{
					vector2.y = vector3.y;
				}
				if (vector3.z > vector2.z)
				{
					vector2.z = vector3.z;
				}
			}
		}
		Vector3 b = (vector + vector2) * 0.5f;
		float num = Vector3.Distance(Camera.main.transform.position, b);
		float num2 = 1f - num / distance0;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		return num2;
	}

	private void Update()
	{
		if (!progressiveMesh)
		{
			return;
		}
		currentTimeToInterval -= Time.deltaTime;
		if (!(currentTimeToInterval <= 0f))
		{
			return;
		}
		bool flag = false;
		if (!culled)
		{
			allBasicRenderers = base.gameObject.GetComponentsInChildren(typeof(Renderer));
			Component[] array = allBasicRenderers;
			int num = 0;
			if (num < array.Length && ((Renderer)array[num]).isVisible)
			{
				flag = true;
			}
		}
		if (culled || flag)
		{
			float num2 = 0f;
			if (Camera.main != null && Camera.main.gameObject != null && Camera.main.gameObject.activeInHierarchy)
			{
				allBasicRenderers = base.gameObject.GetComponentsInChildren(typeof(Renderer));
				if (lod_strategy == 0)
				{
					num2 = ratio_of_screen();
				}
				if (lod_strategy == 1)
				{
					num2 = ratio_of_distance(disappear_distance);
				}
			}
			if (!never_cull && num2 < cull_ratio)
			{
				if (!culled)
				{
					allBasicRenderers = base.gameObject.GetComponentsInChildren(typeof(Renderer));
					Component[] array = allBasicRenderers;
					for (int num = 0; num < array.Length; num++)
					{
						((Renderer)array[num]).enabled = false;
					}
					culled = true;
				}
			}
			else
			{
				if (culled)
				{
					allBasicRenderers = base.gameObject.GetComponentsInChildren(typeof(Renderer));
					Component[] array = allBasicRenderers;
					for (int num = 0; num < array.Length; num++)
					{
						((Renderer)array[num]).enabled = true;
					}
					culled = false;
				}
				int num3 = progressiveMesh.triangles[0];
				int num4 = (int)((1f - num2) * (float)num3);
				if (num4 > num3 - 1)
				{
					num4 = num3 - 1;
				}
				if (current_lod != num4)
				{
					Component[] componentsInChildren = base.gameObject.GetComponentsInChildren(typeof(MeshFilter));
					Component[] componentsInChildren2 = base.gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer));
					Component[] array2 = new Component[componentsInChildren.Length + componentsInChildren2.Length];
					Array.Copy(componentsInChildren, 0, array2, 0, componentsInChildren.Length);
					Array.Copy(componentsInChildren2, 0, array2, componentsInChildren.Length, componentsInChildren2.Length);
					int num5 = MantisLODEditorUtility.SwitchRuntimeLOD(progressiveMesh, mesh_lod_range, num4, array2);
					if ((bool)lodHint)
					{
						lodHint.text = "Level Of Detail: " + num4;
					}
					if ((bool)triangleHint)
					{
						triangleHint.text = "Triangle Count: " + num5 / 3;
					}
					current_lod = num4;
				}
			}
		}
		if ((bool)fpsHint)
		{
			int num6 = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
			fpsHint.text = "FPS: " + num6;
		}
		currentTimeToInterval = updateInterval + (UnityEngine.Random.value + 0.5f) * currentTimeToInterval;
	}

	private void create_default_mesh_lod_range()
	{
		int num = progressiveMesh.triangles[0];
		int num2 = progressiveMesh.triangles[1];
		mesh_lod_range = new int[num2 * 2];
		for (int i = 0; i < num2; i++)
		{
			mesh_lod_range[i * 2] = 0;
			mesh_lod_range[i * 2 + 1] = num - 1;
		}
	}

	private void get_all_meshes()
	{
		if (!working)
		{
			_ = progressiveMesh.triangles[0];
			if (mesh_lod_range == null || mesh_lod_range.Length == 0)
			{
				create_default_mesh_lod_range();
			}
			Component[] componentsInChildren = base.gameObject.GetComponentsInChildren(typeof(MeshFilter));
			Component[] componentsInChildren2 = base.gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer));
			Component[] array = new Component[componentsInChildren.Length + componentsInChildren2.Length];
			Array.Copy(componentsInChildren, 0, array, 0, componentsInChildren.Length);
			Array.Copy(componentsInChildren2, 0, array, componentsInChildren.Length, componentsInChildren2.Length);
			MantisLODEditorUtility.GenerateRuntimeLOD(progressiveMesh, array, optimize_on_the_fly);
			allBasicRenderers = base.gameObject.GetComponentsInChildren(typeof(Renderer));
			currentTimeToInterval = UnityEngine.Random.value * updateInterval;
			current_lod = -1;
			working = true;
		}
	}

	public void reset_all_parameters()
	{
		optimize_on_the_fly = true;
		mesh_lod_range = null;
		never_cull = true;
		lod_strategy = 1;
		cull_ratio = 0.1f;
		disappear_distance = 250f;
		updateInterval = 0.25f;
	}

	private void clean_all()
	{
		if (working)
		{
			MantisLODEditorUtility.FinishRuntimeLOD(progressiveMesh);
			allBasicRenderers = null;
			working = false;
		}
	}

	private void OnEnable()
	{
		Awake();
		Start();
	}

	private void OnDisable()
	{
		clean_all();
	}

	private void OnDestroy()
	{
		clean_all();
	}
}
