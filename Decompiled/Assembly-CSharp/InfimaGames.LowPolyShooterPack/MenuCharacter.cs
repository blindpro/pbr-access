using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class MenuCharacter : MonoBehaviour
{
	[Header("Item Collections (One Active Per Group)")]
	public List<ItemsCollectionsSync.ItemsCollection> itemsCollections = new List<ItemsCollectionsSync.ItemsCollection>();

	public bool isMainLocalPlayer;

	public Text nicknametxt;

	public Text matchRankTxt;

	public Text killsTxt;

	public LayerMask layer;

	public string nickname;

	public int actorNumber;

	public CharacterMultiplayer player;

	private float updateTimer;

	private float updateDuration = 1f;

	private Weapon currentWeapon;

	private Weapon playerWeapon;

	private void Start()
	{
		if (isMainLocalPlayer)
		{
			updateDuration = 0.1f;
		}
		currentWeapon = GetComponentInChildren<Weapon>(includeInactive: false);
	}

	private void Update()
	{
		if ((bool)player)
		{
			if (playerWeapon == null)
			{
				playerWeapon = player.GetComponentInChildren<Weapon>(includeInactive: false);
			}
			nickname = player.Nickname;
			actorNumber = player.ActorNumber;
			nicknametxt.text = nickname;
			int match_rank = player.match_rank;
			matchRankTxt.text = "#" + match_rank;
			killsTxt.text = player.kills.ToString();
			updateTimer -= Time.deltaTime;
			if (updateTimer <= 0f)
			{
				updateTimer = updateDuration;
				CopyTpsCharacterToMenuCharacter();
			}
		}
		else
		{
			updateTimer = updateDuration;
			if ((bool)playerWeapon)
			{
				playerWeapon = null;
			}
		}
		SynchPlayerSkin();
	}

	private void SynchPlayerSkin()
	{
		if ((bool)currentWeapon && (bool)GameManager.Instance)
		{
			int num = -1;
			if (isMainLocalPlayer)
			{
				num = GameManager.Instance.GetComponent<DataManager>().GetInt(currentWeapon.GetWeaponName() + "_skin", -1);
			}
			else if ((bool)playerWeapon)
			{
				num = playerWeapon.GetComponent<WeaponAttachmentManager>().skinIndex;
			}
			WeaponAttachmentManager component = currentWeapon.GetComponent<WeaponAttachmentManager>();
			if (component.skinIndex != num)
			{
				component.skinIndex = num;
				component.Apply();
				Debug.Log("menu character skinIndex from " + currentWeapon.GetWeaponName() + "_skin =" + num);
			}
		}
	}

	private void copy(Transform dest, Transform source)
	{
		if (!dest || !source || dest.name == "AimRigLayer1" || dest.name == "AimRigLayer2" || dest.name == "AimRigLayer3" || dest.name == "IKRigLayer" || dest.name == "UI" || dest.name == "Parachute")
		{
			return;
		}
		if (dest.name == source.name && (bool)dest.GetComponent<Renderer>())
		{
			Statics.SetActive(dest.gameObject, source.gameObject.activeSelf);
		}
		for (int i = 0; i < dest.childCount; i++)
		{
			Transform child = dest.GetChild(i);
			if ((bool)child && source.childCount > i)
			{
				Transform child2 = source.GetChild(i);
				copy(child, child2);
			}
		}
	}

	private void CopyTpsCharacterToMenuCharacter()
	{
		if ((bool)player)
		{
			copy(base.transform, player.GetComponent<ThirdPerson>().tps_animator.transform);
		}
	}

	private void OnEnable()
	{
		updateTimer = 0f;
	}
}
