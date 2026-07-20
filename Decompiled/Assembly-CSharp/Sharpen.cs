using UnityEngine;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class Sharpen : MonoBehaviour
{
	[Range(0f, 1f)]
	[Tooltip("Sharpness")]
	public float Sharpness;

	public Material material;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		material.SetFloat("_CentralFactor", 1f + 3.2f * Sharpness);
		material.SetFloat("_SideFactor", 0.8f * Sharpness);
		Graphics.Blit(source, destination, material, 0);
	}
}
