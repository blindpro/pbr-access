using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class SteamFriendRow : MonoBehaviour
{
	[Header("UI")]
	public Text nameText;

	public UnityEngine.UI.Image iconImage;

	public Button inviteButton;

	public string connectString = "";

	private Friend steamFriend;

	private GameObject lobby;

	public void Setup(Friend friend)
	{
		steamFriend = friend;
		nameText.text = friend.Name;
		inviteButton.onClick.RemoveAllListeners();
		inviteButton.onClick.AddListener(OnInviteClicked);
		LoadAvatar(friend);
	}

	private void Start()
	{
		if ((bool)GameManager.Instance)
		{
			lobby = GameManager.Instance.GetComponent<MatchmakingManager>().WaitingPanel;
		}
	}

	private void Update()
	{
		inviteButton.interactable = lobby.activeSelf;
	}

	private async void LoadAvatar(Friend friend)
	{
		try
		{
			Steamworks.Data.Image? image = await friend.GetLargeAvatarAsync();
			if (image.HasValue)
			{
				Texture2D texture2D = image.Value.Convert();
				Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
				iconImage.sprite = sprite;
			}
			else
			{
				Debug.Log("No avatar found for " + friend.Name);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Error loading avatar for " + friend.Name + ": " + ex.Message);
		}
	}

	private void OnInviteClicked()
	{
		steamFriend.InviteToGame(connectString);
		Debug.Log("Sent Steam invite to " + steamFriend.Name + " for room " + connectString);
	}
}
