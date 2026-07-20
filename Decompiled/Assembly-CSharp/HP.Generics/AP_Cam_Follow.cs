using UnityEngine;

namespace HP.Generics;

public class AP_Cam_Follow : MonoBehaviour
{
	public Transform target;

	public float rotationDamping = 15f;

	private void LateUpdate()
	{
		if (target != null)
		{
			base.transform.position = Vector3.Lerp(base.transform.position, target.position, Time.deltaTime * rotationDamping);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, target.rotation, Time.deltaTime * rotationDamping);
		}
	}
}
