using System;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class TerrainStarter : MonoBehaviour
{
	[Serializable]
	public class SpawnObjParams
	{
		public bool show = true;

		public string name = "";

		public GameObject objToSpawn;

		public int probabilityToSpawn = 100;

		public Vector3 offsetPosition;

		public Vector3 randRotation;

		public float randomScaleMin;

		public float randomScaleMax;

		public int safeZone = 5;

		public float safeHeight = 0.005f;
	}

	[Serializable]
	public class DetailParams
	{
		public bool isShown;

		public string name;

		public int probability;

		public float threshold = 1f;

		public int detailIntensity = 1;

		public Texture2D detailTexture;

		public GameObject detailObject;

		public float minWidth = 1f;

		public float maxWidth = 2f;

		public float minHeight = 1f;

		public float maxHeight = 2f;

		public float noiseSpread = 0.1f;

		public float holeEdgePadding;

		public Color healthyColor = Color.white;

		public Color dryColor = Color.white;

		public DetailRenderMode detailRenderModeGrass = DetailRenderMode.Grass;

		public DetailRenderMode detailRenderModeObj = DetailRenderMode.VertexLit;

		public DetailParams(Color _healthyColor, Color _dryColor)
		{
			healthyColor = _healthyColor;
			dryColor = _dryColor;
		}
	}

	[Serializable]
	public class LayerParams
	{
		public string name = "";

		public TerrainLayer terrainLayer;

		public List<DetailParams> detailList = new List<DetailParams>();
	}

	[Serializable]
	public class TerrainSettings
	{
		public float windSpeed;

		public float windSize;

		public float windBending;

		public Color windGrassTint = Color.white;

		public float detailObjectDistance = 150f;

		public float terrainWidth = 300f;

		public float terrainLength = 300f;

		public float terrainHeight = 250f;

		public int detailResolutionPatch = 64;

		public int detailResolution = 512;

		public int heightmapResolution = 513;

		public int controlTextureResolution = 512;

		public int baseTextureResolution = 1024;

		public float scaleInLightmap = 0.01f;

		public int basemapDistance = 400;

		public float heightmapPixelError = 5f;

		public float treeBillboardDistance = 500f;

		public float treeCrossFadeLength = 5f;
	}

	[Serializable]
	public class TreeSettings
	{
		public bool isShown;

		public string name;

		public GameObject objTree;

		public float bendFactor;
	}

	[HideInInspector]
	public bool seeInspector;

	public RoadData roadData;

	public List<Terrain> terrList = new List<Terrain>();

	public List<Texture2D> noisetexList = new List<Texture2D>();

	public Texture2D noiseOffsetHeight;

	public float heightOffset = 0.001f;

	public List<SpawnObjParams> spawnObjList = new List<SpawnObjParams>();

	public int gridSize = 10;

	[HideInInspector]
	public int editorTabIndex;

	public List<LayerParams> layerParamsList = new List<LayerParams>();

	public List<DetailParams> detailListRef = new List<DetailParams>();

	public TerrainSettings terrainSettings = new TerrainSettings();

	public List<TreeSettings> treeSettings = new List<TreeSettings>();
}
