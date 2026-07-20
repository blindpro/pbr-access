using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class MenuCharactersManager : MonoBehaviour
{
	public string menuCharacterLayer = "MenuCharacter";

	public Transform[] menuCharactersAll;

	public GameObject menuCharacterPrefab;

	public Transform[] homeSquadParents;

	public Transform[] winnerSquadParents;

	public Transform[] top3Parents;

	private MenuCharacter[] homeSquad;

	private MatchmakingManager matchmakingManager;

	private void Start()
	{
		Transform[] array = menuCharactersAll;
		foreach (Transform parent in array)
		{
			GameObject obj = Object.Instantiate(menuCharacterPrefab, parent);
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localRotation = Quaternion.identity;
			Statics.SetLayerRecursively(obj, LayerMask.NameToLayer(menuCharacterLayer));
		}
		homeSquad = new MenuCharacter[homeSquadParents.Length];
		for (int j = 0; j < homeSquadParents.Length; j++)
		{
			homeSquad[j] = homeSquadParents[j].GetComponentInChildren<MenuCharacter>();
		}
		matchmakingManager = MatchmakingManager.Instance;
		MenuCharacter[] array2 = homeSquad;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].isMainLocalPlayer = false;
		}
		homeSquad[0].isMainLocalPlayer = true;
		GetComponent<CharacterCustomizationManager>().ResetFromSaved();
	}

	private void Update()
	{
		if (matchmakingManager.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			return;
		}
		int num = 0;
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if ((bool)mainPlayer)
		{
			homeSquad[0].player = mainPlayer;
			if (mainPlayer.squad != null)
			{
				for (int i = 0; i < mainPlayer.squad.Count; i++)
				{
					int num2 = i + 1;
					CharacterMultiplayer characterMultiplayer = mainPlayer.squad[i];
					if ((bool)characterMultiplayer)
					{
						Statics.SetActive(homeSquad[num2].gameObject, active: true);
						homeSquad[num2].player = characterMultiplayer;
						homeSquad[num2].nickname = characterMultiplayer.Nickname;
						homeSquad[num2].actorNumber = characterMultiplayer.ActorNumber;
						Statics.SetActive(homeSquad[num2].matchRankTxt.gameObject, active: false);
						Statics.SetActive(homeSquad[num2].killsTxt.gameObject, active: false);
						num = num2;
					}
					else
					{
						Statics.SetActive(homeSquad[num2].gameObject, active: false);
					}
				}
			}
		}
		for (int j = num + 1; j < homeSquad.Length; j++)
		{
			Statics.SetActive(homeSquad[j].gameObject, active: false);
		}
		updateMainMenuCharacterFromLocal();
	}

	private void updateMainMenuCharacterFromLocal()
	{
		Statics.SetActive(homeSquad[0].gameObject, active: true);
		homeSquad[0].nickname = matchmakingManager.PlayerName;
		homeSquad[0].isMainLocalPlayer = true;
		Statics.SetActive(homeSquad[0].nicknametxt.gameObject, active: false);
		Statics.SetActive(homeSquad[0].matchRankTxt.gameObject, active: false);
		Statics.SetActive(homeSquad[0].killsTxt.gameObject, active: false);
	}

	public MenuCharacter GetMainMenuCharacter()
	{
		if (homeSquad != null && homeSquad.Length != 0)
		{
			return homeSquad[0];
		}
		return null;
	}
}
