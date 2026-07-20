using UnityEngine;

namespace TTDemoScripts;

public class PlayerHoverController : MonoBehaviour
{
	public float height = 1.3f;

	public float forwardSpeed = 10f;

	public float strafeSpeed = 10f;

	public float runMultiplier = 2f;

	public KeyCode runKey = KeyCode.LeftShift;

	public LayerMask groundLayer;

	private RaycastHit hit;

	private float hoverHeight;

	private void Awake()
	{
	}

	private void Update()
	{
		float z = Input.GetAxis("Vertical") * forwardSpeed * (Input.GetKey(runKey) ? runMultiplier : 1f) * Time.deltaTime;
		float x = Input.GetAxis("Horizontal") * strafeSpeed * Time.deltaTime;
		if (Physics.Raycast(base.transform.position + Vector3.up * 9999f, Vector3.down, out hit, float.PositiveInfinity, groundLayer))
		{
			hoverHeight = hit.point.y + height;
		}
		base.transform.Translate(new Vector3(x, hoverHeight - base.transform.position.y + height, z));
	}
}
