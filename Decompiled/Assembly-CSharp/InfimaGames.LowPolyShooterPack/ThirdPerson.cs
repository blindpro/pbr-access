using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

namespace InfimaGames.LowPolyShooterPack;

public class ThirdPerson : MonoBehaviour
{
	public bool isActive;

	public CharacterMultiplayer characterMultiplayer;

	public CharacterController characterController;

	public Animator tps_animator;

	public Animator fps_animator;

	public Camera tps_camera;

	public Camera fps_camera;

	public Camera fps_weapon_camera;

	public Character character;

	public Movement movement;

	public Transform tps;

	public bool tps_camera_on;

	public LayerMask raycastMask;

	public Vector3 tps_main_pos = new Vector3(-0.2f, -0.2f, 0f);

	public Vector3 tps_others_no_bot_pos = new Vector3(-0.2f, -0.2f, 0f);

	public float anim_axis_smooth = 5f;

	public TwoBoneIKConstraint leftHandRifleIK;

	public TwoBoneIKConstraint leftHandPistolIK;

	public Rig aimRig;

	public Rig leftHandIKRig;

	public Transform fps_inventory_parent;

	public Transform tps_inventory_parent;

	public GameObject characterUI;

	public string fps_layer;

	public string tps_layer;

	public string fps_others_layer;

	public string tps_others_layer;

	public int fps_layer_int;

	public int tps_layer_int;

	public int fps_others_layer_int;

	public int tps_others_layer_int;

	public bool force_spectating_tps_cam = true;

	public bool force_spectating_aim_tps_cam = true;

	private Vector3 tps_main_pos_original;

	private Vector3 _local_velocity;

	private float aimRig_weight = 1f;

	private float leftHandIKRig_weight;

	private bool last_tps_mode;

	private bool tps_change_called_once;

	private NetworkTransformSynch networkTransformSynch;

	private CharacterParachute characterParachute;

	private Camera airplaneCamera;

	private AudioListener[] audioListeners;

	private bool wasActive;

	private SkinnedMeshRenderer[] allSkinMeshes;

	private MeshRenderer[] allMeshes;

	private SkinnedMeshRenderer[] allSkinMeshesALL;

	private MeshRenderer[] allMeshesALL;

	private CharacterBone[] allBones;

	private bool isNoMainPlayerComponentsOff;

	private GameObject FPSCharacterRootMotion;

	private GameObject FPSik_hand_gun;

	private void Awake()
	{
		isActive = false;
		wasActive = true;
		networkTransformSynch = GetComponent<NetworkTransformSynch>();
		audioListeners = GetComponentsInChildren<AudioListener>(includeInactive: true);
		characterParachute = GetComponent<CharacterParachute>();
		FPSCharacterRootMotion = GetComponentsInChildren<JumpMotion>()[0].gameObject;
		FPSik_hand_gun = GetComponentsInChildren<JumpMotion>()[1].gameObject;
	}

	private void Start()
	{
		tps_main_pos_original = tps.transform.localPosition;
		_local_velocity = Vector3.zero;
		last_tps_mode = !tps_camera_on;
		fps_layer_int = LayerMask.NameToLayer(fps_layer);
		tps_layer_int = LayerMask.NameToLayer(tps_layer);
		fps_others_layer_int = LayerMask.NameToLayer(fps_others_layer);
		tps_others_layer_int = LayerMask.NameToLayer(tps_others_layer);
		airplaneCamera = GameManager.Instance.GetComponent<AirplaneManager>().AirplaneCamera;
		allSkinMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
		allMeshes = GetComponentsInChildren<MeshRenderer>();
		allSkinMeshesALL = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		allMeshesALL = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		allBones = GetComponentsInChildren<CharacterBone>();
	}

	private void Update()
	{
		bool flag = false;
		bool flag2 = characterMultiplayer.IsDead();
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		bool isParachuting = characterParachute.isParachuting;
		bool isOnAirplane = characterParachute.isOnAirplane;
		bool isParachuteOpen = characterParachute.isParachuteOpen;
		bool flag9 = false;
		bool isMainPlayer = characterMultiplayer.isMainPlayer;
		bool flag10 = CharacterMultiplayer.GetSpectatingPlayer() != null;
		if (isParachuting && !isOnAirplane)
		{
			isActive = true;
		}
		if (wasActive != isActive)
		{
			bool flag11 = true;
			if (!isActive && !flag2)
			{
				flag11 = false;
			}
			SkinnedMeshRenderer[] array = allSkinMeshes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = flag11;
			}
			MeshRenderer[] array2 = allMeshes;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = flag11;
			}
			Debug.LogWarning("ThirdPerson show all meshes");
		}
		wasActive = isActive;
		flag3 = character.IsThrowingGrenade();
		flag4 = character.IsMeleeing();
		flag6 = character.IsRunning();
		flag7 = character.IsCrouching();
		if (!characterMultiplayer.isBot && !characterMultiplayer.isLocal)
		{
			movement.ComputeIsInAir();
		}
		flag8 = !movement.IsGroundedApproximate();
		flag = character.IsReloading();
		if (characterMultiplayer.isBot)
		{
			flag8 = characterParachute.isParachuting;
		}
		if (flag8)
		{
			flag6 = false;
		}
		InventoryBehaviour inventory = character.GetInventory();
		if ((bool)inventory && (bool)inventory.GetEquipped() && inventory.GetEquipped().name.Contains("Handgun"))
		{
			flag5 = true;
		}
		tps_camera.fieldOfView = fps_camera.fieldOfView;
		bool flag12 = tps_camera_on;
		if (character.GetAimingAlpha() >= 0.1f)
		{
			flag12 = false;
		}
		if (!character.IsAiming() && tps_camera_on)
		{
			flag12 = true;
		}
		if (flag2 || isParachuting || flag9)
		{
			flag12 = true;
		}
		bool flag13 = false;
		bool flag14 = false;
		bool flag15 = false;
		bool flag16 = false;
		if (flag12)
		{
			flag13 = true;
			flag14 = false;
			flag15 = false;
		}
		else
		{
			flag13 = false;
			flag14 = true;
			flag15 = true;
		}
		if (!isMainPlayer && !characterMultiplayer.isSpectating)
		{
			flag12 = true;
			flag13 = false;
			flag14 = false;
			flag15 = false;
		}
		if (flag10 && isMainPlayer)
		{
			flag13 = false;
			flag14 = false;
			flag15 = false;
		}
		if (characterMultiplayer.isSpectating)
		{
			if (force_spectating_tps_cam)
			{
				tps_camera_on = true;
			}
			if (force_spectating_aim_tps_cam)
			{
				flag12 = true;
			}
			if (flag12)
			{
				flag13 = true;
				flag14 = false;
				flag15 = false;
			}
			else
			{
				flag13 = false;
				flag14 = true;
				flag15 = true;
			}
		}
		if (!isActive)
		{
			flag13 = ((isMainPlayer && flag2 && !flag10 && MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Finish) ? true : false);
			flag14 = false;
			flag15 = false;
		}
		if (isMainPlayer)
		{
			if (isOnAirplane)
			{
				flag13 = false;
				flag14 = false;
				flag15 = false;
				flag16 = true;
			}
			else
			{
				flag16 = false;
			}
			Statics.EnableCamera(airplaneCamera, flag16);
		}
		Statics.EnableCamera(tps_camera, flag13);
		Statics.EnableCamera(fps_camera, flag14);
		Statics.EnableCamera(fps_weapon_camera, flag15);
		if (last_tps_mode != flag12 || !tps_change_called_once)
		{
			tps_change_called_once = true;
			if (flag12)
			{
				if ((bool)inventory)
				{
					inventory.transform.parent = tps_inventory_parent;
					inventory.transform.localPosition = Vector3.zero;
					inventory.transform.localRotation = Quaternion.identity;
					inventory.transform.localScale = Vector3.one;
					Statics.SetLayer(inventory.gameObject, tps_layer_int, tps_layer, fps_layer);
					SkinnedMeshRenderer[] array = allSkinMeshesALL;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].shadowCastingMode = ShadowCastingMode.On;
					}
					MeshRenderer[] array2 = allMeshesALL;
					for (int i = 0; i < array2.Length; i++)
					{
						array2[i].shadowCastingMode = ShadowCastingMode.On;
					}
				}
				else
				{
					Debug.LogError("inventory null");
				}
			}
			else if ((bool)inventory)
			{
				inventory.transform.parent = fps_inventory_parent;
				inventory.transform.localPosition = Vector3.zero;
				inventory.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
				inventory.transform.localScale = Vector3.one;
				Statics.SetLayer(inventory.gameObject, fps_layer_int, fps_layer, tps_layer);
				SkinnedMeshRenderer[] array = allSkinMeshesALL;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].shadowCastingMode = ShadowCastingMode.Off;
				}
				MeshRenderer[] array2 = allMeshesALL;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].shadowCastingMode = ShadowCastingMode.Off;
				}
			}
			else
			{
				Debug.LogError("inventory null");
			}
			Vector3 center = characterController.center;
			if (isMainPlayer && flag12)
			{
				center.x = -0.1f;
			}
			else
			{
				center.x = 0f;
			}
			characterController.center = center;
			Debug.LogWarning("ThirdPerson last_tps_mode != tps_mode " + base.name);
		}
		last_tps_mode = flag12;
		inventory.tps_mode = flag12;
		if (!isMainPlayer)
		{
			if (characterMultiplayer.isSpectating)
			{
				Statics.SetLayer(fps_animator.gameObject, fps_layer_int, fps_layer);
				Statics.SetLayer(tps_animator.gameObject, tps_layer_int, tps_layer);
			}
			else
			{
				Statics.SetLayer(fps_animator.gameObject, fps_others_layer_int, fps_others_layer);
				Statics.SetLayer(tps_animator.gameObject, tps_others_layer_int, tps_others_layer);
			}
		}
		else if (flag10)
		{
			Statics.SetLayer(fps_animator.gameObject, fps_others_layer_int, fps_others_layer);
			Statics.SetLayer(tps_animator.gameObject, tps_others_layer_int, tps_others_layer);
		}
		else
		{
			Statics.SetLayer(fps_animator.gameObject, fps_layer_int, fps_layer);
			Statics.SetLayer(tps_animator.gameObject, tps_layer_int, tps_layer);
		}
		tps_main_pos_original.x = (tps_main_pos_original.z = 0f);
		if (isMainPlayer)
		{
			tps.transform.localPosition = tps_main_pos;
		}
		else
		{
			tps.transform.localPosition = tps_main_pos_original;
			if (!characterMultiplayer.isBot && !characterMultiplayer.isLocal)
			{
				Vector3 localPosition = tps_others_no_bot_pos;
				localPosition.x = (localPosition.z = 0f);
				tps.transform.localPosition = localPosition;
			}
		}
		Vector3 direction = networkTransformSynch.world_velocity;
		if (direction.magnitude < 0.5f && !characterMultiplayer.isLocal)
		{
			direction = Vector3.zero;
			flag6 = false;
		}
		Vector3 normalized = characterController.transform.InverseTransformDirection(direction).normalized;
		if (!flag6)
		{
			normalized *= 2f;
		}
		_local_velocity = Vector3.Lerp(_local_velocity, normalized, Time.deltaTime * anim_axis_smooth);
		tps_animator.SetFloat("Horizontal", _local_velocity.x);
		tps_animator.SetFloat("Vertical", _local_velocity.z);
		tps_animator.SetBool("Crouching", flag7);
		tps_animator.SetBool("Running", flag6);
		tps_animator.SetBool("Pistol", flag5);
		tps_animator.SetBool("InAir", flag8);
		tps_animator.SetBool("Throwing", flag3);
		tps_animator.SetBool("Melee", flag4);
		tps_animator.SetBool("Reloading", flag);
		tps_animator.SetBool("Parachuting", isParachuting);
		tps_animator.SetBool("ParachuteOpen", isParachuteOpen);
		Animator parachute = characterParachute.parachute;
		bool flag17 = false;
		if (parachute.gameObject.activeInHierarchy)
		{
			flag17 = true;
			parachute.SetFloat("Horizontal", _local_velocity.x);
			parachute.SetFloat("Vertical", _local_velocity.z);
			parachute.SetBool("ParachuteOpen", isParachuting && isParachuteOpen);
		}
		leftHandPistolIK.weight = (flag5 ? 1 : 0);
		aimRig.weight = ((!(flag6 || flag || flag2 || isParachuting || flag9 || flag17)) ? 1 : 0);
		leftHandIKRig.weight = ((!((flag6 && flag5) || flag || flag2 || flag3 || flag4 || isParachuting || flag9 || flag17)) ? 1 : 0);
		float num = 10f;
		float num2 = 10f;
		if (aimRig.weight > 0.9f && flag5)
		{
			num = 2f;
		}
		if (leftHandIKRig.weight > 0.9f && flag5)
		{
			num2 = 2f;
		}
		aimRig_weight = Mathf.Lerp(aimRig_weight, aimRig.weight, Time.deltaTime * num);
		leftHandIKRig_weight = Mathf.Lerp(leftHandIKRig_weight, leftHandIKRig.weight, Time.deltaTime * num2);
		aimRig.weight = aimRig_weight;
		leftHandIKRig.weight = leftHandIKRig_weight;
		bool active = false;
		if (CharacterMultiplayer.GetMainPlayer().IsSquadMember(characterMultiplayer) && !characterMultiplayer.isSpectating)
		{
			active = true;
		}
		Statics.SetActive(characterUI, active);
		if (!characterMultiplayer.isMainPlayer && !isNoMainPlayerComponentsOff)
		{
			isNoMainPlayerComponentsOff = true;
			if (GetComponent<Movement>().enabled)
			{
				GetComponent<Movement>().enabled = false;
			}
			if (GetComponent<CharacterController>().enabled)
			{
				GetComponent<CharacterController>().enabled = false;
			}
			if ((bool)GetComponentInChildren<WallAvoidance>())
			{
				GetComponentInChildren<WallAvoidance>().gameObject.SetActive(value: false);
			}
			if ((bool)GetComponentInChildren<CrouchingInput>())
			{
				GetComponentInChildren<CrouchingInput>().gameObject.SetActive(value: false);
			}
			Debug.LogWarning("no main player disable components not needed");
		}
		Statics.SetActive(FPSCharacterRootMotion, characterMultiplayer.isMainPlayer || characterMultiplayer.isSpectating);
		Statics.SetActive(FPSik_hand_gun, characterMultiplayer.isMainPlayer || characterMultiplayer.isSpectating);
	}

	public void ComputeLookAtRaycast()
	{
		CharacterBone[] array = allBones;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].GetComponent<Collider>().isTrigger = true;
		}
		if (Physics.Raycast(new Ray(fps_camera.transform.position, fps_camera.transform.forward), out var hitInfo, 10000f, raycastMask, QueryTriggerInteraction.Ignore))
		{
			character.lookat_cursor_raycasted = hitInfo.point;
			character.lookat_cursor_raycasted_normal = hitInfo.normal;
			character.lookat_cursor_raycasted_collider = hitInfo.collider;
		}
		else
		{
			character.lookat_cursor_raycasted = fps_camera.transform.position + fps_camera.transform.forward * 500f;
			character.lookat_cursor_raycasted_normal = -fps_camera.transform.forward;
			character.lookat_cursor_raycasted_collider = null;
		}
		array = allBones;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].GetComponent<Collider>().isTrigger = false;
		}
	}

	public void OnMatchStarted()
	{
		Debug.Log(base.name + "OnMatchStarted");
		isActive = false;
	}

	public void OnDeadScreen()
	{
	}

	public void OnMatchFinished()
	{
	}

	public void OnMatchFinishedEndScreen()
	{
		isActive = false;
	}

	public void ToggleCameraMode()
	{
		tps_camera_on = !tps_camera_on;
	}
}
