using System.Collections;
using UnityEngine;

namespace HP.Generics;

public class characterMovement : MonoBehaviour
{
	public bool SeeInspector = true;

	public Rigidbody rbBodyCharacter;

	public Transform tangentStartPosition;

	public Transform objCamera;

	public GameObject addForceObj;

	public Transform refHead;

	private string s_mouseAxisX = "Mouse X";

	private string s_mouseAxisY = "Mouse Y";

	public float currentDesktop_X_Axis;

	public float currentDesktop_Y_Axis;

	public float speedKeybordMovement = 2f;

	public float minimum = -60f;

	public float maximum = 60f;

	public float characterSpeed = 2f;

	public float sensibilityMouse = 2f;

	public AnimationCurve animationCurveMouse;

	public float mouseY;

	private float tmpXAxis;

	private float tmpYAxis;

	public LayerMask myLayerMask;

	private float XAxis;

	private float YAxis;

	private float mouseVertical;

	private float mouseInputX;

	private float mouseHorizontal;

	public float BrakeForce = 35f;

	public float Coeff = 0.15f;

	public float MaxSpeed = 1f;

	public scPreventClimbing preventClimbing;

	public bool allowCrouch;

	public bool b_Crouch;

	public float targetScaleCrouch = 0.5f;

	private float refScaleCrouch = 1f;

	public float crouchSpeed = 3f;

	public float heightCheck = 2.05f;

	public LayerMask layerCheckCrouch;

	public float speedMultiplier = 3f;

	private float currentSpeedMultiplier = 1f;

	public bool b_AllowRun;

	public bool isRunning;

	public float gravityScale = 1f;

	private static float globalGravity = -9.81f;

	public float MaxAngle = 70f;

	private float currentAngle;

	private Vector3 circlePos = Vector3.zero;

	public bool moreInfoMaxAngle = true;

	public bool isOnFloor = true;

	private float hitDistance = 0.35f;

	public float hitDistanceMin = 0.45f;

	public float hitDistanceMax = 0.75f;

	public LayerMask myLayer;

	public Vector3 rayPosition = Vector3.zero;

	public PhysicMaterial pMove;

	public PhysicMaterial pStop;

	public PhysicMaterial pIce;

	private CapsuleCollider charCol;

	public LayerMask myLayer02;

	public float overlapSize = 0.2f;

	public float overlapPos = 0.11f;

	public bool b_Overlap;

	public bool b_TouchLayer12_17;

	public int jumpForce = 4;

	public float jumpSpeed = 10f;

	public bool b_IsJumping;

	public float minimumJump = 0.2f;

	public float GravityFallSpeed = 4f;

	public float heightRoof = 0.45f;

	public float fallCurve;

	public AnimationCurve animFallCurve;

	public KeyCode upKey = KeyCode.E;

	public KeyCode downKey = KeyCode.D;

	public KeyCode leftKey = KeyCode.S;

	public KeyCode rightKey = KeyCode.F;

	public KeyCode runKey = KeyCode.LeftShift;

	public KeyCode jumpKey = KeyCode.Space;

	public KeyCode crouchKey = KeyCode.C;

	private Vector3 joyInput = Vector3.zero;

	public bool isMovementAllowed = true;

	public float jumpMulti01 = 1f;

	public float jumpMulti02 = 1f;

	private void Start()
	{
		refScaleCrouch = base.gameObject.transform.localScale.y;
		charCol = GetComponent<CapsuleCollider>();
	}

	public void charaGeneralMovementController()
	{
		bodyMovement();
	}

	private void Update()
	{
		if (isMovementAllowed)
		{
			if (Input.GetKeyDown(jumpKey) && !b_IsJumping && isOnFloor)
			{
				StartCoroutine(Jump());
			}
			joyInput = new Vector3(0f, 0f, 0f);
			if (joyInput.sqrMagnitude > 1f)
			{
				joyInput = joyInput.normalized;
			}
			mouseHorizontal = Input.GetAxis(s_mouseAxisX);
			mouseVertical = Input.GetAxis(s_mouseAxisY);
			XAxis = returnDesktopXAxis();
			YAxis = returnDesktopYAxis();
			mouseInputX = Input.GetAxis("Mouse X");
			bodyRotation();
			cameraRotation();
			CheckCrouch();
			CheckRun();
		}
	}

	private void CheckCrouch()
	{
		if (allowCrouch && Input.GetKeyDown(crouchKey) && ((b_Crouch && AP_CheckIfPlayerCanStopCrouching()) || !b_Crouch))
		{
			b_Crouch = !b_Crouch;
		}
	}

	private void CheckRun()
	{
		if (b_AllowRun)
		{
			if (Input.GetKey(runKey) && !b_Crouch)
			{
				isRunning = true;
				currentSpeedMultiplier = speedMultiplier;
			}
			else
			{
				isRunning = false;
				currentSpeedMultiplier = 1f;
			}
		}
	}

	private void FixedUpdate()
	{
		AP_OverlapSphere();
		Ap_isOnFloor();
		AP_ApplyGravity();
		CrouchUpdate();
	}

	private void CrouchUpdate()
	{
		if (allowCrouch)
		{
			if (b_Crouch && base.gameObject.transform.localScale.y != targetScaleCrouch)
			{
				base.gameObject.transform.localScale = Vector3.MoveTowards(base.gameObject.transform.localScale, new Vector3(base.gameObject.transform.localScale.x, targetScaleCrouch, base.gameObject.transform.localScale.z), Time.deltaTime * crouchSpeed);
			}
			else if (!b_Crouch && base.gameObject.transform.localScale.y != refScaleCrouch)
			{
				base.gameObject.transform.localScale = Vector3.MoveTowards(base.gameObject.transform.localScale, new Vector3(base.gameObject.transform.localScale.x, refScaleCrouch, base.gameObject.transform.localScale.z), Time.deltaTime * crouchSpeed);
			}
		}
	}

	private void bodyRotation()
	{
		if (mouseHorizontal != 0f)
		{
			tmpXAxis = mouseInputX * 1.1f;
			tmpXAxis *= sensibilityMouse * 1.2f;
		}
		else
		{
			tmpXAxis = 0f;
		}
		objCamera.transform.Rotate(0f, tmpXAxis, 0f);
	}

	private void cameraRotation()
	{
		if (mouseVertical != 0f)
		{
			tmpYAxis = mouseVertical;
			tmpYAxis = Mathf.Clamp(tmpYAxis, -3f, 3f);
			tmpYAxis *= 1.5f;
			mouseY -= tmpYAxis * sensibilityMouse * (float)returnInvertMouseAxis() * 1.2f;
		}
		mouseY = Mathf.Clamp(mouseY, minimum, maximum);
		objCamera.localEulerAngles = new Vector3(mouseY, objCamera.localEulerAngles.y, 0f);
	}

	private void bodyMovement()
	{
		addForceObj.transform.localEulerAngles = new Vector3(addForceObj.transform.localEulerAngles.x, objCamera.transform.localEulerAngles.y, addForceObj.transform.localEulerAngles.z);
		Vector3 vector = new Vector3(0f, 0f, 0f);
		vector += FindTangentX() * XAxis;
		vector += FindTangentZ() * YAxis;
		if (preventClimbing.b_preventClimbing)
		{
			vector.y = 0f;
		}
		if (isOnFloor)
		{
			if (currentAngle >= 180f - MaxAngle)
			{
				rbBodyCharacter.AddForceAtPosition(vector * characterSpeed * currentSpeedMultiplier, addForceObj.transform.position, ForceMode.Force);
			}
			Vector3 vector2 = rbBodyCharacter.transform.InverseTransformDirection(-rbBodyCharacter.velocity);
			rbBodyCharacter.AddRelativeForce(vector2 * BrakeForce * Coeff, ForceMode.Force);
			if (rbBodyCharacter.velocity.magnitude > MaxSpeed)
			{
				rbBodyCharacter.velocity = rbBodyCharacter.velocity.normalized * MaxSpeed;
			}
		}
		else
		{
			rbBodyCharacter.AddForceAtPosition(vector * characterSpeed * currentSpeedMultiplier, addForceObj.transform.position, ForceMode.Force);
			Vector3 vector3 = rbBodyCharacter.transform.InverseTransformDirection(new Vector3(0f - rbBodyCharacter.velocity.x, 0f, 0f - rbBodyCharacter.velocity.z));
			rbBodyCharacter.AddRelativeForce(vector3 * BrakeForce * Coeff, ForceMode.Force);
			if (rbBodyCharacter.velocity.magnitude > MaxSpeed)
			{
				rbBodyCharacter.velocity = rbBodyCharacter.velocity.normalized * MaxSpeed;
			}
		}
	}

	private float returnDesktopXAxis()
	{
		float num = currentDesktop_X_Axis;
		bool flag = false;
		if (Input.GetKey(leftKey))
		{
			if (num > 0f)
			{
				num = 0f;
			}
			num = Mathf.MoveTowards(num, -1f, Time.deltaTime * speedKeybordMovement);
			flag = true;
		}
		if (Input.GetKey(rightKey))
		{
			if (num < 0f)
			{
				num = 0f;
			}
			num = Mathf.MoveTowards(num, 1f, Time.deltaTime * speedKeybordMovement);
			flag = true;
		}
		if (!flag)
		{
			num = Mathf.MoveTowards(num, 0f, Time.deltaTime * speedKeybordMovement * 2f);
		}
		currentDesktop_X_Axis = num;
		return num;
	}

	private float returnDesktopYAxis()
	{
		float num = currentDesktop_Y_Axis;
		bool flag = false;
		if (Input.GetKey(downKey))
		{
			if (num > 0f)
			{
				num = 0f;
			}
			num = Mathf.MoveTowards(num, -1f, Time.deltaTime * speedKeybordMovement);
			flag = true;
		}
		if (Input.GetKey(upKey))
		{
			if (num < 0f)
			{
				num = 0f;
			}
			num = Mathf.MoveTowards(num, 1f, Time.deltaTime * speedKeybordMovement);
			flag = true;
		}
		if (!flag)
		{
			num = Mathf.MoveTowards(num, 0f, Time.deltaTime * speedKeybordMovement * 2f);
		}
		currentDesktop_Y_Axis = num;
		return num;
	}

	private Vector3 FindTangentZ()
	{
		Vector3 vector = Vector3.zero;
		if (Physics.Raycast(tangentStartPosition.position, -Vector3.up, out var hitInfo, 10f, myLayerMask))
		{
			hitInfo.normal.Normalize();
			vector = Vector3.Cross(hitInfo.normal, -addForceObj.transform.right);
			if (vector.magnitude == 0f)
			{
				vector = Vector3.Cross(hitInfo.normal, Vector3.up);
			}
			Debug.DrawRay(hitInfo.point, vector, Color.yellow);
		}
		return vector;
	}

	private Vector3 FindTangentX()
	{
		Vector3 vector = Vector3.zero;
		if (Physics.Raycast(tangentStartPosition.position, -Vector3.up, out var hitInfo, 10f, myLayerMask))
		{
			hitInfo.normal.Normalize();
			Vector3 rhs = Vector3.Cross(addForceObj.transform.right, hitInfo.normal);
			Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.white);
			vector = Vector3.Cross(hitInfo.normal, rhs);
			if (vector.magnitude == 0f)
			{
				vector = Vector3.Cross(hitInfo.normal, Vector3.up);
			}
			Debug.DrawRay(hitInfo.point, vector, Color.red);
		}
		return vector;
	}

	private int returnInvertMouseAxis()
	{
		return 1;
	}

	public void charaStopMoving()
	{
		if (rbBodyCharacter.velocity != Vector3.zero)
		{
			rbBodyCharacter.velocity = Vector3.zero;
		}
	}

	private void AP_ApplyGravity()
	{
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, -Vector3.up, out var hitInfo, 100f) && isOnFloor)
		{
			currentAngle = Vector3.SignedAngle(hitInfo.normal, -Vector3.up, Vector3.up);
			gravityScale = 1f - (180f - currentAngle) / 80f;
			circlePos = hitInfo.point;
		}
		if (b_TouchLayer12_17 && isOnFloor)
		{
			charCol.material = pIce;
			gravityScale = 0f;
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
		}
		else if (currentAngle < 180f - MaxAngle || !isOnFloor)
		{
			charCol.material = pIce;
			fallCurve = Mathf.MoveTowards(fallCurve, 1f, Time.deltaTime);
			gravityScale = Mathf.MoveTowards(gravityScale, 20f, animFallCurve.Evaluate(fallCurve) * GravityFallSpeed * Time.deltaTime);
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
		}
		else if (YAxis == 0f && XAxis == 0f)
		{
			charCol.material = pStop;
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
			gravityScale = 0f;
		}
		else if (YAxis == 0f && XAxis != 0f)
		{
			charCol.material = pMove;
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
			gravityScale = 0f;
		}
		else
		{
			charCol.material = pMove;
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
		}
		if (rbBodyCharacter.velocity.sqrMagnitude * 10000f < 2f && YAxis == 0f && XAxis == 0f && isOnFloor)
		{
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeAll;
			gravityScale = 0f;
		}
		if (b_IsJumping)
		{
			charCol.material = pIce;
			rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
		}
		Vector3 force = globalGravity * gravityScale * Vector3.up;
		rbBodyCharacter.AddForce(force, ForceMode.Acceleration);
	}

	public void Ap_isOnFloor()
	{
		float num = 0.6f * (180f - currentAngle) / 80f;
		if (isOnFloor)
		{
			hitDistance = hitDistanceMax + num;
		}
		else
		{
			hitDistance = hitDistanceMin + num;
		}
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, -Vector3.up, hitDistance, myLayer))
		{
			if (b_Overlap)
			{
				isOnFloor = true;
			}
			rayPosition = base.transform.position + Vector3.up * 0.1f;
		}
		else
		{
			if (b_Overlap)
			{
				isOnFloor = false;
			}
			rayPosition = base.transform.position;
		}
	}

	private void AP_OverlapSphere()
	{
		if (Physics.OverlapSphere(base.transform.position + Vector3.up * overlapPos, overlapSize, myLayer02).Length != 0)
		{
			b_Overlap = true;
			return;
		}
		b_Overlap = false;
		isOnFloor = false;
	}

	private bool AP_CheckIfPlayerCanStopCrouching()
	{
		Debug.DrawRay(base.transform.position + Vector3.up * 0.1f, Vector3.up * heightCheck, Color.yellow);
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, Vector3.up, heightCheck, layerCheckCrouch))
		{
			return false;
		}
		return true;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer == 12 || collision.gameObject.layer == 17)
		{
			b_TouchLayer12_17 = true;
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (collision.gameObject.layer == 12 || collision.gameObject.layer == 17)
		{
			b_TouchLayer12_17 = true;
		}
		if (collision.gameObject.layer == 18)
		{
			isOnFloor = true;
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		if (collision.gameObject.layer == 12 || collision.gameObject.layer == 17)
		{
			b_TouchLayer12_17 = false;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(circlePos, 0.1f);
		if (rayPosition == base.transform.position)
		{
			Gizmos.color = Color.green;
		}
		else
		{
			Gizmos.color = Color.blue;
		}
		Gizmos.DrawSphere(rayPosition, 0.1f);
		Gizmos.color = Color.white;
		Gizmos.DrawSphere(base.transform.position + Vector3.up * 0.334f, overlapSize);
	}

	public IEnumerator Jump()
	{
		fallCurve = 0f;
		float t = 0f;
		b_IsJumping = true;
		bool keyUp = false;
		rbBodyCharacter.AddForceAtPosition(Vector3.up * 5f * jumpMulti01, addForceObj.transform.position, ForceMode.Impulse);
		while (t < 0.5f)
		{
			if (Input.GetKeyUp(jumpKey))
			{
				keyUp = true;
			}
			if (AP_CheckIfPlayerIsTouchingRoof() || keyUp)
			{
				t = 2f;
			}
			else
			{
				float num = 1f - rbBodyCharacter.velocity.normalized.y;
				rbBodyCharacter.AddForceAtPosition(Vector3.up * (0.25f + 0.25f * num) * Time.deltaTime * 100f * jumpMulti02, addForceObj.transform.position, ForceMode.Impulse);
				t += Time.deltaTime;
			}
			yield return new WaitForEndOfFrame();
		}
		b_IsJumping = false;
		fallCurve = 0f;
		yield return null;
	}

	private bool AP_CheckIfPlayerIsTouchingRoof()
	{
		if (Physics.Raycast(refHead.transform.position + Vector3.up * 0.1f, Vector3.up, heightRoof, layerCheckCrouch))
		{
			return true;
		}
		return false;
	}

	public void ResetMovement()
	{
		currentDesktop_X_Axis = 0f;
		currentDesktop_Y_Axis = 0f;
		YAxis = 0f;
		XAxis = 0f;
		isRunning = false;
		gravityScale = 0f;
		isMovementAllowed = true;
		charCol.material = pStop;
		rbBodyCharacter.velocity *= 0f;
		rbBodyCharacter.angularVelocity *= 0f;
	}
}
