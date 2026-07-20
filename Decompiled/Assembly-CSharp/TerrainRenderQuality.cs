using System;
using UnityEngine;

public class TerrainRenderQuality : MonoBehaviour
{
	[Serializable]
	public class TerrainQuality
	{
		public string qualityName = "";

		public float detailDistance = 50f;

		public float detailDensity = 0.045f;

		public float treesDistance = 130f;

		public int maxMeshTree = 50;

		public void Apply(Terrain terrain)
		{
			if (!terrain)
			{
				Debug.LogError("apply to terrain null");
				return;
			}
			terrain.detailObjectDistance = detailDistance;
			terrain.detailObjectDensity = detailDensity;
			terrain.treeDistance = treesDistance;
			terrain.treeMaximumFullLODCount = maxMeshTree;
			Debug.Log(qualityName + " terrain qualitty applied");
		}

		public void Get(Terrain terrain)
		{
			if (!terrain)
			{
				Debug.LogError("get terrain null");
				return;
			}
			detailDistance = terrain.detailObjectDistance;
			detailDensity = terrain.detailObjectDensity;
			treesDistance = terrain.treeDistance;
			maxMeshTree = terrain.treeMaximumFullLODCount;
		}
	}

	public TerrainQuality[] qualities;

	private TerrainQuality original = new TerrainQuality();

	private Terrain terrain;

	private string currentRenderQuality = "";

	private void OnEnable()
	{
		terrain = GetComponent<Terrain>();
		original.Get(terrain);
	}

	private void Start()
	{
		currentRenderQuality = QualitySettings.names[QualitySettings.GetQualityLevel()];
		ApplyQuality(currentRenderQuality);
	}

	private void OnDisable()
	{
		original.Apply(terrain);
	}

	private void Update()
	{
		string text = QualitySettings.names[QualitySettings.GetQualityLevel()];
		if (text != currentRenderQuality)
		{
			ApplyQuality(text);
			currentRenderQuality = text;
		}
	}

	private void ApplyQuality(string renderQuality)
	{
		TerrainQuality[] array = qualities;
		foreach (TerrainQuality terrainQuality in array)
		{
			if (terrainQuality.qualityName == renderQuality)
			{
				terrainQuality.Apply(terrain);
				break;
			}
		}
	}
}
