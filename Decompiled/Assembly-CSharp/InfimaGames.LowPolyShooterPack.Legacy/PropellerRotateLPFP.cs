using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Legacy;

public class PropellerRotateLPFP : MonoBehaviour
{
	[Tooltip("How fast the propellers rotate on the Z axis")]
	public float rotationSpeed = 2500f;

	private void Update()
	{
		base.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
	}
}
