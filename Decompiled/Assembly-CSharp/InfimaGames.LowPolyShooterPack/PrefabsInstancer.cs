using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[Serializable]
public class PrefabsInstancer
{
	public GameObject Prefab;

	public int PrefabsCount;

	private GameObject[] prefabs;

	private int currentPrefab;

	private void SetActive(GameObject o, bool active)
	{
		o.name = active.ToString();
		Renderer[] componentsInChildren = o.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = active;
		}
	}

	public void CreatePrefabs(Transform parent)
	{
		if (Prefab == null)
		{
			return;
		}
		if (prefabs == null || (prefabs != null && prefabs.Length == 0))
		{
			prefabs = new GameObject[PrefabsCount];
			for (int i = 0; i < PrefabsCount; i++)
			{
				prefabs[i] = UnityEngine.Object.Instantiate(Prefab, parent);
			}
		}
		GameObject[] array = prefabs;
		foreach (GameObject obj in array)
		{
			obj.SetActive(value: true);
			obj.transform.position = new Vector3(-10000f, -10000f, -10000f);
		}
		currentPrefab = 0;
	}

	public void DeletePrefabs()
	{
		if (prefabs != null)
		{
			GameObject[] array = prefabs;
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object.Destroy(array[i]);
			}
			prefabs = null;
		}
	}

	public GameObject CreatePrefab(Vector3 pos, Quaternion rotation)
	{
		return CreatePrefab(pos, rotation, Vector3.zero);
	}

	public GameObject CreatePrefab(Vector3 pos, Quaternion rotation, Vector3 normal, bool useNormal = false)
	{
		if (prefabs == null)
		{
			return null;
		}
		GameObject gameObject = null;
		gameObject = prefabs[currentPrefab];
		currentPrefab++;
		if (currentPrefab >= prefabs.Length)
		{
			currentPrefab = 0;
		}
		if (gameObject == null)
		{
			return null;
		}
		gameObject.transform.position = pos;
		gameObject.transform.LookAt(pos + normal * 10f);
		if (!useNormal)
		{
			gameObject.transform.rotation = rotation;
		}
		ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Play();
		}
		TrailRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<TrailRenderer>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].Clear();
		}
		return gameObject;
	}
}
