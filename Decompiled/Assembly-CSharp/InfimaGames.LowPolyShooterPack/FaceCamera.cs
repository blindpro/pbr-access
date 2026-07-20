using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class FaceCamera : MonoBehaviour
{
	private Transform cameraTransform;

	private void Start()
	{
		if (Camera.main != null)
		{
			cameraTransform = Camera.main.transform;
		}
	}

	private void Update()
	{
		base.transform.LookAt(cameraTransform, Vector3.up);
	}
}
