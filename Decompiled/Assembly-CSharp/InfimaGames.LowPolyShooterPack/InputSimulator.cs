using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack;

public class InputSimulator : MonoBehaviourPun, IPunObservable
{
	public enum ActionState
	{
		None,
		Started,
		Performed,
		Canceled
	}

	public class Action
	{
		public InputActionPhase phase;
	}

	public bool firing;

	public bool aiming;

	public bool running;

	public bool throwing;

	public bool melee;

	public bool jumping;

	public bool reloading;

	public bool switching;

	public bool inspecting;

	public bool crouching;

	public Vector2 motion_axis;

	public Vector2 look_axis;

	public Character character;

	private bool was_firing;

	private bool was_aiming;

	private bool was_running;

	private bool was_throwing;

	private bool was_melee;

	private bool was_jumping;

	private bool was_reloading;

	private bool was_switching;

	private bool was_inspecting;

	private bool was_crouching;

	private Action startedAction = new Action();

	private Action performedAction = new Action();

	private Action canceledAction = new Action();

	public CrouchingInput crouchingInput;

	private CameraLook cameraLook;

	private CharacterMultiplayer characterMultiplayer;

	private NetworkTransformSynch networkTransformSynch;

	public const byte JUMP = 0;

	public const byte CROUCH = 1;

	public const byte RELOAD = 2;

	public const byte THROW = 3;

	public const byte MELEE = 4;

	public const byte SWITCH = 5;

	public const byte INSPECT = 6;

	public const byte OPEN_PARACHUTE = 7;

	public const byte END_PARACHUTING = 8;

	public const byte JUMP_FROM_PLANE = 9;

	private void Awake()
	{
		cameraLook = GetComponentInChildren<CameraLook>();
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
		networkTransformSynch = GetComponent<NetworkTransformSynch>();
	}

	private void Start()
	{
		startedAction.phase = InputActionPhase.Started;
		performedAction.phase = InputActionPhase.Performed;
		canceledAction.phase = InputActionPhase.Canceled;
		Restart();
	}

	private void Update()
	{
		if (firing && !was_firing)
		{
			character.OnTryFireSimulator(startedAction);
		}
		if (firing)
		{
			character.OnTryFireSimulator(performedAction);
		}
		if (!firing && was_firing)
		{
			character.OnTryFireSimulator(canceledAction);
		}
		if (aiming && !was_aiming)
		{
			character.OnTryAimingSimulator(startedAction);
		}
		if (aiming)
		{
			character.OnTryAimingSimulator(performedAction);
		}
		if (!aiming && was_aiming)
		{
			character.OnTryAimingSimulator(canceledAction);
		}
		if (running && !was_running)
		{
			character.OnTryRunSimulator(startedAction);
		}
		if (running)
		{
			character.OnTryRunSimulator(performedAction);
		}
		if (!running && was_running)
		{
			character.OnTryRunSimulator(canceledAction);
		}
		if (throwing && !was_throwing)
		{
			character.OnTryThrowGrenadeSimulator(startedAction);
		}
		if (throwing)
		{
			character.OnTryThrowGrenadeSimulator(performedAction);
			throwing = false;
			RPC_Action(3);
		}
		if (!throwing && was_throwing)
		{
			character.OnTryThrowGrenadeSimulator(canceledAction);
		}
		if (melee && !was_melee)
		{
			character.OnTryMeleeSimulator(startedAction);
		}
		if (melee)
		{
			character.OnTryMeleeSimulator(performedAction);
			melee = false;
			RPC_Action(4);
		}
		if (!melee && was_melee)
		{
			character.OnTryMeleeSimulator(canceledAction);
		}
		if (jumping && !was_jumping)
		{
			character.OnTryJumpSimulator(startedAction);
		}
		if (jumping)
		{
			character.OnTryJumpSimulator(performedAction);
			jumping = false;
		}
		if (!jumping && was_jumping)
		{
			character.OnTryJumpSimulator(canceledAction);
		}
		if (reloading && !was_reloading)
		{
			character.OnTryPlayReloadSimulator(startedAction);
		}
		if (reloading)
		{
			character.OnTryPlayReloadSimulator(performedAction);
			reloading = false;
			RPC_Action(2);
		}
		if (!reloading && was_reloading)
		{
			character.OnTryPlayReloadSimulator(canceledAction);
		}
		if (switching && !was_switching)
		{
			character.OnTryInventoryNextSimulator(startedAction);
		}
		if (switching)
		{
			character.OnTryInventoryNextSimulator(performedAction);
			switching = false;
		}
		if (!switching && was_switching)
		{
			character.OnTryInventoryNextSimulator(canceledAction);
		}
		if (inspecting && !was_inspecting)
		{
			character.OnTryInspectSimulator(startedAction);
		}
		if (inspecting)
		{
			character.OnTryInspectSimulator(performedAction);
			inspecting = false;
			RPC_Action(6);
		}
		if (!inspecting && was_inspecting)
		{
			character.OnTryInspectSimulator(canceledAction);
		}
		if (crouching && !was_crouching)
		{
			crouchingInput.OnTryCrouchSimulator(startedAction);
		}
		if (crouching)
		{
			crouchingInput.OnTryCrouchSimulator(performedAction);
			crouching = false;
			RPC_Action(1);
		}
		if (!crouching && was_crouching)
		{
			crouchingInput.OnTryCrouchSimulator(canceledAction);
		}
		was_firing = firing;
		was_aiming = aiming;
		was_running = running;
		was_throwing = throwing;
		was_melee = melee;
		was_jumping = jumping;
		was_reloading = reloading;
		was_switching = switching;
		was_inspecting = inspecting;
		was_crouching = crouching;
	}

	public void Restart()
	{
		firing = false;
		was_firing = false;
		aiming = false;
		running = false;
		throwing = false;
		melee = false;
		jumping = false;
		reloading = false;
		switching = false;
		inspecting = false;
		crouching = false;
		was_aiming = false;
		was_running = false;
		was_throwing = false;
		was_melee = false;
		was_jumping = false;
		was_reloading = false;
		was_switching = false;
		was_inspecting = false;
		was_crouching = false;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			if (!base.photonView.IsMine)
			{
				return;
			}
			if (!networkTransformSynch.compressLookRotation)
			{
				stream.SendNext(cameraLook.rotationPitchOverrided);
			}
			else
			{
				stream.SendNext((byte)((cameraLook.rotationPitchOverrided + 90f) / 180f * 255f));
			}
			byte b = 0;
			bool flag = firing;
			bool flag2 = aiming;
			bool flag3 = running;
			if (characterMultiplayer.isMainPlayer)
			{
				Character component = GetComponent<Character>();
				flag = component.IsHoldingButtonFire();
				flag2 = component.IsAiming();
				flag3 = component.IsRunning();
				if (flag3)
				{
					Debug.Log("main player sending run true");
				}
			}
			if (flag)
			{
				b |= 1;
			}
			if (flag2)
			{
				b |= 2;
			}
			if (flag3)
			{
				b |= 4;
			}
			stream.SendNext(b);
		}
		else if (!base.photonView.IsMine)
		{
			if (!networkTransformSynch.compressLookRotation)
			{
				cameraLook.rotationPitchOverrided = (float)stream.ReceiveNext();
			}
			else
			{
				byte b2 = (byte)stream.ReceiveNext();
				cameraLook.rotationPitchOverrided = (float)(int)b2 / 255f * 180f - 90f;
			}
			byte b3 = (byte)stream.ReceiveNext();
			firing = (b3 & 1) != 0;
			aiming = (b3 & 2) != 0;
			running = (b3 & 4) != 0;
			if (running)
			{
				Debug.Log("received run true");
			}
		}
	}

	public void RPC_Action(byte actionId)
	{
		if (base.photonView.IsMine)
		{
			base.photonView.RPC("RPC_InputSimAction", RpcTarget.Others, actionId);
		}
	}

	[PunRPC]
	private void RPC_InputSimAction(byte actionId)
	{
		if (!base.photonView.IsMine)
		{
			Debug.Log("RPC_InputSimAction " + actionId + " " + base.name);
			switch (actionId)
			{
			case 0:
				jumping = true;
				break;
			case 1:
				crouching = true;
				break;
			case 2:
				reloading = true;
				break;
			case 4:
				melee = true;
				break;
			case 5:
				switching = true;
				break;
			case 6:
				inspecting = true;
				break;
			case 3:
				throwing = true;
				break;
			case 7:
				GetComponent<CharacterParachute>().isParachuteOpen = true;
				break;
			case 8:
				GetComponent<CharacterParachute>().ForceHideParachute();
				break;
			case 9:
				GetComponent<CharacterParachute>().isOnAirplane = false;
				break;
			default:
				Debug.LogWarning("Unknown actionId: " + actionId);
				break;
			}
		}
	}
}
