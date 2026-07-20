using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Movement : MovementBehaviour
{
	public AudioSource jumpAudioSource;

	public AudioSource landAudioSource;

	[Tooltip("How fast the character's speed increases.")]
	[SerializeField]
	private float acceleration = 9f;

	[Tooltip("Acceleration value used when the character is in the air. This means either jumping, or falling.")]
	[SerializeField]
	private float accelerationInAir = 3f;

	[Tooltip("How fast the character's speed decreases.")]
	[SerializeField]
	private float deceleration = 11f;

	[Tooltip("The speed of the player while walking.")]
	[SerializeField]
	private float speedWalking = 4f;

	[Tooltip("How fast the player moves while aiming.")]
	[SerializeField]
	private float speedAiming = 3.2f;

	[Tooltip("How fast the player moves while aiming.")]
	[SerializeField]
	private float speedCrouching = 3.5f;

	[Tooltip("How fast the player moves while running.")]
	[SerializeField]
	private float speedRunning = 6.8f;

	[Tooltip("Value to multiply the walking speed by when the character is moving forward.")]
	[SerializeField]
	[Range(0f, 1f)]
	private float walkingMultiplierForward = 1f;

	[Tooltip("Value to multiply the walking speed by when the character is moving sideways.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float walkingMultiplierSideways = 1f;

	[Tooltip("Value to multiply the walking speed by when the character is moving backwards.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float walkingMultiplierBackwards = 1f;

	[Tooltip("How much control the player has over changes in direction while the character is in the air.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float airControl = 0.8f;

	[Tooltip("The value of the character's gravity. Basically, defines how fast the character falls.")]
	[SerializeField]
	private float gravity = 1.1f;

	[Tooltip("The value of the character's gravity while jumping.")]
	[SerializeField]
	private float jumpGravity = 1f;

	[Tooltip("The value of the character's gravity. Basically, defines how fast the character falls when flying.")]
	[SerializeField]
	private float flyGravity = 2f;

	[Tooltip("The value of the character's gravity. Basically, defines how fast the character falls when flying with parachute.")]
	[SerializeField]
	private float parachuteGravity = 1f;

	public Vector2 flyControlMultiplierMinMax = new Vector2(1f, 3f);

	[Tooltip("The force of the jump.")]
	[SerializeField]
	private float jumpForce = 100f;

	[Tooltip("Force applied to keep the character from flying away while descending slopes.")]
	[SerializeField]
	private float stickToGroundForce = 0.03f;

	[Tooltip("height to check from ground to be in air.")]
	[SerializeField]
	private float heightToBeInAir = 1.5f;

	[Tooltip("Setting this to false will always block the character from crouching.")]
	[SerializeField]
	private bool canCrouch = true;

	[Tooltip("If true, the character will be able to crouch/un-crouch while falling, which can lead to some slightly interesting results.")]
	[SerializeField]
	private bool canCrouchWhileFalling;

	[Tooltip("If true, the character will be able to jump while crouched too!")]
	[SerializeField]
	private bool canJumpWhileCrouching = true;

	[Tooltip("Height of the character while crouching.")]
	[SerializeField]
	private float standHeight = 1f;

	[Tooltip("center of the character while crouching.")]
	[SerializeField]
	private Vector3 standCenter = new Vector3(0f, 0.65f, 0f);

	[Tooltip("Height of the character while crouching.")]
	[SerializeField]
	private float crouchHeight = 1f;

	[Tooltip("center of the character while crouching.")]
	[SerializeField]
	private Vector3 crouchCenter = new Vector3(0f, 0.6f, 0f);

	[Tooltip("Mask of possible layers that can cause overlaps when trying to un-crouch. Very important!")]
	[SerializeField]
	private LayerMask crouchOverlapsMask;

	[Tooltip("Force applied to other rigidbodies when walking into them. This force is multiplied by the character's velocity, so it is never applied by itself, that's important to note.")]
	[SerializeField]
	private float rigidbodyPushForce = 1f;

	private CharacterController controller;

	private CharacterBehaviour playerCharacter;

	private WeaponBehaviour equippedWeapon;

	private Vector3 velocity;

	private bool isGrounded;

	private bool wasGrounded;

	private bool isInAir;

	private bool jumping;

	private bool crouching;

	private float lastJumpTime;

	private CharacterMultiplayer characterMultiplayer;

	private CharacterParachute characterParachute;

	private bool wasParachuteOpen;

	private bool shouldLand;

	public bool isInAirComputed;

	protected override void Awake()
	{
		playerCharacter = GetComponentInParent<Character>();
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
	}

	protected override void Start()
	{
		controller = GetComponent<CharacterController>();
		characterParachute = GetComponent<CharacterParachute>();
		Crouch(false);
	}

	protected override void Update()
	{
		equippedWeapon = playerCharacter.GetInventory().GetEquipped();
		ComputeIsInAir();
		isGrounded = IsGrounded();
		isInAir = IsInAir();
		if (isInAirComputed)
		{
			shouldLand = true;
		}
		if (isGrounded && !wasGrounded)
		{
			if (!characterMultiplayer.isBot && shouldLand)
			{
				landAudioSource.Play();
				shouldLand = false;
			}
			jumping = false;
			lastJumpTime = 0f;
		}
		else if (wasGrounded && !isGrounded)
		{
			lastJumpTime = Time.time;
		}
		MoveCharacter();
		wasGrounded = isGrounded;
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (hit.moveDirection.y > 0f && velocity.y > 0f)
		{
			velocity.y = 0f;
		}
		Rigidbody rigidbody = hit.rigidbody;
		if (!(rigidbody == null))
		{
			Vector3 force = (hit.moveDirection + Vector3.up * 0.35f) * velocity.magnitude * rigidbodyPushForce;
			rigidbody.AddForceAtPosition(force, hit.point);
		}
	}

	private void MoveCharacter()
	{
		Vector2 vector = Vector3.ClampMagnitude(playerCharacter.GetInputMovement(), 1f);
		Vector3 direction = new Vector3(vector.x, 0f, vector.y);
		if (playerCharacter.IsRunning())
		{
			direction *= speedRunning;
		}
		else if (crouching)
		{
			direction *= speedCrouching;
		}
		else if (playerCharacter.IsAiming())
		{
			direction *= speedAiming;
		}
		else
		{
			direction *= speedWalking;
			direction.x *= walkingMultiplierSideways;
			direction.z *= ((vector.y > 0f) ? walkingMultiplierForward : walkingMultiplierBackwards);
		}
		direction = base.transform.TransformDirection(direction);
		if (equippedWeapon != null)
		{
			direction *= equippedWeapon.GetMultiplierMovementSpeed();
		}
		if (!isGrounded)
		{
			if (wasGrounded && !jumping)
			{
				velocity.y = 0f;
			}
			if (characterParachute.isParachuting)
			{
				float num = 1f;
				float value = 1f + 2f * base.transform.position.y * 0.02f;
				value = Mathf.Clamp(value, flyControlMultiplierMinMax.x, flyControlMultiplierMinMax.y);
				if (Vector3.Dot(direction.normalized, base.transform.forward) > -0.5f)
				{
					direction *= value;
				}
				velocity = Vector3.Lerp(velocity, new Vector3(direction.x, velocity.y, direction.z), Time.deltaTime * num);
			}
			else
			{
				velocity += direction * (accelerationInAir * airControl * Time.deltaTime);
			}
			float num2 = gravity;
			if (characterParachute.isParachuting)
			{
				num2 = flyGravity;
			}
			if (characterParachute.isParachuteOpen)
			{
				num2 = parachuteGravity;
				if (!wasParachuteOpen)
				{
					velocity.y = 0f;
				}
			}
			wasParachuteOpen = characterParachute.isParachuteOpen;
			if (GetComponent<CharacterMultiplayer>().isLocal)
			{
				velocity.y -= ((velocity.y >= 0f) ? jumpGravity : num2) * Time.deltaTime;
			}
		}
		else if (!jumping)
		{
			velocity = Vector3.Lerp(velocity, new Vector3(direction.x, velocity.y, direction.z), Time.deltaTime * ((direction.sqrMagnitude > 0f) ? acceleration : deceleration));
		}
		Vector3 motion = velocity * Time.deltaTime;
		if (controller.isGrounded && !jumping)
		{
			motion.y = 0f - stickToGroundForce;
		}
		controller.Move(motion);
	}

	public override bool WasGrounded()
	{
		return wasGrounded;
	}

	public void ComputeIsInAir()
	{
		if (IsGrounded())
		{
			isInAirComputed = false;
			return;
		}
		Vector3 origin = controller.transform.position + controller.center;
		float radius = controller.radius;
		float num = controller.height * 0.5f;
		isInAirComputed = !Physics.SphereCast(origin, radius * 0.7f, -Vector3.up, out var _, num * heightToBeInAir, crouchOverlapsMask);
	}

	public override bool IsInAir()
	{
		if (GetComponent<CharacterMultiplayer>().isBot)
		{
			isInAirComputed = characterParachute.isParachuting;
		}
		return isInAirComputed;
	}

	public override bool IsJumping()
	{
		return jumping;
	}

	public override bool CanCrouch(bool newCrouching)
	{
		if (!canCrouch)
		{
			return false;
		}
		if (!isGrounded && !canCrouchWhileFalling && GetComponent<CharacterMultiplayer>().isLocal)
		{
			return false;
		}
		if (newCrouching)
		{
			return true;
		}
		return Physics.OverlapSphere(base.transform.position + Vector3.up * standHeight, controller.radius, crouchOverlapsMask).Length == 0;
	}

	public override bool IsCrouching()
	{
		return crouching;
	}

	public override void Jump()
	{
		if ((!crouching || canJumpWhileCrouching) && isGrounded)
		{
			jumping = true;
			velocity = new Vector3(velocity.x, Mathf.Sqrt(2f * jumpForce * jumpGravity), velocity.z);
			lastJumpTime = Time.time;
			if (!characterMultiplayer.isBot)
			{
				jumpAudioSource.Play();
			}
		}
	}

	public override void Crouch(bool newCrouching)
	{
		if ((bool)characterMultiplayer && (bool)GameManager.Instance && characterMultiplayer.IsLocalMainPlayer() && characterMultiplayer.GetComponent<ThirdPerson>().isActive)
		{
			GameManager.Instance.GetComponent<HitCursorsManager>().PlayCrouchSound();
		}
		crouching = newCrouching;
		controller.height = (crouching ? crouchHeight : standHeight);
		controller.center = (crouching ? crouchCenter : standCenter);
	}

	public override void TryCrouch(bool value)
	{
		if (value && CanCrouch(newCrouching: true))
		{
			Crouch(true);
		}
		else if (!value)
		{
			StartCoroutine("TryUncrouch");
		}
	}

	public override void TryToggleCrouch()
	{
		TryCrouch(!crouching);
	}

	private IEnumerator TryUncrouch()
	{
		yield return new WaitUntil(() => CanCrouch(newCrouching: false));
		Crouch(false);
	}

	public override float GetLastJumpTime()
	{
		return lastJumpTime;
	}

	public override float GetMultiplierForward()
	{
		return walkingMultiplierForward;
	}

	public override float GetMultiplierSideways()
	{
		return walkingMultiplierSideways;
	}

	public override float GetMultiplierBackwards()
	{
		return walkingMultiplierBackwards;
	}

	public override Vector3 GetVelocity()
	{
		if (controller == null)
		{
			return Vector3.zero;
		}
		return controller.velocity;
	}

	public override bool IsGrounded()
	{
		if (!controller.enabled)
		{
			return false;
		}
		return controller.isGrounded;
	}

	public override bool IsGroundedApproximate()
	{
		return !IsInAir();
	}
}
