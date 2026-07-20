using UnityEngine;

namespace HP.Generics;

public class scPreventClimbing : MonoBehaviour
{
	public bool b_preventClimbing;

	private void OnTriggerStay(Collider other)
	{
	}

	private void OnTriggerExit(Collider other)
	{
	}
}
