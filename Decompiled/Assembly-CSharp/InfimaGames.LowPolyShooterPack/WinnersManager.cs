using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class WinnersManager : MonoBehaviour
{
	public Text congratsTop3Txt;

	public Text congratsSquadTxt;

	public Text mainPlayerRank;

	public Text mainPlayerRankDead;

	public Text nextMatchTimeTxt;

	public Transform squadWinnersParent;

	public Transform top3WinnersParent;

	public float timeBeforeNextMatch = 30f;

	public Text remainingTxt;

	public Text remainingSquadsTxt;

	public Text killsTxt;

	private float nextMatchTimer;

	private bool matchFinished;

	private List<int> remainingSquadsIds = new List<int>();

	private List<int> _remainingSquadsIds = new List<int>();

	private void Start()
	{
	}

	private void Update()
	{
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Finish && matchFinished)
		{
			nextMatchTimer -= Time.deltaTime;
			if (nextMatchTimer <= 0f)
			{
				nextMatchTimer = 0f;
				matchFinished = false;
				MatchmakingManager.Instance.RPC_RestartMatch();
			}
			nextMatchTimeTxt.text = ((int)nextMatchTimer).ToString() ?? "";
		}
		if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
		{
			return;
		}
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!mainPlayer)
		{
			return;
		}
		killsTxt.text = mainPlayer.kills.ToString();
		Text text = mainPlayerRankDead;
		string text2 = (mainPlayerRank.text = mainPlayer.match_rank + "#");
		text.text = text2;
		int num = 0;
		_remainingSquadsIds.Clear();
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if ((bool)character && !character.IsDead())
			{
				num++;
				if (!_remainingSquadsIds.Contains(character.SquadId))
				{
					_remainingSquadsIds.Add(character.SquadId);
				}
			}
		}
		remainingTxt.text = num.ToString();
		remainingSquadsTxt.text = _remainingSquadsIds.Count.ToString();
		MatchmakingManager.Instance.CheckMatchEnd();
	}

	public byte GetRemaining()
	{
		byte b = 0;
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if ((bool)character && !character.IsDead())
			{
				b++;
			}
		}
		return b;
	}

	public List<int> GetRemainingSquadsIds()
	{
		remainingSquadsIds.Clear();
		foreach (CharacterMultiplayer character in CharacterMultiplayer.characters)
		{
			if ((bool)character && !character.IsDead() && !remainingSquadsIds.Contains(character.SquadId))
			{
				remainingSquadsIds.Add(character.SquadId);
			}
		}
		return remainingSquadsIds;
	}

	public void Show(bool squad, CharacterMultiplayer[] rankedWinners)
	{
		if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Finish)
		{
			squadWinnersParent.gameObject.SetActive(squad);
			top3WinnersParent.gameObject.SetActive(!squad);
			MenuCharacter[] componentsInChildren = (squad ? squadWinnersParent : top3WinnersParent).GetComponentsInChildren<MenuCharacter>(includeInactive: true);
			MenuCharacter[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
			for (int j = 0; j < rankedWinners.Length; j++)
			{
				componentsInChildren[j].gameObject.SetActive(value: true);
				componentsInChildren[j].player = rankedWinners[j];
				Statics.SetActive(componentsInChildren[j].matchRankTxt.gameObject, active: true);
				Statics.SetActive(componentsInChildren[j].killsTxt.gameObject, active: true);
			}
			nextMatchTimer = timeBeforeNextMatch;
			matchFinished = true;
			bool flag = MatchmakingManager.Instance.IsSquadMatch();
			Statics.SetActive(congratsTop3Txt.gameObject, !flag);
			Statics.SetActive(congratsSquadTxt.gameObject, flag);
		}
	}
}
