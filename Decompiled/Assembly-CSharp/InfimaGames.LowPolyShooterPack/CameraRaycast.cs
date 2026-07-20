using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CameraRaycast : MonoBehaviour
{
	public LayerMask layer;

	public Transform target;

	public float default_local_pos_z = 1.86f;

	public float inPlane_local_pos_z = 20f;

	public float parachute_local_pos_z = 10f;

	public float radius = 0.2f;

	public float smoothing = 10f;

	public float smoothing_parachting = 2f;

	private float maxDistance;

	private Vector3 currentLocalPos;

	private Camera cam;

	private CharacterParachute characterParachute;

	private void Awake()
	{
		maxDistance = default_local_pos_z;
		currentLocalPos = new Vector3(0f, 0f, 0f - default_local_pos_z);
		base.transform.localPosition = currentLocalPos;
		cam = GetComponent<Camera>();
		characterParachute = GetComponentInParent<CharacterParachute>();
	}

	private void LateUpdate()
	{
		if ((bool)cam && !cam.enabled)
		{
			return;
		}
		maxDistance = default_local_pos_z;
		float num = smoothing;
		if ((bool)characterParachute)
		{
			if (characterParachute.isParachuting)
			{
				num = smoothing_parachting;
			}
			if (characterParachute.isParachuting && characterParachute.isParachuteOpen)
			{
				maxDistance = parachute_local_pos_z;
			}
		}
		Vector3 normalized = (target.TransformPoint(new Vector3(0f, 0f, 0f - default_local_pos_z)) - target.position).normalized;
		float num2 = maxDistance;
		if (Physics.SphereCast(target.position, radius, normalized, out var hitInfo, maxDistance + radius, layer, QueryTriggerInteraction.Ignore))
		{
			num2 = Mathf.Clamp(hitInfo.distance - radius, 0f, maxDistance);
		}
		float b = 0f - Mathf.Lerp(0f, maxDistance, num2 / maxDistance);
		currentLocalPos.z = Mathf.Lerp(currentLocalPos.z, b, Time.deltaTime * num);
		base.transform.localPosition = currentLocalPos;
	}

	public void OnMatchStarted()
	{
		currentLocalPos = new Vector3(0f, 0f, 0f - inPlane_local_pos_z);
		base.transform.localPosition = currentLocalPos;
	}
}
