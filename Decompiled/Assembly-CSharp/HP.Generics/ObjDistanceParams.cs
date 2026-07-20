using System;
using UnityEngine;

namespace HP.Generics;

[Serializable]
public class ObjDistanceParams
{
	public GameObject obj;

	public bool state;

	public ObjDistanceParams(GameObject _obj, bool _state)
	{
		obj = _obj;
		state = _state;
	}
}
