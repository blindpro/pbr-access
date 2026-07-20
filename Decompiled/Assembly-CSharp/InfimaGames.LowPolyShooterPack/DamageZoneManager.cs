using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class DamageZoneManager : MonoBehaviour
{
	public Transform damageZone;

	public Transform target_damageZone;

	public Transform damageZoneDefault;

	public CanvasGroup safeZoneAppearsMsg;

	public CanvasGroup safeZoneShrinkInMsg;

	public CanvasGroup safeZoneShrinkingMsg;

	public Text appearsInTimerTxt;

	public Text shrinkInTimerTxt;

	public float appearsInDuration = 60f;

	public float shrinkInDuration = 50f;

	public float shrinkInDurationFirst = 120f;

	public float shrinkingDuration = 120f;

	public float appearsInDelay = 20f;

	public float hitPlayersDuration = 5f;

	public byte hitPlayersDamage = 10;

	private Vector3 defaultPos;

	private Vector3 defaultScale;

	private float appearsInTimer;

	private float shrinkInTimer;

	private float shrinkingTimer;

	private float hitPlayersTimer;

	private bool matchPlaying;

	private CapsuleCollider damageZoneCapsule;

	private CapsuleCollider target_damageZoneCapsule;

	private CapsuleCollider damageZoneDefaultCapsule;

	private Vector3 damageZoneStartScale;

	private Vector3 damageZoneOldScale;

	private Vector3 damageZoneOldPos;

	private void Start()
	{
		damageZoneCapsule = damageZone.GetComponent<CapsuleCollider>();
		target_damageZoneCapsule = target_damageZone.GetComponent<CapsuleCollider>();
		damageZoneDefaultCapsule = damageZoneDefault.GetComponent<CapsuleCollider>();
		damageZoneStartScale = damageZone.localScale;
		damageZone.gameObject.SetActive(value: false);
		target_damageZone.gameObject.SetActive(value: false);
		defaultPos = damageZoneDefault.position;
		defaultScale = damageZoneDefault.localScale;
		safeZoneAppearsMsg.gameObject.SetActive(value: true);
		safeZoneShrinkInMsg.gameObject.SetActive(value: true);
		safeZoneShrinkingMsg.gameObject.SetActive(value: true);
		safeZoneAppearsMsg.alpha = 0f;
		safeZoneShrinkInMsg.alpha = 0f;
		safeZoneShrinkingMsg.alpha = 0f;
	}

	private void Update()
	{
		bool flag = false;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer && mainPlayer.isMasterServer)
		{
			flag = true;
		}
		if (flag && matchPlaying)
		{
			hitPlayersTimer -= Time.deltaTime;
			if (hitPlayersTimer <= 0f)
			{
				hitPlayersTimer = hitPlayersDuration;
				HitPlayers();
			}
		}
		else
		{
			hitPlayersTimer = hitPlayersDuration;
		}
		if (!matchPlaying)
		{
			return;
		}
		if (appearsInTimer > 0f)
		{
			appearsInTimer -= Time.deltaTime;
			shrinkInTimer = 0f;
			shrinkingTimer = 0f;
			appearsInTimerTxt.text = Statics.ElapsedTimeToTimeFormat(appearsInTimer);
			if (appearsInTimer <= 0f)
			{
				appearsInTimer = 0f;
				if (flag)
				{
					RPC_UpdateDamageZone(1, 1);
				}
			}
		}
		if (shrinkInTimer > 0f)
		{
			appearsInTimer = 0f;
			shrinkInTimer -= Time.deltaTime;
			shrinkingTimer = 0f;
			shrinkInTimerTxt.text = Statics.ElapsedTimeToTimeFormat(shrinkInTimer);
			if (shrinkInTimer <= 0f)
			{
				shrinkInTimer = 0f;
				if (flag)
				{
					RPC_UpdateDamageZone(2, 0);
				}
			}
		}
		if (!(shrinkingTimer > 0f))
		{
			return;
		}
		appearsInTimer = 0f;
		shrinkInTimer = 0f;
		shrinkingTimer -= Time.deltaTime;
		float num = Mathf.Clamp01(shrinkingTimer / shrinkingDuration);
		damageZone.position = Vector3.Lerp(damageZoneOldPos, target_damageZone.position, 1f - num);
		damageZone.localScale = Vector3.Lerp(damageZoneOldScale, target_damageZone.localScale, 1f - num);
		if (shrinkingTimer <= 0f)
		{
			shrinkingTimer = 0f;
			if (flag)
			{
				RPC_UpdateDamageZone(1, 0);
			}
		}
	}

	public void OnMatchStarted()
	{
		damageZone.gameObject.SetActive(value: false);
		target_damageZone.gameObject.SetActive(value: false);
		damageZone.position = defaultPos;
		damageZone.localScale = damageZoneStartScale;
		target_damageZone.position = defaultPos;
		target_damageZone.localScale = defaultScale;
		safeZoneAppearsMsg.alpha = 0f;
		safeZoneShrinkInMsg.alpha = 0f;
		safeZoneShrinkingMsg.alpha = 0f;
		appearsInTimer = 0f;
		shrinkInTimer = 0f;
		shrinkingTimer = 0f;
		matchPlaying = false;
		CharacterMultiplayer.GetMainPlayer();
		CancelInvoke("RPC_OnShowAppearsInMsg");
		Invoke("RPC_OnShowAppearsInMsg", appearsInDelay);
	}

	public void OnMatchFinished()
	{
		damageZone.gameObject.SetActive(value: false);
		target_damageZone.gameObject.SetActive(value: false);
		safeZoneAppearsMsg.alpha = 0f;
		safeZoneShrinkInMsg.alpha = 0f;
		safeZoneShrinkingMsg.alpha = 0f;
		appearsInTimer = 0f;
		shrinkInTimer = 0f;
		shrinkingTimer = 0f;
		matchPlaying = false;
		CancelInvoke("RPC_OnShowAppearsInMsg");
	}

	public void OnDead()
	{
	}

	public Vector3 GetTargetDamageZonePos()
	{
		return target_damageZone.position;
	}

	public float GetTargetDamageZoneRadius()
	{
		return target_damageZone.GetComponent<CapsuleCollider>().radius * target_damageZone.localScale.x;
	}

	public bool IsShrinking()
	{
		return safeZoneShrinkingMsg.alpha > 0.5f;
	}

	public void OnShowAppearsInMsg()
	{
		appearsInTimer = appearsInDuration;
		shrinkInTimer = 0f;
		shrinkingTimer = 0f;
		matchPlaying = true;
		safeZoneAppearsMsg.alpha = 1f;
		safeZoneShrinkInMsg.alpha = 0f;
		safeZoneShrinkingMsg.alpha = 0f;
	}

	public void OnShowShrinkInMsg()
	{
		if (matchPlaying)
		{
			damageZone.gameObject.SetActive(value: true);
			target_damageZone.gameObject.SetActive(value: true);
			shrinkInTimer = shrinkInDuration;
			appearsInTimer = 0f;
			shrinkingTimer = 0f;
			safeZoneAppearsMsg.alpha = 0f;
			safeZoneShrinkInMsg.alpha = 1f;
			safeZoneShrinkingMsg.alpha = 0f;
		}
	}

	public void OnShowShrinkingMsg()
	{
		if (matchPlaying)
		{
			shrinkingTimer = shrinkingDuration;
			appearsInTimer = 0f;
			shrinkInTimer = 0f;
			safeZoneAppearsMsg.alpha = 0f;
			safeZoneShrinkInMsg.alpha = 0f;
			safeZoneShrinkingMsg.alpha = 1f;
			damageZoneOldPos = damageZone.position;
			damageZoneOldScale = damageZone.localScale;
		}
	}

	public void ReduceTargetZoneCircle()
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer && mainPlayer.isMasterServer)
		{
			GetRandomCircle(target_damageZone.position, target_damageZoneCapsule.radius * target_damageZone.localScale.x, out var outPos, out var outRadius);
			target_damageZone.position = outPos;
			float num = outRadius / target_damageZoneCapsule.radius;
			target_damageZone.localScale = new Vector3(num, target_damageZone.localScale.y, num);
		}
	}

	private void RPC_UpdateDamageZone(byte actionId, byte isFirstTime = 0)
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer && mainPlayer.isMasterServer)
		{
			MatchmakingManager.Instance.RPC_UpdateDamageZone(actionId, isFirstTime);
		}
	}

	public void RPC_OnShowAppearsInMsg()
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer && mainPlayer.isMasterServer)
		{
			MatchmakingManager.Instance.RPC_UpdateDamageZone(0, 1);
		}
	}

	private void HitPlayers()
	{
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if ((bool)character && !character.IsDead() && !character.IsInsideSafeZone() && character.GetComponent<ThirdPerson>().isActive)
			{
				character.RPC_Damage(hitPlayersDamage, 0);
			}
		}
	}

	private void GetRandomCircle(Vector3 inPos, float inRadius, out Vector3 outPos, out float outRadius)
	{
		outRadius = inRadius * 0.6f;
		float maxInclusive = inRadius - outRadius;
		Vector3 vector = Random.insideUnitCircle.normalized;
		float num = Random.Range(0f, maxInclusive);
		outPos = inPos + vector * num;
		outPos.y = inPos.y;
	}
}
