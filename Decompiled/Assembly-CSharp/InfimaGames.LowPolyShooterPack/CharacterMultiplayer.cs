using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack.Interface;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterMultiplayer : MonoBehaviourPun, IPunInstantiateMagicCallback, IPunObservable
{
	public Text NicknameTxt;

	public Slider HealthSld;

	public string Nickname = "";

	public int ActorNumber;

	public int SquadId;

	public bool isMasterServer;

	public bool isLocal;

	public bool isMainPlayer;

	public bool isBot;

	public bool isSpectating;

	public bool isBotSquadLeader;

	public byte health = byte.MaxValue;

	public int kills;

	public int deaths;

	public byte match_rank;

	public static List<CharacterMultiplayer> characters = new List<CharacterMultiplayer>();

	public List<CharacterMultiplayer> squad = new List<CharacterMultiplayer>();

	public bool isHealing;

	public float healingSpeed = 1f;

	public byte healing_add;

	private bool isDead;

	private RagDollController ragDollController;

	private Character _character;

	private PostProcessVolume volume;

	private ColorGrading colorGrading;

	private KillsLogManager killsLog;

	private AmmoBox dropedAmmoBox;

	private Image healingImg;

	private NetworkTransformSynch transformSynch;

	private RecoilMotion[] recoilMotions;

	public float DefaultRecoil = 0.5f;

	public float GripRecoil = 0.2f;

	public void SetRecoil(float recoil)
	{
		Debug.Log("Set Recoil " + base.name + " " + recoil);
		RecoilMotion[] array = recoilMotions;
		foreach (RecoilMotion recoilMotion in array)
		{
			if ((bool)recoilMotion && recoilMotion.enabled)
			{
				recoilMotion.SetAlpha(recoil);
			}
		}
	}

	public void SetRecoil(bool hasgGrip, Weapon weapon)
	{
		if (weapon.gameObject.activeSelf)
		{
			Debug.Log("Set Recoil " + base.name + " " + hasgGrip + " " + weapon.GetWeaponName());
			if (hasgGrip)
			{
				SetRecoil(GripRecoil);
			}
			else
			{
				SetRecoil(DefaultRecoil);
			}
		}
	}

	private void Awake()
	{
		recoilMotions = GetComponentsInChildren<RecoilMotion>();
		ragDollController = GetComponent<RagDollController>();
		_character = GetComponent<Character>();
		transformSynch = GetComponent<NetworkTransformSynch>();
		Add();
	}

	private void Start()
	{
		recoilMotions = GetComponentsInChildren<RecoilMotion>();
		SetRecoil(DefaultRecoil);
		killsLog = GameManager.Instance.GetComponent<KillsLogManager>();
		volume = Object.FindObjectOfType<PostProcessVolume>();
		healingImg = GameManager.Instance.HealingImg;
		if ((bool)volume && volume.enabled && (bool)volume.profile)
		{
			volume.profile.TryGetSettings<ColorGrading>(out colorGrading);
		}
	}

	public void Restart()
	{
		isDead = false;
		ragDollController.EnableRagDoll(enable: false);
		health = byte.MaxValue;
		kills = 0;
		deaths = 0;
		match_rank = 1;
		isHealing = false;
		if ((bool)dropedAmmoBox)
		{
			Object.Destroy(dropedAmmoBox);
			dropedAmmoBox = null;
		}
		if (isMainPlayer)
		{
			Statics.SetActive(healingImg.transform.parent.gameObject, active: false);
		}
	}

	private void Update()
	{
		if (Application.isEditor && Input.GetKeyDown(KeyCode.U) && !isDead && IsLocalMainPlayer())
		{
			RPC_Dead(0);
		}
		if (Application.isEditor && Input.GetKeyDown(KeyCode.I) && isDead)
		{
			RPC_Respawn();
		}
		if (Application.isEditor && Input.GetKeyDown(KeyCode.L) && IsLocalMainPlayer() && isMasterServer)
		{
			MatchmakingManager.Instance.RPC_MatchFinished();
		}
		if (Application.isEditor && Input.GetKeyDown(KeyCode.N) && IsLocalMainPlayer() && isMasterServer)
		{
			Time.timeScale = 1f - Time.timeScale;
		}
		isLocal = base.photonView.IsMine;
		isMainPlayer = IsLocalMainPlayer();
		isMasterServer = PhotonNetwork.IsMasterClient && isMainPlayer;
		Animator characterAnimator = _character.GetCharacterAnimator();
		Animator tps_animator = _character.GetComponent<ThirdPerson>().tps_animator;
		if (!isMainPlayer)
		{
			if (GetComponent<PlayerInput>().enabled)
			{
				GetComponent<PlayerInput>().enabled = false;
			}
			if (GetComponent<CanvasSpawner>().enabled)
			{
				GetComponent<CanvasSpawner>().enabled = false;
			}
			if (GetComponent<TimeHandler>().enabled)
			{
				GetComponent<TimeHandler>().enabled = false;
			}
			if (isSpectating)
			{
				if ((bool)characterAnimator && characterAnimator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
				{
					characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				}
				if ((bool)tps_animator && tps_animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
				{
					tps_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				}
			}
			else
			{
				if ((bool)characterAnimator && characterAnimator.cullingMode != AnimatorCullingMode.CullUpdateTransforms)
				{
					characterAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
				}
				if ((bool)tps_animator && tps_animator.cullingMode != AnimatorCullingMode.CullUpdateTransforms)
				{
					tps_animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
				}
			}
		}
		else
		{
			if (!GetComponent<PlayerInput>().enabled)
			{
				GetComponent<PlayerInput>().enabled = true;
			}
			if (!GetComponent<CanvasSpawner>().enabled)
			{
				GetComponent<CanvasSpawner>().enabled = true;
			}
			if (!GetComponent<TimeHandler>().enabled)
			{
				GetComponent<TimeHandler>().enabled = true;
			}
			if ((bool)characterAnimator && characterAnimator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
			{
				characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			}
			if ((bool)tps_animator && tps_animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
			{
				tps_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			}
		}
		NicknameTxt.text = Nickname;
		HealthSld.value = (float)(int)health / 255f;
		UpdateHealing();
		if (!colorGrading || (!isMainPlayer && !isSpectating) || IsDead() || MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
		{
			return;
		}
		if (IsInsideSafeZone())
		{
			if (colorGrading.colorFilter.overrideState)
			{
				colorGrading.colorFilter.overrideState = false;
			}
		}
		else if (!colorGrading.colorFilter.overrideState)
		{
			colorGrading.colorFilter.overrideState = true;
		}
	}

	private void OnDestroy()
	{
		if (characters != null && characters.Contains(this))
		{
			characters.Remove(this);
		}
	}

	public static CharacterMultiplayer GetPlayer(string nickname, int actorNumber)
	{
		FillCharactersList();
		foreach (CharacterMultiplayer character in characters)
		{
			if ((bool)character && character.Nickname == nickname && character.ActorNumber == actorNumber)
			{
				return character;
			}
		}
		return null;
	}

	public static CharacterMultiplayer GetPlayer(int actorNumber)
	{
		FillCharactersList();
		foreach (CharacterMultiplayer character in characters)
		{
			if ((bool)character && character.ActorNumber == actorNumber)
			{
				return character;
			}
		}
		return null;
	}

	public static CharacterMultiplayer GetMainPlayer()
	{
		FillCharactersList();
		foreach (CharacterMultiplayer character in characters)
		{
			if ((bool)character && character.IsLocalMainPlayer())
			{
				return character;
			}
		}
		return null;
	}

	public static CharacterMultiplayer GetSpectatingPlayer()
	{
		FillCharactersList();
		foreach (CharacterMultiplayer character in characters)
		{
			if ((bool)character && character.isSpectating)
			{
				return character;
			}
		}
		return null;
	}

	public void Add()
	{
		if (characters != null && !characters.Contains(this))
		{
			characters.Add(this);
		}
	}

	private static void FillCharactersList()
	{
	}

	public void RemovedFromRoom()
	{
	}

	public void OnMatchStarted()
	{
		_character.ShowCursor(show: false);
		_character.Restart();
		GetComponent<ThirdPerson>().isActive = false;
	}

	public void OnMatchFinished()
	{
		_character.ShowCursor(show: true);
		if ((bool)dropedAmmoBox)
		{
			Object.Destroy(dropedAmmoBox);
			dropedAmmoBox = null;
		}
	}

	public void FillSquad(List<MatchmakingManager.Squad> squads)
	{
		this.squad.Clear();
		MatchmakingManager.Squad squad = null;
		foreach (MatchmakingManager.Squad squad2 in squads)
		{
			if (squad2.squadPlayers.Find((MatchmakingManager.SquadPlayer p) => p.name == Nickname && p.ActorNumber == ActorNumber) != null)
			{
				squad = squad2;
				break;
			}
		}
		if (squad != null)
		{
			SquadId = squad.id;
			{
				foreach (MatchmakingManager.SquadPlayer squadPlayer in squad.squadPlayers)
				{
					if (squadPlayer.ActorNumber != ActorNumber)
					{
						CharacterMultiplayer player = GetPlayer(squadPlayer.name, squadPlayer.ActorNumber);
						if ((bool)player)
						{
							this.squad.Add(player);
						}
					}
				}
				return;
			}
		}
		Debug.LogWarning($"fill squad not finding squad {base.name} {SquadId}");
	}

	public bool IsInsideSafeZone()
	{
		CapsuleCollider capsuleCollider = GameManager.Instance?.GetComponent<DamageZoneManager>().damageZone.GetComponent<CapsuleCollider>();
		if (capsuleCollider == null || !capsuleCollider.gameObject.activeSelf)
		{
			return true;
		}
		float x = capsuleCollider.bounds.extents.x;
		Vector3 center = capsuleCollider.bounds.center;
		center.y = base.transform.position.y;
		if (Vector3.Distance(base.transform.position, center) > x)
		{
			return false;
		}
		return true;
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		Nickname = (string)instantiationData[0];
		ActorNumber = (int)instantiationData[1];
		string text = (string)instantiationData[2];
		SquadId = (int)instantiationData[3];
		isBot = (bool)instantiationData[4];
		isLocal = base.photonView.IsMine;
		isMainPlayer = IsLocalMainPlayer();
		isMasterServer = PhotonNetwork.IsMasterClient && isMainPlayer;
		ItemsCollectionsSync.DecodeBodyData((byte[])instantiationData[5], out var body, out var head, out var neck, out var glasses, out var earmuffs, out var beard, out var hair, out var facemask, out var vest, out var bag, out var parachute);
		base.name = Nickname + "--((" + ActorNumber + "))";
		Debug.Log("OnPhotonInstantiate NickName: " + Nickname + " ActorNumber:" + ActorNumber + " IsBot:" + isBot + " initSpawn:" + text + " TeamId:" + SquadId);
		Debug.Log($"Body Parts => Body:{body}, Head:{head}, Neck:{neck}, Glasses:{glasses}, Earmuffs:{earmuffs}, Beard:{beard}, Hair:{hair}, Facemask:{facemask}, vest:{vest}, bag:{bag}, parachute:{parachute}");
		GetComponent<ItemsCollectionsSync>().Apply(body, head, neck, glasses, earmuffs, beard, hair, facemask, vest, bag, parachute);
		if (IsLocalMainPlayer() && (bool)GameManager.Instance)
		{
			Debug.Log("reset from saved " + Nickname);
			GameManager.Instance.GetComponent<CharacterCustomizationManager>().ResetFromSaved();
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public bool IsLocal()
	{
		return base.photonView.IsMine;
	}

	public bool IsLocalMainPlayer()
	{
		if (base.photonView.IsMine)
		{
			return !isBot;
		}
		return false;
	}

	public bool IsSquadMember(CharacterMultiplayer member)
	{
		return squad.Contains(member);
	}

	public bool IsDead()
	{
		return isDead;
	}

	public void RPC_Dead(byte killerActorId)
	{
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			base.photonView.RPC("OnDead", RpcTarget.All, killerActorId);
		}
	}

	[PunRPC]
	public void OnDead(byte shooterActorId)
	{
		if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing || isDead)
		{
			return;
		}
		isDead = true;
		health = 0;
		isBotSquadLeader = false;
		match_rank = MatchmakingManager.Instance.GetComponent<WinnersManager>().GetRemaining();
		match_rank++;
		ragDollController.EnableRagDoll(enable: true);
		GetComponent<ThirdPerson>().isActive = false;
		GetComponent<CharacterInventory>().OnDead();
		GetComponent<Character>().OnDead();
		DropAmmoBox();
		if (IsLocalMainPlayer())
		{
			MatchmakingManager.Instance.Dead(shooterActorId);
		}
		CharacterMultiplayer player = GetPlayer(shooterActorId);
		if ((bool)player)
		{
			player.AddKill();
			Weapon equippedWeapon = player.GetComponent<Character>().GetEquippedWeapon();
			if ((bool)equippedWeapon && (bool)equippedWeapon.weaponIcon)
			{
				killsLog.AddKillLog(player.Nickname, Nickname, equippedWeapon.weaponIcon);
			}
		}
	}

	public void RPC_Respawn()
	{
		if (isLocal && MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			base.photonView.RPC("OnRespawn", RpcTarget.All);
		}
	}

	[PunRPC]
	public void OnRespawn()
	{
		if (isDead)
		{
			_character.Restart();
			GetComponent<ThirdPerson>().isActive = true;
			if (IsLocalMainPlayer())
			{
				MatchmakingManager.Instance.Respawn();
			}
		}
	}

	[PunRPC]
	public void Damage(byte damage, byte shooterActorId)
	{
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing && !isDead)
		{
			int num = health - damage;
			if (num < 1)
			{
				num = 1;
			}
			health = (byte)num;
			if (IsLocalMainPlayer())
			{
				MatchmakingManager.Instance.GetComponent<HitCursorsManager>().ShowDamageCursor(shooterActorId);
			}
			if (isBot && isLocal)
			{
				GetComponent<CharacterBot>().Damage(shooterActorId);
			}
		}
	}

	public void RPC_Damage(byte damage, byte shooterActorId)
	{
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			if (health - damage <= 0)
			{
				RPC_Dead(shooterActorId);
				return;
			}
			base.photonView.RPC("Damage", RpcTarget.All, damage, shooterActorId);
		}
	}

	[PunRPC]
	public void RestoreHealth(byte add)
	{
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing && !isDead)
		{
			int num = health + add;
			if (num > 255)
			{
				num = 255;
			}
			health = (byte)num;
		}
	}

	public void RPC_RestoreHealth(byte add)
	{
		if (isMainPlayer && !IsDead() && MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			base.photonView.RPC("RestoreHealth", RpcTarget.All, add);
		}
	}

	private void UpdateHealing()
	{
		if (!isMainPlayer)
		{
			return;
		}
		if (isHealing)
		{
			healingImg.fillAmount += Time.deltaTime * healingSpeed * 1.3f;
			if (healingImg.fillAmount >= 1f)
			{
				isHealing = false;
				RPC_RestoreHealth(healing_add);
			}
			Statics.SetActive(healingImg.transform.parent.gameObject, active: true);
		}
		else
		{
			if (healingImg.fillAmount != 0f)
			{
				healingImg.fillAmount = 0f;
			}
			Statics.SetActive(healingImg.transform.parent.gameObject, active: false);
		}
	}

	public void AddKill()
	{
		kills++;
		if (IsLocalMainPlayer())
		{
			MatchmakingManager.Instance.GetComponent<HitCursorsManager>().ShowKillScore();
		}
	}

	private void DropAmmoBox()
	{
		PickupsManager component = GameManager.Instance.GetComponent<PickupsManager>();
		Vector3 position = base.transform.position;
		Quaternion identity = Quaternion.identity;
		if (!Physics.Raycast(base.transform.position + new Vector3(0f, 0.5f, 0f), Vector3.down, out var hitInfo, 1f, component.pickLayerMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		position.y = hitInfo.point.y;
		identity = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
		GameObject gameObject = Object.Instantiate(component.ammoBoxPrefab, position, identity);
		dropedAmmoBox = gameObject.GetComponent<AmmoBox>();
		if (!dropedAmmoBox)
		{
			return;
		}
		dropedAmmoBox.achievable = false;
		dropedAmmoBox.lootPoint = null;
		ItemsCollectionsSync component2 = GetComponent<ItemsCollectionsSync>();
		GameObject collectionActiveObj = component2.GetCollectionActiveObj("armor");
		GameObject collectionActiveObj2 = component2.GetCollectionActiveObj("backbag");
		GameObject collectionActiveObj3 = component2.GetCollectionActiveObj("hat");
		Weapon equippedWeapon = GetComponent<Character>().GetEquippedWeapon();
		if ((bool)collectionActiveObj)
		{
			string text = collectionActiveObj.name;
			text = text.Replace("Female", "Male").Replace("female", "Male");
			PickupsManager.Item itemByObj = component.GetItemByObj(text);
			if (itemByObj != null)
			{
				dropedAmmoBox.items.Add(itemByObj);
			}
		}
		if ((bool)collectionActiveObj2)
		{
			string objName = collectionActiveObj2.name;
			PickupsManager.Item itemByObj2 = component.GetItemByObj(objName);
			if (itemByObj2 != null)
			{
				dropedAmmoBox.items.Add(itemByObj2);
			}
		}
		if ((bool)collectionActiveObj3)
		{
			string objName2 = collectionActiveObj3.name;
			PickupsManager.Item itemByObj3 = component.GetItemByObj(objName2);
			if (itemByObj3 != null)
			{
				dropedAmmoBox.items.Add(itemByObj3);
			}
		}
		if ((bool)equippedWeapon)
		{
			string weaponName = equippedWeapon.GetWeaponName();
			PickupsManager.Item item = component.GetItem(weaponName);
			if (item != null)
			{
				dropedAmmoBox.items.Add(item);
			}
		}
		dropedAmmoBox.ShowObjects();
	}
}
