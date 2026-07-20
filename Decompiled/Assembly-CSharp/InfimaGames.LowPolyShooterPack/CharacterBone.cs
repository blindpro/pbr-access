using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterBone : MonoBehaviour
{
	public Character character;

	public float damage = 1f;

	private void Awake()
	{
		character = GetComponentInParent<Character>();
	}

	private void Start()
	{
	}

	private void Update()
	{
	}
}
