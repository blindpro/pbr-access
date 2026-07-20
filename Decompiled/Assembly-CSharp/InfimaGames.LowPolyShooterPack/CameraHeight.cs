using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CameraHeight : MonoBehaviour
{
	[Tooltip("The Character Controller component.")]
	[SerializeField]
	private CharacterController characterController;

	[Tooltip("The interpolation speed of the camera. Determines how smoothly the camera will transition to its new location.")]
	[SerializeField]
	private float interpolationSpeed = 12f;

	private float height = 1.8f;

	private void Update()
	{
		if (characterController == null)
		{
			Log.kill("Component " + base.name + " on GameObject " + base.gameObject.name + " has missing references, and willnot correctly function. Please fix this so the component can work properly!");
		}
		else
		{
			float b = characterController.height * 0.9f;
			height = Mathf.Lerp(height, b, interpolationSpeed * Time.deltaTime);
			base.transform.localPosition = Vector3.up * height;
		}
	}
}
