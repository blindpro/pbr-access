using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class MiniMapCameraFollow : MonoBehaviour
{
	public float playerFollowY = 1332.1f;

	public float airplaneFollowY = 1632.1f;

	private void Start()
	{
	}

	private void Update()
	{
		CharacterMultiplayer characterMultiplayer = CharacterMultiplayer.GetMainPlayer();
		CharacterMultiplayer spectatingPlayer = CharacterMultiplayer.GetSpectatingPlayer();
		if ((bool)spectatingPlayer)
		{
			characterMultiplayer = spectatingPlayer;
		}
		if ((bool)characterMultiplayer)
		{
			base.transform.position = new Vector3(characterMultiplayer.transform.position.x, playerFollowY, characterMultiplayer.transform.position.z);
			if (characterMultiplayer.GetComponent<CharacterParachute>().isOnAirplane)
			{
				GameObject airplane = MatchmakingManager.Instance.GetComponent<AirplaneManager>().Airplane;
				base.transform.position = new Vector3(airplane.transform.position.x, airplaneFollowY, airplane.transform.position.z);
			}
		}
	}
}
