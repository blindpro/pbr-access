using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class PlaneDoorLightsLPFP : MonoBehaviour
{
	[Header("Plane Lights Object")]
	public GameObject planeDoorLights;

	[Header("Green Light Material")]
	public Material greenEmission;

	[Header("Light Components")]
	public Light redLight;

	public Light greenLight;

	[Header("Timer")]
	public float openDoorTimer;

	private void Start()
	{
		StartCoroutine(DoorLightsTimer());
		redLight.enabled = true;
		greenLight.enabled = false;
	}

	private IEnumerator DoorLightsTimer()
	{
		yield return new WaitForSeconds(openDoorTimer);
		planeDoorLights.GetComponent<MeshRenderer>().material = greenEmission;
		redLight.enabled = false;
		greenLight.enabled = true;
	}
}
