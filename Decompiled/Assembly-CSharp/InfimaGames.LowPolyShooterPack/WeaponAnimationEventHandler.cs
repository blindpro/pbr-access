using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class WeaponAnimationEventHandler : MonoBehaviour
{
	private WeaponBehaviour weapon;

	private void Awake()
	{
		weapon = GetComponent<WeaponBehaviour>();
	}

	private void OnEjectCasing()
	{
		if (weapon != null)
		{
			weapon.EjectCasing();
		}
	}
}
