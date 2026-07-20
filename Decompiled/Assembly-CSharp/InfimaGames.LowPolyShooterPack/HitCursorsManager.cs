using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class HitCursorsManager : MonoBehaviour
{
	public CanvasGroup hitCursor;

	public CanvasGroup damageCursor;

	public CanvasGroup killScorePanel;

	public CanvasGroup killsTxt;

	public CanvasGroup killsExpTxt;

	public CanvasGroup killsGPTxt;

	public float hitCursorDuration = 1f;

	public float damageCursorDuration = 2f;

	public float killsScoreDuration = 1f;

	public Vector2 hitCursorScaleMinMax = new Vector2(0.56f, 0.7f);

	public int hitAudioSource;

	public int boxOpenAudioSource = 1;

	public int lootAudioSource = 2;

	public int dropAudioSource = 3;

	public int upgradedAudioSource = 4;

	public int crouchAudioSource = 5;

	private float hitCursorTimer;

	private float damageCursorTimer;

	private float killScoreTimer;

	private byte shooterActorId;

	private AudioSource[] audioSources;

	private void Start()
	{
		audioSources = GetComponents<AudioSource>();
		hitCursor.gameObject.SetActive(value: true);
		damageCursor.gameObject.SetActive(value: true);
		killScorePanel.gameObject.SetActive(value: true);
		hitCursor.alpha = 0f;
		damageCursor.alpha = 0f;
		killScorePanel.alpha = 1f;
		killsTxt.alpha = 0f;
		killsExpTxt.alpha = 0f;
		killsGPTxt.alpha = 0f;
	}

	private void Update()
	{
		hitCursorTimer -= Time.deltaTime;
		damageCursorTimer -= Time.deltaTime;
		killScoreTimer -= Time.deltaTime;
		if (hitCursorTimer < 0f)
		{
			hitCursorTimer = 0f;
		}
		if (damageCursorTimer < 0f)
		{
			damageCursorTimer = 0f;
		}
		if (killScoreTimer < 0f)
		{
			killScoreTimer = 0f;
		}
		float num = hitCursorDuration / 2f;
		if (hitCursorTimer < num)
		{
			hitCursor.alpha = Mathf.Lerp(1f, 0f, 1f - hitCursorTimer / num);
			float x = hitCursorScaleMinMax.x;
			hitCursor.GetComponent<RectTransform>().localScale = new Vector3(x, x, x);
		}
		else
		{
			hitCursor.alpha = 1f;
			float num2 = hitCursorDuration / 10f;
			float num3 = hitCursorDuration - num2;
			float num4 = hitCursorDuration - num2 * 2f;
			if (hitCursorTimer > num3)
			{
				float num5 = Mathf.Lerp(hitCursorScaleMinMax.x, hitCursorScaleMinMax.y, 1f - (hitCursorTimer - num3) / num2);
				hitCursor.GetComponent<RectTransform>().localScale = new Vector3(num5, num5, num5);
			}
			else if (hitCursorTimer > num4)
			{
				float num6 = Mathf.Lerp(hitCursorScaleMinMax.y, hitCursorScaleMinMax.x, 1f - (hitCursorTimer - num4) / num2);
				hitCursor.GetComponent<RectTransform>().localScale = new Vector3(num6, num6, num6);
			}
		}
		num = damageCursorDuration / 2f;
		if (damageCursorTimer < num)
		{
			damageCursor.alpha = Mathf.Lerp(1f, 0f, 1f - damageCursorTimer / num);
		}
		else
		{
			damageCursor.alpha = 1f;
		}
		UpdateDamageCursor();
		num = killsScoreDuration / 2f;
		if (killScoreTimer < num)
		{
			killsTxt.alpha = Mathf.Lerp(1f, 0f, 1f - killScoreTimer / num);
			killsExpTxt.alpha = Mathf.Lerp(1f, 0f, 1f - killScoreTimer / num);
			killsGPTxt.alpha = Mathf.Lerp(1f, 0f, 1f - killScoreTimer / num);
			return;
		}
		float num7 = killsScoreDuration / 10f;
		float num8 = killsScoreDuration - num7;
		float num9 = killsScoreDuration - num7 * 2f;
		float num10 = killsScoreDuration - num7 * 3f;
		if (killScoreTimer > num8)
		{
			killsTxt.alpha = Mathf.Lerp(0f, 1f, 1f - (killScoreTimer - num8) / num7);
		}
		else if (killScoreTimer > num9)
		{
			killsExpTxt.alpha = Mathf.Lerp(0f, 1f, 1f - (killScoreTimer - num9) / num7);
		}
		else if (killScoreTimer > num10)
		{
			killsGPTxt.alpha = Mathf.Lerp(0f, 1f, 1f - (killScoreTimer - num10) / num7);
		}
	}

	public void ShowHitCursor()
	{
		hitCursorTimer = hitCursorDuration;
		PlayHitSound();
	}

	public void ShowDamageCursor(byte _shooterActorId)
	{
		shooterActorId = _shooterActorId;
		damageCursorTimer = damageCursorDuration;
		PlayHitSound();
	}

	public void ShowKillScore()
	{
		killScoreTimer = killsScoreDuration;
		killsTxt.alpha = 0f;
		killsExpTxt.alpha = 0f;
		killsGPTxt.alpha = 0f;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			killsTxt.GetComponent<Text>().text = "+" + mainPlayer.kills;
			Text component = killsExpTxt.GetComponent<Text>();
			int kills = mainPlayer.kills;
			component.text = "+" + kills;
			killsGPTxt.GetComponent<Text>().text = "+" + mainPlayer.kills * GameManager.Instance.GetComponent<ProgressionManager>().kill_to_gp;
			Weapon equippedWeapon = mainPlayer.GetComponent<Character>().GetEquippedWeapon();
			if ((bool)equippedWeapon)
			{
				string weaponShortDesc = GameManager.Instance.GetComponent<PickupsManager>().GetWeaponShortDesc(equippedWeapon.GetWeaponName());
				GameManager.Instance.GetComponent<ProgressionManager>().AddWeaponExp(weaponShortDesc);
			}
			else
			{
				Debug.LogWarning("ShowKillScore GetEquippedWeapon null");
			}
		}
	}

	private void UpdateDamageCursor()
	{
		CharacterMultiplayer player = CharacterMultiplayer.GetPlayer(shooterActorId);
		if (!player)
		{
			return;
		}
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			ThirdPerson component = mainPlayer.GetComponent<ThirdPerson>();
			if ((bool)component)
			{
				Camera camera = ((!component.tps_camera_on) ? component.fps_camera : component.tps_camera);
				Transform transform = camera.transform;
				Vector3 position = player.transform.position;
				position.y = transform.position.y;
				Vector3 normalized = (position - transform.position).normalized;
				float num = Vector3.SignedAngle(transform.transform.forward, normalized, Vector3.up);
				RectTransform component2 = damageCursor.GetComponent<RectTransform>();
				Vector3 eulerAngles = component2.eulerAngles;
				eulerAngles.z = 0f - num;
				component2.eulerAngles = eulerAngles;
			}
		}
	}

	public void PlayHitSound()
	{
		AudioSource obj = audioSources[hitAudioSource];
		obj.PlayOneShot(obj.clip);
	}

	public void PlayBoxOpenSound()
	{
		AudioSource obj = audioSources[boxOpenAudioSource];
		obj.PlayOneShot(obj.clip);
	}

	public void PlayLootSound()
	{
		AudioSource obj = audioSources[lootAudioSource];
		obj.PlayOneShot(obj.clip);
	}

	public void PlayDropSound()
	{
		AudioSource obj = audioSources[dropAudioSource];
		obj.PlayOneShot(obj.clip);
	}

	public void PlayUpGradedSound()
	{
		AudioSource obj = audioSources[upgradedAudioSource];
		obj.PlayOneShot(obj.clip);
	}

	public void PlayCrouchSound()
	{
		AudioSource obj = audioSources[crouchAudioSource];
		obj.PlayOneShot(obj.clip);
	}
}
