using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class FieldOfViewIncreaseLPFP : MonoBehaviour
{
	[Header("Player Camera")]
	[Tooltip("The camera attached to the player head bone")]
	public Camera playerCamera;

	[Header("Player Flashlight")]
	[Tooltip("The spotlight attached to the player head bone")]
	public Light flashLight;

	[Header("FOV Settings")]
	public int targetFOV = 60;

	public float fovSpeed = 0.4f;

	public float startAfter = 33.5f;

	public float flashlightStartAfter = 38f;

	private bool increaseEnabled;

	private void Start()
	{
		increaseEnabled = false;
		flashLight.enabled = false;
		StartCoroutine(StartFOVTimer());
		StartCoroutine(FlashlightTimer());
	}

	private IEnumerator StartFOVTimer()
	{
		yield return new WaitForSeconds(startAfter);
		increaseEnabled = true;
	}

	private IEnumerator FlashlightTimer()
	{
		yield return new WaitForSeconds(flashlightStartAfter);
		flashLight.enabled = true;
	}

	private void Update()
	{
		if (increaseEnabled)
		{
			playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
		}
	}
}
