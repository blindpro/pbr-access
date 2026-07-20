using UnityEngine;

public class LOD_Runtime : MonoBehaviour
{
	public float cull_value = 0.03f;

	public bool skin_mesh_separated;

	public float cull_value_skinmesh = 0.04f;

	public bool include_disabled;

	public bool ignore_particles;

	private void Start()
	{
		Renderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<Renderer>(include_disabled);
		foreach (Renderer renderer in componentsInChildren)
		{
			if ((bool)renderer && renderer.gameObject != null && !renderer.gameObject.GetComponent<LODGroup>() && (!ignore_particles || !renderer.GetComponent<ParticleSystem>()))
			{
				LODGroup lODGroup = renderer.gameObject.AddComponent<LODGroup>();
				LOD[] array = new LOD[1];
				float screenRelativeTransitionHeight = cull_value;
				if (skin_mesh_separated && (bool)renderer.gameObject.GetComponent<SkinnedMeshRenderer>())
				{
					screenRelativeTransitionHeight = cull_value_skinmesh;
				}
				if (skin_mesh_separated && (bool)renderer.gameObject.GetComponent<SkinnedMeshRenderer>() && renderer.gameObject.name.Contains("_Armor_"))
				{
					screenRelativeTransitionHeight = cull_value;
				}
				array[0] = new LOD(screenRelativeTransitionHeight, new Renderer[1] { renderer });
				lODGroup.SetLODs(array);
				lODGroup.RecalculateBounds();
			}
		}
	}

	private void Update()
	{
	}
}
