using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CameraLook : MonoBehaviour
{
	[Tooltip("Sensitivity when looking around.")]
	[SerializeField]
	private Vector2 sensitivity = new Vector2(1f, 1f);

	[Tooltip("Minimum and maximum up/down rotation angle the camera can have.")]
	[SerializeField]
	private Vector2 yClamp = new Vector2(-60f, 60f);

	[Tooltip("Should the look rotation be interpolated?")]
	[SerializeField]
	private bool smooth;

	[Tooltip("The speed at which the look rotation is interpolated.")]
	[SerializeField]
	private float interpolationSpeed = 25f;

	private CharacterMultiplayer characterMultiplayer;

	private CharacterBehaviour playerCharacter;

	private Rigidbody playerCharacterRigidbody;

	private Quaternion rotationCharacter;

	private Quaternion rotationCamera;

	public float rotationPitchOverrided;

	private SettingsManager settingsManager;

	private void Start()
	{
		playerCharacter = GetComponentInParent<Character>();
		characterMultiplayer = GetComponentInParent<CharacterMultiplayer>();
		settingsManager = GameManager.Instance.GetComponent<SettingsManager>();
		rotationCharacter = playerCharacter.transform.localRotation;
		rotationCamera = base.transform.localRotation;
	}

	private void LateUpdate()
	{
		Vector2 vector = (playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : Vector2.zero);
		if (!playerCharacter.GetComponent<ThirdPerson>().isActive || characterMultiplayer.isBot || !characterMultiplayer.isLocal)
		{
			vector = Vector2.zero;
		}
		if (characterMultiplayer.isMainPlayer)
		{
			vector *= settingsManager.SensibbilitySlider.value;
			if (settingsManager.inverseMouseX.isOn)
			{
				vector.x *= -1f;
			}
			if (settingsManager.inverseMouseY.isOn)
			{
				vector.y *= -1f;
			}
		}
		Quaternion quaternion = Quaternion.Euler(0f, vector.x, 0f);
		Quaternion quaternion2 = Quaternion.Euler(0f - vector.y, 0f, 0f);
		rotationCamera *= quaternion2;
		rotationCamera = Clamp(rotationCamera);
		rotationCharacter *= quaternion;
		Quaternion localRotation = base.transform.localRotation;
		if (smooth)
		{
			localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);
			localRotation = Clamp(localRotation);
			playerCharacter.transform.rotation = Quaternion.Slerp(playerCharacter.transform.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed);
		}
		else
		{
			localRotation *= quaternion2;
			localRotation = Clamp(localRotation);
			playerCharacter.transform.rotation *= quaternion;
		}
		base.transform.localRotation = localRotation;
	}

	private Quaternion Clamp(Quaternion rotation)
	{
		rotation.x /= rotation.w;
		rotation.y /= rotation.w;
		rotation.z /= rotation.w;
		rotation.w = 1f;
		float value = 114.59156f * Mathf.Atan(rotation.x);
		value = Mathf.Clamp(value, yClamp.x, yClamp.y);
		if (!characterMultiplayer.isMainPlayer)
		{
			value = rotationPitchOverrided;
		}
		else
		{
			rotationPitchOverrided = value;
		}
		rotation.x = Mathf.Tan(MathF.PI / 360f * value);
		return rotation;
	}
}
