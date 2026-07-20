using UnityEngine;

namespace TTDemoScripts;

public class SimpleSmoothMouseLook : MonoBehaviour
{
	private Vector2 _mouseAbsolute;

	private Vector2 _smoothMouse;

	public Vector2 clampInDegrees = new Vector2(360f, 180f);

	public bool lockCursor;

	public Vector2 sensitivity = new Vector2(2f, 2f);

	public Vector2 smoothing = new Vector2(3f, 3f);

	public Vector2 targetDirection;

	private void Start()
	{
		targetDirection = base.transform.localRotation.eulerAngles;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			lockCursor = !lockCursor;
		}
		Cursor.lockState = (lockCursor ? CursorLockMode.Locked : CursorLockMode.None);
		Cursor.visible = !lockCursor;
		Quaternion quaternion = Quaternion.Euler(targetDirection);
		Vector2 a = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
		a = Vector2.Scale(a, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));
		_smoothMouse.x = Mathf.Lerp(_smoothMouse.x, a.x, 1f / smoothing.x);
		_smoothMouse.y = Mathf.Lerp(_smoothMouse.y, a.y, 1f / smoothing.y);
		_mouseAbsolute += _smoothMouse;
		if (clampInDegrees.x < 360f)
		{
			_mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, (0f - clampInDegrees.x) * 0.5f, clampInDegrees.x * 0.5f);
		}
		Quaternion localRotation = Quaternion.AngleAxis(0f - _mouseAbsolute.y, quaternion * Vector3.right);
		base.transform.localRotation = localRotation;
		if (clampInDegrees.y < 360f)
		{
			_mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, (0f - clampInDegrees.y) * 0.5f, clampInDegrees.y * 0.5f);
		}
		base.transform.localRotation *= quaternion;
		Quaternion quaternion2 = Quaternion.AngleAxis(_mouseAbsolute.x, base.transform.InverseTransformDirection(Vector3.up));
		base.transform.localRotation *= quaternion2;
	}
}
