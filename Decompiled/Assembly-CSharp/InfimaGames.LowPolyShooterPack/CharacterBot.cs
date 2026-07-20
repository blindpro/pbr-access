using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterBot : MonoBehaviour
{
	public enum Status
	{
		looting,
		go_to_circle,
		fight_enemy
	}

	public bool isEnemyLost;

	public bool isOutOfAmmo;

	public float maxTrackDistance = 30f;

	public float trackDuration = 60f;

	public float stuckCheckDuration = 10f;

	public bool debug;

	public Status CurrentStatus;

	public AmmoBox targetAmmoBox;

	public LayerMask groundLayer;

	public LayerMask defaultLayer;

	public LayerMask defaultAndGroundLayer;

	public float moveSpeed = 4f;

	public float runSpeed = 5f;

	public float aimSpeed = 2f;

	public float crouchSpeed = 2f;

	public float turnSpeed = 5f;

	public float fov_dist = 100f;

	public float fov_angle = 100f;

	public bool canDoHardComputingThisFrame = true;

	public float maxDurationToStayWithoutWeapon = 25f;

	public float bot_vs_bot_difficulty = 0.2f;

	public float lootingToggleOffDuration = 50f;

	public float lootingToggleOnDuration = 300f;

	public float bot_vs_bot_delay_before_enable_kills = 300f;

	private PickupsManager pickupsManager;

	private DamageZoneManager damageZone;

	private CharacterMultiplayer characterMultiplayer;

	private CharacterParachute characterParachute;

	private CharacterInventory characterInventory;

	private InputSimulator inputSimulator;

	private ThirdPerson thirdPerson;

	private NavMeshAgent agent;

	private bool agentWasEnabled;

	private float waitResetAgentTime = 5f;

	private Vector3 currentDestination;

	private Vector3 targetPoint;

	private CharacterMultiplayer mySquadLeader;

	private CameraLook cameraLook;

	private float targetPointRadius = 5f;

	private float targetPointReachRadius = 0.5f;

	private bool targetPointStop;

	private List<AmmoBox> lootedBoxes = new List<AmmoBox>();

	private float trackTimer;

	private float ammoBoxStopTimer;

	private float stuckTimer;

	private float lootInterval = 3f;

	private float nextLootTime;

	private float lootingToggleTimer;

	private bool lootingOnOff = true;

	private bool shouldLootCurrentBox;

	private CharacterMultiplayer enemy;

	private float aimingTimer;

	private float aimPreciseDuration = 2f;

	private bool bot_vs_bot_kill;

	private float bot_difficulty = 1f;

	private float nextFireDecisionTime;

	private bool decidedFiring;

	private CharacterMultiplayer lastEnemy;

	private void Start()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		damageZone = GameManager.Instance.GetComponent<DamageZoneManager>();
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
		characterParachute = GetComponent<CharacterParachute>();
		thirdPerson = GetComponent<ThirdPerson>();
		agent = GetComponent<NavMeshAgent>();
		cameraLook = GetComponentInChildren<CameraLook>();
		characterInventory = GetComponent<CharacterInventory>();
		inputSimulator = GetComponent<InputSimulator>();
		agent.enabled = false;
		agent.updatePosition = true;
		agent.updateRotation = false;
		agent.speed = moveSpeed;
		agent.angularSpeed = turnSpeed;
		SettingsManager component = GameManager.Instance.GetComponent<SettingsManager>();
		MatchmakingManager component2 = GameManager.Instance.GetComponent<MatchmakingManager>();
		int value = component.botsDifficultyDropdown.value;
		bot_difficulty = 1f;
		if (value == 0 && component2.IsOffline())
		{
			bot_difficulty = 0.2f;
		}
		if (value == 1 && component2.IsOffline())
		{
			bot_difficulty = 0.5f;
		}
		Debug.Log("bot difficulty " + bot_difficulty);
	}

	private void Update()
	{
		bool flag = ShouldEnableAgent();
		if (flag != agentWasEnabled)
		{
			if (flag)
			{
				StartAgent();
			}
			else
			{
				StopAgent();
			}
		}
		UpdateBot();
		agentWasEnabled = flag;
	}

	private void EnableBotVsBotKill()
	{
		bot_vs_bot_kill = true;
	}

	public void OnMatchStarted()
	{
		bot_vs_bot_kill = false;
		agent.enabled = false;
		agentWasEnabled = false;
		lootedBoxes.Clear();
		CurrentStatus = Status.looting;
		isEnemyLost = false;
		isOutOfAmmo = false;
		targetAmmoBox = null;
		ammoBoxStopTimer = 0f;
		lootingOnOff = true;
		lootingToggleTimer = lootingToggleOnDuration;
		CancelInvoke("EnableBotVsBotKill");
		Invoke("EnableBotVsBotKill", bot_vs_bot_delay_before_enable_kills);
	}

	public void OnMatchFinished()
	{
	}

	private void StopAgent()
	{
		agent.enabled = false;
		inputSimulator.firing = false;
		inputSimulator.aiming = false;
		inputSimulator.running = false;
		Debug.LogWarning("Stop Bot");
	}

	private void StartAgent()
	{
		agent.enabled = true;
		agent.updatePosition = true;
		agent.updateRotation = false;
		agent.isStopped = false;
		agent.ResetPath();
		agent.Warp(base.transform.position);
		if (!agent.isOnNavMesh)
		{
			if (NavMesh.SamplePosition(base.transform.position, out var hit, 5f, -1))
			{
				agent.Warp(hit.position);
			}
			else
			{
				Debug.LogError("Couldn't find NavMesh near agent!" + base.name);
			}
		}
		UpdateDestination(targetPointRadius);
		Debug.LogWarning("Enable Bot");
		CancelInvoke("LootWeapon");
		Invoke("LootWeapon", maxDurationToStayWithoutWeapon);
	}

	private Vector3 GetRandomPointAroundTarget(Vector3 t, float radius = 5f, float first_radius_scale = 0.5f)
	{
		for (int i = 0; i < 10; i++)
		{
			Vector2 vector = Random.insideUnitCircle * radius;
			if (i == 0)
			{
				vector *= first_radius_scale;
			}
			if (NavMesh.SamplePosition(t + new Vector3(vector.x, 0f, vector.y), out var hit, radius, -1))
			{
				return hit.position;
			}
			if (i == 0 && CurrentStatus == Status.looting && !shouldLootCurrentBox)
			{
				shouldLootCurrentBox = true;
			}
		}
		Debug.LogWarning(base.name + " random point not found ");
		Vector2 vector2 = Random.insideUnitCircle * radius;
		_ = t + new Vector3(vector2.x, 0f, vector2.y);
		return t;
	}

	public void SetDestination(Vector3 target, float radius = 5f, float first_radius_scale = 0.5f)
	{
		currentDestination = GetRandomPointAroundTarget(target, radius, first_radius_scale);
		if (debug)
		{
			Debug.LogWarning(base.name + " SetDestination " + currentDestination.ToString());
		}
		if (!agent.SetDestination(currentDestination))
		{
			Debug.LogWarning("SetDestination1 failed retry in few sec " + base.name);
			StartCoroutine(WaitResetAgent());
		}
	}

	private IEnumerator WaitResetAgent()
	{
		yield return new WaitForSeconds(waitResetAgentTime);
		ResetAgent();
	}

	private void ResetAgent()
	{
		StopAgent();
		if (ShouldEnableAgent())
		{
			StartAgent();
		}
	}

	private bool ShouldEnableAgent()
	{
		if (characterMultiplayer.isBot && characterMultiplayer.isLocal && !characterParachute.isParachuting && !characterMultiplayer.IsDead() && thirdPerson.isActive)
		{
			return MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing;
		}
		return false;
	}

	private void SelectValidSquadLeader()
	{
		mySquadLeader = null;
	}

	private void UpdateBot()
	{
		if (agent.enabled)
		{
			SelectValidSquadLeader();
			GoToDestination();
			UpdateRotation();
			UpdateStatus();
			if (inputSimulator.running)
			{
				agent.speed = runSpeed;
			}
			else if (inputSimulator.aiming)
			{
				agent.speed = aimSpeed;
			}
			else
			{
				agent.speed = moveSpeed;
			}
		}
	}

	private void GoToDestination()
	{
		if (debug)
		{
			Debug.Log($"has path {agent.hasPath} pathPending {agent.pathPending} pathCompleted {agent.pathStatus}");
		}
		CheckStuck();
		if (Vector3.Distance(base.transform.position, currentDestination) < targetPointReachRadius && ammoBoxStopTimer <= 0f)
		{
			UpdateDestination(targetPointRadius);
		}
	}

	private Vector3 GetDamageZoneDestination()
	{
		Vector3 targetDamageZonePos = damageZone.GetTargetDamageZonePos();
		float targetDamageZoneRadius = damageZone.GetTargetDamageZoneRadius();
		Vector2 vector = Random.insideUnitCircle * targetDamageZoneRadius;
		if (Physics.Raycast(new Ray(targetDamageZonePos + new Vector3(vector.x, 0f, vector.y), Vector3.down), out var hitInfo, 10000f, groundLayer))
		{
			return hitInfo.point;
		}
		Debug.LogWarning("GetDamageZoneDestination failed");
		return base.transform.position;
	}

	private void UpdateStatus()
	{
		isOutOfAmmo = IsOutOfAmmo();
		if (UpdateStatus(trackDuration, Time.deltaTime))
		{
			UpdateDestination(targetPointRadius);
		}
	}

	private Vector3 GetNextTarget()
	{
		targetPoint = GetDamageZoneDestination();
		Vector3 targetDamageZonePos = damageZone.GetTargetDamageZonePos();
		float targetDamageZoneRadius = damageZone.GetTargetDamageZoneRadius();
		Vector3 position = base.transform.position;
		if (CurrentStatus == Status.looting)
		{
			targetPointRadius = 5f;
			shouldLootCurrentBox = Random.value >= 0.5f;
			targetAmmoBox = GetTargetAmmoBox(position, targetDamageZonePos, targetDamageZoneRadius, pickupsManager.ammoBoxesAchievable, lootedBoxes, shouldLootCurrentBox);
			if ((bool)targetAmmoBox)
			{
				targetPoint = targetAmmoBox.transform.position;
				ammoBoxStopTimer = Random.Range(2.5f, 5f);
				if (shouldLootCurrentBox)
				{
					ammoBoxStopTimer *= 2f;
					if (lootedBoxes != null && !lootedBoxes.Contains(targetAmmoBox))
					{
						lootedBoxes.Add(targetAmmoBox);
					}
				}
				nextLootTime = ammoBoxStopTimer;
			}
			else if (debug)
			{
				Debug.LogWarning("ammo box not found");
			}
		}
		else if (CurrentStatus == Status.go_to_circle)
		{
			targetAmmoBox = null;
			targetPointRadius = 5f;
		}
		else if (CurrentStatus == Status.fight_enemy)
		{
			targetAmmoBox = null;
			if ((bool)enemy)
			{
				if (!isEnemyLost)
				{
					targetPoint = GetRandomPointAroundTarget(base.transform.position, 2f, 1f);
				}
				else
				{
					targetPoint = GetRandomPointAroundTarget(enemy.transform.position, 5f, 1f);
				}
			}
		}
		return targetPoint;
	}

	private void UpdateDestination(float radius)
	{
		GetNextTarget();
		float first_radius_scale = 0.1f;
		if (CurrentStatus == Status.looting && !shouldLootCurrentBox)
		{
			first_radius_scale = 0f;
		}
		SetDestination(targetPoint, targetPointRadius, first_radius_scale);
	}

	private void UpdateRotation()
	{
		Vector3 position = thirdPerson.fps_camera.transform.position;
		Vector3 world_velocity = GetComponent<NetworkTransformSynch>().world_velocity;
		world_velocity.y = 0f;
		if (world_velocity.sqrMagnitude <= 0.001f)
		{
			ammoBoxStopTimer -= Time.deltaTime;
			if (ammoBoxStopTimer <= 0f)
			{
				ammoBoxStopTimer = 0f;
			}
		}
		bool flag = false;
		if (CurrentStatus == Status.looting || CurrentStatus == Status.go_to_circle)
		{
			if (world_velocity.sqrMagnitude > 0.001f)
			{
				LookAt(position + world_velocity.normalized * 10f, turnSpeed);
			}
			else if ((bool)targetAmmoBox)
			{
				bool flag2 = false;
				if (Vector3.Distance(base.transform.position, targetAmmoBox.transform.position) >= 0.3f || shouldLootCurrentBox || IsOutOfAmmo())
				{
					flag2 = true;
				}
				if (flag2)
				{
					if (Physics.Linecast(thirdPerson.fps_camera.transform.position, targetAmmoBox.transform.position + new Vector3(0f, 0.5f, 0f), out var hitInfo, defaultAndGroundLayer, QueryTriggerInteraction.Ignore))
					{
						if (debug)
						{
							Debug.LogWarning("not visible ammo box ignored, " + hitInfo.collider.name);
						}
						ammoBoxStopTimer = 0f;
					}
					else
					{
						LookAt(targetAmmoBox.transform.position, turnSpeed);
						if (ammoBoxStopTimer <= nextLootTime)
						{
							nextLootTime -= lootInterval;
							LootItem();
						}
					}
				}
				else if ((bool)targetAmmoBox.lootPoint)
				{
					LookAt(position + targetAmmoBox.lootPoint.transform.forward * 10f, turnSpeed);
				}
			}
			inputSimulator.firing = false;
			inputSimulator.aiming = false;
			inputSimulator.running = true;
		}
		else if (CurrentStatus == Status.fight_enemy && (bool)enemy)
		{
			if (isEnemyLost)
			{
				if (world_velocity.sqrMagnitude > 0.001f)
				{
					LookAt(position + world_velocity.normalized * 10f, turnSpeed);
				}
				inputSimulator.firing = false;
				inputSimulator.aiming = false;
				inputSimulator.running = true;
			}
			else
			{
				Vector3 characterUpperChestPos = GetCharacterUpperChestPos(enemy.GetComponent<ThirdPerson>());
				float num = 2f;
				if (aimingTimer <= 0f)
				{
					num = 4f;
				}
				LookAt(characterUpperChestPos, turnSpeed * num * bot_difficulty);
				flag = true;
				inputSimulator.firing = true;
				if (bot_difficulty < 1f)
				{
					inputSimulator.firing = false;
					if (Time.time >= nextFireDecisionTime)
					{
						nextFireDecisionTime = Time.time + Random.Range(0.15f, 0.35f);
						if (bot_difficulty >= 1f)
						{
							decidedFiring = true;
						}
						else
						{
							float num2 = Mathf.Lerp(0.2f, 1f, bot_difficulty);
							decidedFiring = Random.value <= num2;
						}
					}
					inputSimulator.firing = decidedFiring;
				}
				inputSimulator.reloading = NeedReload();
				if (debug)
				{
					Debug.Log(NeedReload().ToString());
				}
				if (Vector3.Distance(enemy.transform.position, base.transform.position) > 20f)
				{
					inputSimulator.aiming = true;
				}
				inputSimulator.running = false;
				if (inputSimulator.reloading)
				{
					flag = false;
				}
			}
		}
		if (lastEnemy != enemy)
		{
			flag = false;
		}
		if (flag)
		{
			aimingTimer -= Time.deltaTime;
			if (aimingTimer <= 0f)
			{
				aimingTimer = 0f;
			}
		}
		else
		{
			aimingTimer = aimPreciseDuration;
		}
		lastEnemy = enemy;
	}

	private void LookAt(Vector3 targetPos, float smooth)
	{
		Transform transform = thirdPerson.fps_camera.transform;
		_ = transform.position;
		Vector3 direction = targetPos - transform.position;
		Vector3 vector = new Vector3(direction.x, 0f, direction.z);
		if (vector.sqrMagnitude > 0.0001f)
		{
			Quaternion b = Quaternion.LookRotation(vector.normalized, Vector3.up);
			b = Quaternion.Slerp(base.transform.rotation, b, turnSpeed * Time.deltaTime);
			base.transform.rotation = b;
		}
		Vector3 vector2 = base.transform.InverseTransformDirection(direction);
		float value = (0f - Mathf.Atan2(vector2.y, vector2.z)) * 57.29578f;
		value = Mathf.Clamp(value, -89f, 89f);
		value = Mathf.Lerp(cameraLook.rotationPitchOverrided, value, turnSpeed * Time.deltaTime);
		cameraLook.rotationPitchOverrided = value;
	}

	private bool IsInsideSafeCircle(float marge = 0.9f)
	{
		Vector3 targetDamageZonePos = damageZone.GetTargetDamageZonePos();
		float num = damageZone.GetTargetDamageZoneRadius() * marge;
		targetDamageZonePos.y = base.transform.position.y;
		return Vector3.Distance(targetDamageZonePos, base.transform.position) < num;
	}

	private bool IsPointInsideCircle(Vector3 point, Vector3 pos, float radius)
	{
		pos.y = point.y;
		return Vector3.Distance(pos, point) <= radius;
	}

	public bool UpdateStatus(float trackDuration, float deltaTime)
	{
		Status currentStatus = CurrentStatus;
		GetVisibleEnemy();
		lootingToggleTimer -= deltaTime;
		if (lootingToggleTimer <= 0f)
		{
			lootingOnOff = !lootingOnOff;
			if (lootingOnOff)
			{
				lootingToggleTimer = lootingToggleOnDuration;
			}
			else
			{
				lootingToggleTimer = lootingToggleOffDuration;
			}
		}
		if (isEnemyLost)
		{
			trackTimer -= deltaTime;
			if ((bool)enemy && enemy.IsDead())
			{
				trackTimer = 0f;
			}
			if (trackTimer <= 0f)
			{
				trackTimer = 0f;
				if ((bool)enemy && isEnemyLost)
				{
					enemy = null;
				}
			}
		}
		if (CurrentStatus == Status.looting && !IsInsideSafeCircle(1f))
		{
			CurrentStatus = Status.go_to_circle;
		}
		else if (CurrentStatus == Status.go_to_circle && IsInsideSafeCircle())
		{
			CurrentStatus = Status.looting;
		}
		if (!lootingOnOff && CurrentStatus == Status.looting)
		{
			CurrentStatus = Status.go_to_circle;
		}
		if ((bool)enemy)
		{
			if (!isEnemyLost && !IsOutOfAmmo())
			{
				CurrentStatus = Status.fight_enemy;
				trackTimer = trackDuration;
			}
			if (isEnemyLost)
			{
				if (!IsInsideSafeCircle(1f) && CurrentStatus == Status.fight_enemy)
				{
					enemy = null;
					trackTimer = trackDuration;
					CurrentStatus = Status.go_to_circle;
				}
				else if (CurrentStatus != Status.fight_enemy)
				{
					CurrentStatus = Status.fight_enemy;
				}
			}
			if (IsOutOfAmmo() && CurrentStatus == Status.fight_enemy)
			{
				enemy = null;
				trackTimer = trackDuration;
				CurrentStatus = Status.go_to_circle;
			}
		}
		if (CurrentStatus == Status.fight_enemy && enemy == null)
		{
			trackTimer = trackDuration;
			isEnemyLost = true;
			CurrentStatus = Status.go_to_circle;
		}
		return CurrentStatus != currentStatus;
	}

	private AmmoBox GetTargetAmmoBox(Vector3 botPos, Vector3 circlePos, float circleRadius, List<AmmoBox> ammoBoxes, List<AmmoBox> excludeAmmoBoxes, bool exclude = false, int pickFromClosestN = 10)
	{
		float num = circleRadius * circleRadius;
		AmmoBox[] array = new AmmoBox[Mathf.Min(pickFromClosestN, ammoBoxes.Count)];
		int num2 = 0;
		float[] array2 = new float[array.Length];
		for (int i = 0; i < ammoBoxes.Count; i++)
		{
			AmmoBox ammoBox = ammoBoxes[i];
			if (exclude && excludeAmmoBoxes != null && excludeAmmoBoxes.Contains(ammoBox))
			{
				continue;
			}
			Vector3 vector = new Vector3(circlePos.x, ammoBox.transform.position.y, circlePos.z);
			if ((ammoBox.transform.position - vector).sqrMagnitude > num)
			{
				continue;
			}
			float sqrMagnitude = (ammoBox.transform.position - botPos).sqrMagnitude;
			if (num2 < array.Length)
			{
				array[num2] = ammoBox;
				array2[num2] = sqrMagnitude;
				num2++;
				continue;
			}
			int num3 = 0;
			float num4 = array2[0];
			for (int j = 1; j < num2; j++)
			{
				if (array2[j] > num4)
				{
					num3 = j;
					num4 = array2[j];
				}
			}
			if (sqrMagnitude < num4)
			{
				array[num3] = ammoBox;
				array2[num3] = sqrMagnitude;
			}
		}
		if (num2 == 0)
		{
			return null;
		}
		int num5 = Random.Range(0, num2);
		return array[num5];
	}

	private AmmoBox GetTargetAmmoBox(Vector3 circlePos, float circleRadius, Vector3 leaderPos, List<AmmoBox> ammoBoxes, List<AmmoBox> excludeAmmoBoxes, bool exclude = false, float nearLeaderRange = 30f)
	{
		float num = circleRadius * circleRadius;
		float num2 = nearLeaderRange * nearLeaderRange;
		AmmoBox[] array = new AmmoBox[ammoBoxes.Count];
		int num3 = 0;
		for (int i = 0; i < ammoBoxes.Count; i++)
		{
			AmmoBox ammoBox = ammoBoxes[i];
			if (!exclude || excludeAmmoBoxes == null || !excludeAmmoBoxes.Contains(ammoBox))
			{
				Vector3 position = ammoBox.transform.position;
				float sqrMagnitude = (position - circlePos).sqrMagnitude;
				float sqrMagnitude2 = (position - leaderPos).sqrMagnitude;
				if (sqrMagnitude <= num && sqrMagnitude2 <= num2)
				{
					array[num3] = ammoBox;
					num3++;
				}
			}
		}
		if (num3 == 0)
		{
			return null;
		}
		int num4 = Random.Range(0, num3);
		return array[num4];
	}

	private void LootWeapon()
	{
		if (debug)
		{
			Debug.Log("Loot weapon");
		}
		int[] array = new int[11]
		{
			0, 1, 2, 10, 11, 12, 13, 14, 15, 16,
			17
		};
		int num = Random.Range(0, array.Length);
		GetComponent<Character>().OnSetInventoryWeapon(array[num], cursorLockedInclude: true);
		Weapon weapon = (Weapon)GetComponent<Character>().GetInventory().GetWeapon(array[num]);
		if ((bool)weapon)
		{
			weapon.AddMags(100000);
		}
	}

	private void LootItem()
	{
		if (!characterParachute.isParachuting && (bool)targetAmmoBox)
		{
			if (debug)
			{
				Debug.Log("Loot Item");
			}
			int num = 0;
			if (characterInventory.vest == null && Random.value > 0.5f)
			{
				int[] array = new int[3] { 49, 50, 51 };
				num = Random.Range(0, array.Length);
				int num2 = array[num];
				characterInventory.vest = pickupsManager.items[num2];
				characterInventory.SetVest(characterInventory.vest.objName);
			}
			if (characterInventory.helmet == null && Random.value > 0.5f)
			{
				int[] array2 = new int[14]
				{
					58, 59, 60, 61, 62, 63, 64, 65, 66, 67,
					68, 69, 70, 71
				};
				num = Random.Range(0, array2.Length);
				int num3 = array2[num];
				characterInventory.helmet = pickupsManager.items[num3];
				characterInventory.SetHelmet(characterInventory.helmet.objName);
			}
			if (characterInventory.bag != null && characterInventory.bag.level == 1 && Random.value > 0.5f)
			{
				int[] array3 = new int[6] { 52, 53, 54, 55, 56, 57 };
				num = Random.Range(0, array3.Length);
				int num4 = array3[num];
				characterInventory.bag = pickupsManager.items[num4];
				characterInventory.SetBag(characterInventory.bag.objName);
			}
		}
	}

	private bool IsOutOfAmmo()
	{
		Weapon equippedWeapon = GetComponent<Character>().GetEquippedWeapon();
		if (((bool)equippedWeapon && equippedWeapon.GetAmmunitionCurrent() > 0) || equippedWeapon.GetCurrentMags() > 0)
		{
			return false;
		}
		return true;
	}

	private bool NeedReload()
	{
		Weapon equippedWeapon = GetComponent<Character>().GetEquippedWeapon();
		if ((bool)equippedWeapon && equippedWeapon.GetAmmunitionCurrent() <= 0 && equippedWeapon.GetCurrentMags() > 0)
		{
			return true;
		}
		return false;
	}

	private CharacterMultiplayer GetVisibleEnemy()
	{
		if (!canDoHardComputingThisFrame)
		{
			if ((bool)enemy && enemy.IsDead())
			{
				isEnemyLost = true;
			}
			return null;
		}
		if ((bool)enemy && CanSeeEnnemy(enemy))
		{
			trackTimer = trackDuration;
			isEnemyLost = false;
			return enemy;
		}
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if (character != characterMultiplayer && (bool)character && !characterMultiplayer.IsSquadMember(character) && CanSeeEnnemy(character))
			{
				trackTimer = trackDuration;
				enemy = character;
				isEnemyLost = false;
				return enemy;
			}
		}
		isEnemyLost = true;
		return null;
	}

	public static Vector3 GetCharacterUpperChestPos(ThirdPerson thirdPerson)
	{
		return thirdPerson.tps_animator.GetBoneTransform(HumanBodyBones.Head).position;
	}

	private bool IsInFOV(Vector3 target, Vector3 observer, Vector3 observer_forward, float angle, float distance)
	{
		Vector3 normalized = (target - observer).normalized;
		if (Vector3.Distance(observer, target) > distance)
		{
			return false;
		}
		if (Vector3.Angle(observer_forward, normalized) < angle)
		{
			return true;
		}
		return false;
	}

	private bool CanSeeEnnemy(CharacterMultiplayer e)
	{
		if (e == null)
		{
			return false;
		}
		if (e.isBot && !bot_vs_bot_kill)
		{
			return false;
		}
		if (!e.isBot && (e.IsDead() || e.GetComponent<CharacterParachute>().isParachuting || !e.GetComponent<ThirdPerson>().isActive))
		{
			return false;
		}
		if (e.isBot && !e.GetComponent<NavMeshAgent>().enabled)
		{
			return false;
		}
		Vector3 characterUpperChestPos = GetCharacterUpperChestPos(thirdPerson);
		if ((bool)e)
		{
			float num = fov_angle;
			float num2 = fov_dist;
			if (e.isBot)
			{
				num2 *= bot_vs_bot_difficulty * 0.5f;
				num *= bot_vs_bot_difficulty * 2f;
			}
			else
			{
				num2 *= bot_difficulty;
				num *= bot_difficulty;
			}
			Vector3 characterUpperChestPos2 = GetCharacterUpperChestPos(e.GetComponent<ThirdPerson>());
			if (IsInFOV(characterUpperChestPos2, characterUpperChestPos, base.transform.forward, num, num2) && !Physics.Linecast(characterUpperChestPos, characterUpperChestPos2, out var _, defaultAndGroundLayer, QueryTriggerInteraction.Ignore))
			{
				return true;
			}
		}
		return false;
	}

	public void Damage(int shooterActorId)
	{
		CharacterMultiplayer player = CharacterMultiplayer.GetPlayer(shooterActorId);
		if ((bool)player && (enemy == null || isEnemyLost) && enemy != player)
		{
			CurrentStatus = Status.fight_enemy;
			enemy = player;
			trackTimer = trackDuration;
			isEnemyLost = true;
			targetPoint = GetRandomPointAroundTarget(base.transform.position, 10f);
			SetDestination(targetPoint, 10f, 1f);
			if (!IsInvoking("GoToShooter"))
			{
				CancelInvoke("GoToShooter");
				Invoke("GoToShooter", 3f);
			}
		}
	}

	private void GoToShooter()
	{
		if (agent.enabled)
		{
			UpdateDestination(5f);
		}
	}

	private void CheckStuck()
	{
		Vector3 world_velocity = GetComponent<NetworkTransformSynch>().world_velocity;
		world_velocity.y = 0f;
		if (world_velocity.sqrMagnitude <= 0.001f)
		{
			if (ammoBoxStopTimer <= 0f)
			{
				stuckTimer -= Time.deltaTime;
				if (stuckTimer <= 0f)
				{
					stuckTimer = 0f;
					UpdateDestination(5f);
					if (debug)
					{
						Debug.LogWarning("stucking bot detected");
					}
				}
			}
			else
			{
				stuckTimer = stuckCheckDuration;
			}
		}
		else
		{
			stuckTimer = stuckCheckDuration;
		}
		if (!agent.hasPath && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathPartial)
		{
			if (debug)
			{
				Debug.LogWarning("try to fix a stuck bot !agent.hasPath && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathPartial");
			}
			if (ammoBoxStopTimer <= 0f)
			{
				UpdateDestination(15f);
			}
		}
	}
}
