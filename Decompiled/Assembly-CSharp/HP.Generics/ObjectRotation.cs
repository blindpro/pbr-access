using UnityEngine;

namespace HP.Generics;

public class ObjectRotation : MonoBehaviour
{
	public Transform trans;

	public float speed = 1000f;

	public void Update()
	{
		RotateObject();
	}

	private void RotateObject()
	{
		trans.localEulerAngles += new Vector3(0f, 0f, Time.deltaTime * speed);
	}
}
