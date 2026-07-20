using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooldhands.UICursor.Example;

public class RotateOnSelected : MonoBehaviour
{
	public Vector3 RotateVector = new Vector3(50f, 0f, 0f);

	private void Update()
	{
		if (EventSystem.current.currentSelectedGameObject == base.gameObject)
		{
			base.transform.Rotate(RotateVector * Time.deltaTime);
		}
	}
}
