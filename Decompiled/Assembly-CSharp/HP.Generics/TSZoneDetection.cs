using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics;

public class TSZoneDetection : MonoBehaviour
{
	public UnityEvent ActionOnColliderEnter;

	public UnityEvent ActionOnColliderExit;

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)other.GetComponent<TSCharacterTag>())
		{
			ActionOnColliderEnter.Invoke();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((bool)other.GetComponent<TSCharacterTag>())
		{
			ActionOnColliderExit.Invoke();
		}
	}
}
