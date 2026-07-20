using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class SpriteBillboard : MonoBehaviour
{
	public bool UpdateRotation = true;

	public bool UseCameraUpAxis;

	public bool AlignToCamera;

	private Vector3 myUp;

	private void Start()
	{
	}

	private void Update()
	{
		if (!UpdateRotation)
		{
			return;
		}
		myUp = base.transform.up;
		Camera camera = Camera.main;
		if ((bool)MatchmakingManager.Instance && MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
		{
			CharacterMultiplayer mainPlayer = CharacterMultiplayer.GetMainPlayer();
			if ((bool)mainPlayer)
			{
				camera = mainPlayer.GetComponent<ThirdPerson>().fps_camera;
			}
		}
		if ((bool)camera)
		{
			Vector3 up = myUp;
			Vector3 position = camera.transform.position;
			if (UseCameraUpAxis)
			{
				up = camera.transform.up;
			}
			else
			{
				position.y = base.transform.position.y;
			}
			base.transform.LookAt(position, up);
			if (AlignToCamera)
			{
				base.transform.rotation = camera.transform.rotation;
			}
		}
	}
}
