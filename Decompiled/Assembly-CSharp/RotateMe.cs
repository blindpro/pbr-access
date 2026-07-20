using UnityEngine;

public class RotateMe : MonoBehaviour
{
	private Vector3 center_to_ratate;

	private void Start()
	{
		center_to_ratate = new Vector3(0f, 0f, 0f);
		Component[] componentsInChildren = base.gameObject.GetComponentsInChildren(typeof(Renderer));
		Component[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 center = ((Renderer)array[i]).bounds.center;
			center_to_ratate += center;
		}
		center_to_ratate /= (float)componentsInChildren.Length;
	}

	private void Update()
	{
		base.gameObject.transform.RotateAround(center_to_ratate, Vector3.up, 10f * Time.deltaTime);
		base.gameObject.transform.RotateAround(center_to_ratate, Vector3.forward, 15f * Time.deltaTime);
	}
}
