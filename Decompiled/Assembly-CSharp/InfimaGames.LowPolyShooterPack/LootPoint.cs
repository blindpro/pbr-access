using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class LootPoint : MonoBehaviour
{
	public Transform ammoBoxParent;

	public bool achievable = true;

	private PickupsManager pickupsManager;

	private void Start()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		Vector3 position = ammoBoxParent.position;
		Quaternion rotation = Quaternion.identity;
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 5.5f, pickupsManager.pickLayerMask, QueryTriggerInteraction.Ignore))
		{
			position.y = hitInfo.point.y;
			rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
			Vector3 normalized = Vector3.ProjectOnPlane(base.transform.forward, hitInfo.normal).normalized;
			if (normalized.sqrMagnitude > 0.001f)
			{
				rotation = Quaternion.LookRotation(normalized, hitInfo.normal);
			}
		}
		GameObject obj = Object.Instantiate(pickupsManager.ammoBoxPrefab, ammoBoxParent);
		obj.transform.position = position;
		obj.transform.rotation = rotation;
		AmmoBox component = obj.GetComponent<AmmoBox>();
		if ((bool)component)
		{
			component.achievable = achievable;
			component.lootPoint = this;
		}
	}

	private void Update()
	{
	}
}
