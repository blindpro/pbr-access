using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterParachute : MonoBehaviour
{
	private Transform airplaneSpawn;

	private AirplaneManager airplaneManager;

	public bool isOnAirplane;

	public bool isParachuting;

	public bool isParachuteOpen;

	public Animator parachute;

	public Transform parachuteRotation;

	public bool canJumpFromPlane;

	public bool canOpenParachute;

	public float minHeightUrgentParachute = 100f;

	public float startCameraPitch = 35f;

	private CharacterMultiplayer characterMultiplayer;

	private float defaultFogDensity;

	private float defaultFogEnd;

	private Transform botTarget;

	private Vector3 botSpawn;

	private float botLandLerpTime;

	private static int botSafeLandPoint = 0;

	private float botGravity;

	private float botDuration;

	private float botSpeed = 10f;

	private float botArcHeight = -85f;

	private static List<Transform> botTargetsList = new List<Transform>();

	private CameraLook cameraLook;

	private CameraRaycast cameraRaycast;

	private NetworkTransformSynch transformSynch;

	private void Start()
	{
		airplaneManager = GameManager.Instance.GetComponent<AirplaneManager>();
		airplaneSpawn = GameManager.Instance.GetComponent<AirplaneManager>().spawnPoint;
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
		defaultFogDensity = RenderSettings.fogDensity;
		defaultFogEnd = RenderSettings.fogEndDistance;
		cameraLook = GetComponentInChildren<CameraLook>();
		cameraRaycast = GetComponentInChildren<CameraRaycast>();
		transformSynch = GetComponent<NetworkTransformSynch>();
	}

	private void Update()
	{
		if (!isOnAirplane)
		{
			if (characterMultiplayer.IsLocal() && !isParachuteOpen && base.transform.position.y < minHeightUrgentParachute)
			{
				OpenParachute();
			}
			if (characterMultiplayer.IsLocal() && characterMultiplayer.IsDead() && isParachuting)
			{
				EndParachuting();
				GetComponent<InputSimulator>().RPC_Action(8);
			}
			if (characterMultiplayer.IsLocal() && GetComponent<Movement>().IsGroundedApproximate() && isParachuting && !characterMultiplayer.isBot && canOpenParachute)
			{
				EndParachuting();
				GetComponent<InputSimulator>().RPC_Action(8);
			}
			if (characterMultiplayer.isBot && characterMultiplayer.isLocal && isParachuting)
			{
				float num = 20f;
				float num2 = 10f;
				Vector3 position = botTarget.position;
				position.y = base.transform.position.y;
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation((position - base.transform.position).normalized, base.transform.up), Time.deltaTime * num);
				Vector3 vector = botTarget.position - base.transform.position;
				base.transform.position += vector.normalized * Time.deltaTime * num2;
				float num3 = 1f;
				if (isParachuteOpen)
				{
					num3 = 0f;
				}
				botGravity -= Time.deltaTime * num3;
				Vector3 position2 = base.transform.position;
				position2.y += Time.deltaTime * botGravity;
				base.transform.position = position2;
				if (Vector3.Distance(base.transform.position, botTarget.position) < 1f)
				{
					base.transform.position = botTarget.position;
					EndParachuting();
					GetComponent<InputSimulator>().RPC_Action(8);
				}
			}
			if (!isParachuting)
			{
				isParachuteOpen = false;
				parachuteRotation.localRotation = Quaternion.identity;
			}
			if (isParachuteOpen && !parachute.gameObject.activeInHierarchy)
			{
				parachute.gameObject.SetActive(value: true);
			}
		}
		UpdateParachuteRotation();
	}

	public void OnMatchStarted()
	{
		base.transform.rotation = Quaternion.identity;
		isOnAirplane = true;
		isParachuting = true;
		isParachuteOpen = false;
		canJumpFromPlane = false;
		canOpenParachute = false;
		parachuteRotation.localRotation = Quaternion.identity;
		botLandLerpTime = 0f;
		botGravity = 0f;
		parachute.gameObject.SetActive(value: false);
		StartCoroutine(AllowJumpFromPlane(20f));
		cameraLook.transform.localRotation = Quaternion.Euler(startCameraPitch, 0f, 0f);
		airplaneManager.parachuteClothLoopAudio.Stop();
		airplaneManager.parachuteWindLoopAudio.Stop();
		airplaneManager.parachuteJumpAudio.Stop();
		airplaneManager.parachuteLandAudio.Stop();
		airplaneManager.parachuteOpenAudio.Stop();
		if (characterMultiplayer.IsLocalMainPlayer())
		{
			RenderSettings.fogDensity = airplaneManager.airplaneFogDensity;
			RenderSettings.fogEndDistance = airplaneManager.airplaneFogEnd;
			botSafeLandPoint = Random.Range(0, airplaneManager.safeBotsLandPoints.Length);
			botTargetsList.Clear();
			Transform[] safeBotsLandPoints = airplaneManager.safeBotsLandPoints;
			foreach (Transform item in safeBotsLandPoints)
			{
				botTargetsList.Add(item);
			}
		}
		if ((bool)cameraRaycast)
		{
			cameraRaycast.OnMatchStarted();
		}
	}

	public void OnMatchFinished()
	{
	}

	public void JumpFromPlane()
	{
		if (characterMultiplayer.IsLocal() && canJumpFromPlane && isOnAirplane)
		{
			isOnAirplane = false;
			botSpawn = airplaneSpawn.position;
			botLandLerpTime = 0f;
			botGravity = 0f;
			if (characterMultiplayer.isBot)
			{
				float num = Vector3.Distance(botSpawn, botTarget.position);
				botDuration = num / botSpeed;
			}
			SetCharacterPosition(airplaneSpawn.position);
			GetComponent<Movement>().ComputeIsInAir();
			GetComponent<InputSimulator>().RPC_Action(9);
			StartCoroutine(AllowOpenParachute(10f));
			if (characterMultiplayer.IsLocalMainPlayer())
			{
				airplaneManager.parachuteClothLoopAudio.Play();
				airplaneManager.parachuteWindLoopAudio.Play();
				airplaneManager.parachuteJumpAudio.Play();
				RenderSettings.fogDensity = defaultFogDensity;
				RenderSettings.fogEndDistance = defaultFogEnd;
				characterMultiplayer.transform.rotation = Quaternion.Euler(0f, airplaneManager.AirplaneCamera.transform.rotation.eulerAngles.y, 0f);
			}
		}
		if (!characterMultiplayer.IsLocal() && canJumpFromPlane && isOnAirplane)
		{
			botGravity = 0f;
			StartCoroutine(AllowOpenParachute(10f));
		}
	}

	public void OpenParachute()
	{
		if (characterMultiplayer.IsLocal() && !characterMultiplayer.IsDead() && canOpenParachute && isParachuting && !isParachuteOpen)
		{
			isParachuteOpen = true;
			botGravity = 0f;
			GetComponent<InputSimulator>().RPC_Action(7);
			if (characterMultiplayer.IsLocalMainPlayer())
			{
				airplaneManager.parachuteOpenAudio.Play();
			}
		}
	}

	public void EndParachuting()
	{
		isParachuting = false;
		if (characterMultiplayer.IsLocalMainPlayer())
		{
			airplaneManager.parachuteClothLoopAudio.Stop();
			airplaneManager.parachuteWindLoopAudio.Stop();
			airplaneManager.parachuteLandAudio.Play();
		}
		ForceHideParachute();
	}

	private IEnumerator AllowJumpFromPlane(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		canJumpFromPlane = true;
		canOpenParachute = false;
		if (characterMultiplayer.isBot)
		{
			StartCoroutine(BotJump(Random.Range(5, 60)));
		}
	}

	private IEnumerator AllowOpenParachute(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		canJumpFromPlane = true;
		canOpenParachute = true;
	}

	private IEnumerator BotJump(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		if (botTargetsList.Count > 0)
		{
			int index = Random.Range(0, botTargetsList.Count);
			botTarget = botTargetsList[index];
			botTargetsList.RemoveAt(index);
		}
		else
		{
			Debug.LogError("No more parachute bot targets to pick!");
		}
		JumpFromPlane();
	}

	private void SetCharacterPosition(Vector3 newPosition)
	{
		GetComponent<CharacterController>().enabled = false;
		GetComponent<CharacterController>().transform.position = newPosition;
		GetComponent<CharacterController>().enabled = true;
	}

	public void HideParachute()
	{
		parachute.gameObject.SetActive(value: false);
	}

	public void ForceHideParachute()
	{
		isParachuting = false;
		CancelInvoke("HideParachute");
		Invoke("HideParachute", 1f);
	}

	private void UpdateParachuteRotation()
	{
		if (characterMultiplayer.isBot)
		{
			return;
		}
		Quaternion b = Quaternion.identity;
		float num = 5f;
		if (isParachuting && isParachuteOpen)
		{
			num = 0.5f;
			Vector3 world_velocity = transformSynch.world_velocity;
			world_velocity.y = 0f;
			Vector3 vector = base.transform.InverseTransformDirection(world_velocity);
			float num2 = 3.5f;
			float num3 = 0f;
			if (vector.x > num2)
			{
				num3 = 10f;
			}
			else if (vector.x < 0f - num2)
			{
				num3 = -10f;
			}
			float x = 0f;
			if (vector.z > num2)
			{
				x = 10f;
			}
			else if (vector.z < 0f - num2)
			{
				x = -10f;
			}
			b = Quaternion.Euler(x, 0f, 0f - num3);
		}
		parachuteRotation.localRotation = Quaternion.Slerp(parachuteRotation.localRotation, b, Time.deltaTime * num);
	}
}
