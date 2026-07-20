using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CopyTransform : MonoBehaviour
{
	[Tooltip("Transform to copy from.")]
	[SerializeField]
	private Transform copy;

	private Transform local;

	private void Awake()
	{
		local = base.transform;
	}

	private void Update()
	{
		local.position = copy.position;
		local.rotation = copy.rotation;
		local.localScale = copy.localScale;
	}
}
