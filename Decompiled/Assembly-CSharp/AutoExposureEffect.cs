using UnityEngine;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
public class AutoExposureEffect : MonoBehaviour
{
	public Material material;

	[Header("Exposure")]
	public float targetBrightness = 0.5f;

	public float minExposure = 0.5f;

	public float maxExposure = 2f;

	[Header("Adaptation")]
	public float speedUp = 3f;

	public float speedDown = 1f;

	[Header("Performance")]
	public int downsample = 16;

	public float updateInterval = 0.1f;

	private float currentExposure = 1f;

	private float timer;

	private RenderTexture smallRT;

	private Texture2D tex;

	private void Start()
	{
		int width = Screen.width / downsample;
		int height = Screen.height / downsample;
		smallRT = new RenderTexture(width, height, 0);
		tex = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		if (material == null)
		{
			Graphics.Blit(src, dest);
			return;
		}
		Graphics.Blit(src, smallRT);
		timer += Time.deltaTime;
		if (timer >= updateInterval)
		{
			timer = 0f;
			RenderTexture.active = smallRT;
			tex.ReadPixels(new Rect(0f, 0f, smallRT.width, smallRT.height), 0, 0);
			tex.Apply();
			Color[] pixels = tex.GetPixels();
			float num = 0f;
			for (int i = 0; i < pixels.Length; i++)
			{
				float grayscale = pixels[i].grayscale;
				grayscale = Mathf.Clamp(grayscale, 0.05f, 0.95f);
				num += grayscale;
			}
			num /= (float)pixels.Length;
			float value = targetBrightness / (num + 0.001f);
			value = Mathf.Clamp(value, minExposure, maxExposure);
			float num2 = ((value > currentExposure) ? speedUp : speedDown);
			currentExposure = Mathf.Lerp(currentExposure, value, updateInterval * num2);
		}
		material.SetFloat("_Exposure", currentExposure);
		Graphics.Blit(src, dest, material);
	}

	private void OnDestroy()
	{
		if (smallRT != null)
		{
			smallRT.Release();
		}
	}
}
