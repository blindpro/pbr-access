using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class CharacterDemonstration : MonoBehaviour
{
	[Tooltip("Transform component of the character weapon's magazine.")]
	[SerializeField]
	private Transform magazineTransform;

	[Tooltip("Magazine Prefab. A generic one is used, no need for specific prefabs.")]
	[SerializeField]
	private GameObject prefabMagazine;

	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private void Awake()
	{
		meshFilter = magazineTransform.GetComponent<MeshFilter>();
		meshRenderer = magazineTransform.GetComponent<MeshRenderer>();
	}

	public void DropMagazine(bool drop = true)
	{
		magazineTransform.gameObject.SetActive(!drop);
		if (drop)
		{
			GameObject obj = Object.Instantiate(prefabMagazine, magazineTransform.position, magazineTransform.rotation);
			obj.GetComponent<MeshRenderer>().sharedMaterials = meshRenderer.sharedMaterials;
			obj.GetComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;
			Object.Destroy(obj, 5f);
		}
	}
}
