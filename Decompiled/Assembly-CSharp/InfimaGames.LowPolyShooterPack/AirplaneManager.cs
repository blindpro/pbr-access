using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class AirplaneManager : MonoBehaviour
{
	public Transform airplaneRotation;

	public Camera AirplaneCamera;

	public GameObject Airplane;

	public Transform targetPos;

	public Transform spawnPoint;

	public Vector3 startRotation;

	public Transform cameraY;

	public Transform cameraX;

	public Transform[] engines;

	public Vector3 cameraStartLocalPos;

	public Vector3 cameraEndLocalPos;

	public float speed = 1f;

	public float lerpSpeed = 1f;

	public float enginesTurnSpeed = 10f;

	public float airplaneFogDensity = 0.001f;

	public float airplaneFogEnd = 2000f;

	public bool parachuting;

	public CanvasGroup miniMapSmall;

	public AudioSource parachuteWindLoopAudio;

	public AudioSource parachuteClothLoopAudio;

	public AudioSource parachuteOpenAudio;

	public AudioSource parachuteLandAudio;

	public AudioSource parachuteJumpAudio;

	public Transform[] safeBotsLandPoints;

	public GameObject airplaneTutorial;

	private Vector3 defaultPos;

	private Vector3 defaultRot;

	private float lerpTime;

	private bool targetAchieved;

	private SettingsManager settingsManager;

	private void Start()
	{
		settingsManager = GetComponent<SettingsManager>();
		NavmeshPoint[] array = Object.FindObjectsOfType<NavmeshPoint>(includeInactive: true);
		safeBotsLandPoints = new Transform[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			safeBotsLandPoints[i] = array[i].transform;
		}
		Airplane.gameObject.SetActive(value: false);
		parachuting = false;
		defaultPos = Airplane.transform.localPosition;
		defaultRot = new Vector3(cameraX.localRotation.eulerAngles.x, cameraY.localRotation.eulerAngles.y, 0f);
	}

	private void Update()
	{
		bool active = false;
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			Airplane.transform.position = Airplane.transform.position + Airplane.transform.forward * speed * Time.deltaTime;
			Vector3 position = targetPos.position;
			position.y = Airplane.transform.position.y;
			if (Vector3.Distance(position, Airplane.transform.position) < 1f && !targetAchieved)
			{
				targetAchieved = true;
				foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
				{
					if (character.IsLocal())
					{
						character.GetComponent<CharacterParachute>().JumpFromPlane();
					}
				}
			}
			if (lerpTime <= 1f)
			{
				cameraY.localRotation = Quaternion.Euler(0f, Mathf.Lerp(startRotation.y, defaultRot.y, lerpTime), 0f);
				cameraX.localRotation = Quaternion.Euler(Mathf.Lerp(startRotation.x, defaultRot.x, lerpTime), 0f, 0f);
				AirplaneCamera.transform.localPosition = Vector3.Lerp(cameraStartLocalPos, cameraEndLocalPos, lerpTime);
				miniMapSmall.alpha = 0f;
			}
			else
			{
				CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
				if ((bool)mainPlayer)
				{
					float num = 1f;
					Vector2 mouseAxisLook = mainPlayer.GetComponent<Character>().mouseAxisLook;
					mouseAxisLook *= settingsManager.SensibbilitySlider.value;
					if (settingsManager.inverseMouseX.isOn)
					{
						mouseAxisLook.x *= -1f;
					}
					if (settingsManager.inverseMouseY.isOn)
					{
						mouseAxisLook.y *= -1f;
					}
					if (GameManager.Instance.InGameMenu.activeSelf)
					{
						mouseAxisLook *= 0f;
					}
					cameraY.localRotation = Quaternion.Euler(0f, cameraY.localRotation.eulerAngles.y + num * mouseAxisLook.x, 0f);
					cameraX.localRotation = Quaternion.Euler(Mathf.Clamp(cameraX.localRotation.eulerAngles.x - num * mouseAxisLook.y, 30f, 90f), 0f, 0f);
				}
				miniMapSmall.alpha = 1f;
			}
			lerpTime += lerpSpeed * Time.deltaTime;
			if (lerpTime > 1f)
			{
				lerpTime = 1.01f;
			}
			Transform[] array = engines;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Rotate(new Vector3(0f, 0f, Time.deltaTime * enginesTurnSpeed), Space.Self);
			}
			active = AirplaneCamera.enabled && !GameManager.Instance.InGameMenu.activeSelf;
		}
		Statics.SetActive(airplaneTutorial, active);
	}

	public void OnMatchStarted()
	{
		targetAchieved = false;
		parachuting = true;
		Airplane.gameObject.SetActive(value: true);
		Airplane.transform.localPosition = defaultPos;
		AirplaneCamera.gameObject.SetActive(value: true);
		lerpTime = 0f;
	}

	public void OnMatchFinished()
	{
		Airplane.gameObject.SetActive(value: false);
	}

	private void ParachuteAll()
	{
	}
}
