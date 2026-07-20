using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class TerrainModif : MonoBehaviour
{
	[HideInInspector]
	public bool seeInspector;

	public List<Terrain> terrList = new List<Terrain>();

	public float roadOffsetHeight = 0.03f;
}
