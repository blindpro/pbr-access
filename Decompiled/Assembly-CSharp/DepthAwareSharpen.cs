using UnityEngine;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
public class DepthAwareSharpen : MonoBehaviour
{
	public Shader sharpenShader;

	private Material _mat;

	[Range(0f, 5f)]
	public float sharpenStrength = 1f;

	[Range(0f, 10f)]
	public float maskBlurStrength = 8f;

	[Range(0f, 5f)]
	public float edgeBlurStrength = 2f;

	[Range(0f, 1f)]
	public float depthSensitivity = 0.05f;

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		if (_mat == null)
		{
			_mat = new Material(sharpenShader);
			_mat.hideFlags = HideFlags.HideAndDontSave;
		}
		_mat.SetFloat("_Strength", sharpenStrength);
		_mat.SetFloat("_BlurStrength", maskBlurStrength);
		_mat.SetFloat("_EdgeBlurStrength", edgeBlurStrength);
		_mat.SetFloat("_DepthSensitivity", depthSensitivity);
		Graphics.Blit(src, dest, _mat);
	}

	private void Start()
	{
		GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
	}

	private void OnDisable()
	{
		if (_mat != null)
		{
			Object.DestroyImmediate(_mat);
		}
	}
}
