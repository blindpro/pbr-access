using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class WallAvoidance : MonoBehaviour
{
	[Tooltip("The Transform of the character's camera.")]
	[SerializeField]
	private Transform playerCamera;

	[Tooltip("The maximum distance to check for walls.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float distance = 1f;

	[Tooltip("The radius of the sphere check.")]
	[Range(0f, 2f)]
	[SerializeField]
	private float radius = 0.5f;

	[Tooltip("The layers to count as wall layers.")]
	[SerializeField]
	private LayerMask layerMask;

	private bool hasWall;

	public bool HasWall => hasWall;

	private void Update()
	{
		if (playerCamera == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		Ray ray = new Ray(playerCamera.position, playerCamera.forward);
		hasWall = Physics.SphereCast(ray, radius, distance, layerMask);
	}
}
