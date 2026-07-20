using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterMinimap : MonoBehaviour
{
	public MeshRenderer smallMinimapIcon;

	public MeshRenderer bigMinimapIcon;

	public Color friendColor;

	public Color enemyFiringColor;

	public Color mainPlayerColor;

	private CharacterMultiplayer characterMultiplayer;

	private Character character;

	private float fireTimer;

	public float fireShowTime = 2f;

	private void Start()
	{
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
		character = GetComponent<Character>();
	}

	private void Update()
	{
		CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
		if (!mainPlayer)
		{
			return;
		}
		fireTimer -= Time.deltaTime;
		if (fireTimer <= 0f)
		{
			fireTimer = 0f;
		}
		if (character.IsHoldingButtonFire())
		{
			fireTimer = fireShowTime;
		}
		Color color;
		if (characterMultiplayer.isMainPlayer)
		{
			color = mainPlayerColor;
		}
		else if (mainPlayer.IsSquadMember(characterMultiplayer))
		{
			color = friendColor;
		}
		else
		{
			color = enemyFiringColor;
			if (!characterMultiplayer.isSpectating)
			{
				color.a = 0f;
			}
		}
		smallMinimapIcon.material.color = color;
		bigMinimapIcon.material.color = color;
	}
}
