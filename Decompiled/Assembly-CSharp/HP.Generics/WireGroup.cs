using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics;

public class WireGroup : MonoBehaviour
{
	[HideInInspector]
	public bool seeInspector;

	public bool moreOptions;

	public List<Wire> listWire = new List<Wire>();

	public float offsetForward = 2f;

	public float offsetDown = 0.5f;

	public float precision = 0.75f;
}
