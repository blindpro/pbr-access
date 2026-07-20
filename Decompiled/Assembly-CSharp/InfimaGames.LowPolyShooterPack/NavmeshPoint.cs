using UnityEngine;
using UnityEngine.AI;

namespace InfimaGames.LowPolyShooterPack;

public class NavmeshPoint : MonoBehaviour
{
	private void Start()
	{
		base.transform.position = GetNearestNavMeshPoint(base.transform.position);
		GetComponent<BoxCollider>().enabled = false;
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
	}

	public Vector3 GetNearestNavMeshPoint(Vector3 sourcePosition, float maxDistance = 10f)
	{
		if (NavMesh.SamplePosition(sourcePosition, out var hit, maxDistance, -1))
		{
			return hit.position;
		}
		Debug.LogWarning("No NavMesh found near the point.");
		return sourcePosition;
	}
}
