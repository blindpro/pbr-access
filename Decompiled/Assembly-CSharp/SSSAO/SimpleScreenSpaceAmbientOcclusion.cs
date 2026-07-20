using System;
using UnityEngine;

namespace SSSAO;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Simple Screen Space Ambient Occlusion")]
public class SimpleScreenSpaceAmbientOcclusion : MonoBehaviour
{
	public enum VisualizationMode
	{
		None,
		ViewAO,
		ViewBleeding
	}

	public enum Quality
	{
		Low,
		Medium,
		High,
		Ultrahigh
	}

	private Camera cam;

	public Texture2D m_AxisPattern;

	[Tooltip("Tints occlusion color.")]
	public Color m_OcclusionColor = Color.black;

	[Tooltip("Quality of the SSAO effect. Higher qualities will consume more resources.")]
	public Quality m_Quality = Quality.High;

	[Tooltip("Maximum reach of the effect in world space.")]
	[Range(0f, 10f)]
	public float m_Radius = 0.2f;

	[Tooltip(" Minimum and maximum radius in pixels. Since the radius is scaled by pixel depth, pixels further away from the camera might get a very small radius and pixels very close to the camera might get a very big radius.Changing this parameter clamps the radius so that it doesn\u00b4t get too small or too big at extreme distances.")]
	[MinMaxRange(0.0001f, 0.5f)]
	public Vector2 m_RadiusRange = new Vector2(0.02f, 0.3f);

	[Tooltip("Width of the occlusion cone considered by each pixel.Set it higher to reduce self-occlusion.")]
	[Range(0f, 1f)]
	public float m_OcclusionBias = 0.05f;

	[Tooltip("Amount of base occlusion. Increasing its value will cause flat surfaces to turn grey, occlusion will be subtracted from  corners and added to crevices, resulting in increased contrast.")]
	[Range(0f, 1f)]
	public float m_OcclusionOffset;

	[Tooltip("Modulates the amount of occlusion contributed by each pixel.")]
	[Range(0f, 20f)]
	public float m_OcclusionIntensity = 2f;

	[Tooltip("Use this to control the shape of the occlusion curve. A value of 1 means linear occlusion, 2 means quadratic occlusion, and so forth.")]
	[Range(0.25f, 10f)]
	public float m_OcclusionExponent = 2f;

	[Tooltip("Modulates the amount of occlusion added at high luminance zones. This prevents brightly iluminated areas from being washed off by SSAO.")]
	[Range(0f, 1f)]
	public float m_LuminanceModulation = 0.8f;

	[Tooltip("Modulates the amount of color bleeding.")]
	[Range(0f, 20f)]
	public float m_BleedingIntensity;

	[Range(1f, 4f)]
	[Tooltip("Amount of downsampling performed when calculating SSAO. Higher is cheaper, but less precise.")]
	public int m_Downsampling = 1;

	[Tooltip("Toggles bilateral blur.")]
	public bool m_Blur = true;

	[Tooltip("Blur will kick in only for samples with less than this difference in depth.")]
	[Range(0f, 2f)]
	public float m_BlurDepthThreshold = 1f;

	[Tooltip("Blur will kick in only for samples with less than this difference in normals.")]
	[Range(0f, 2f)]
	public float m_BlurNormalThreshold = 0.1f;

	public VisualizationMode m_Visualization;

	private Shader m_SSAOShader;

	private Material m_SSAOMaterial;

	private bool m_Supported;

	private string[] keywords = new string[2];

	private static Material CreateMaterial(Shader shader)
	{
		if (!shader)
		{
			return null;
		}
		return new Material(shader)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
	}

	private static void DestroyMaterial(Material mat)
	{
		if ((bool)mat)
		{
			UnityEngine.Object.DestroyImmediate(mat);
			mat = null;
		}
	}

	private void OnDisable()
	{
		DestroyMaterial(m_SSAOMaterial);
	}

	private void Start()
	{
		if (!SystemInfo.supportsImageEffects || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
		{
			m_Supported = false;
			base.enabled = false;
			return;
		}
		CreateMaterials();
		if (!m_SSAOMaterial || m_SSAOMaterial.passCount != 5)
		{
			m_Supported = false;
			base.enabled = false;
		}
		else
		{
			m_Supported = true;
		}
	}

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void OnPreCull()
	{
		cam.depthTextureMode |= DepthTextureMode.DepthNormals;
		cam.depthTextureMode |= DepthTextureMode.Depth;
	}

	private void CreateMaterials()
	{
		if (!m_SSAOShader)
		{
			m_SSAOShader = Shader.Find("Hidden/Simple SSAO");
		}
		if (!m_SSAOShader)
		{
			Debug.LogError("Could not find required SSSAO shader. Cannot initialize Simple SSAO.");
		}
		else if (!m_SSAOMaterial && m_SSAOShader.isSupported)
		{
			m_SSAOMaterial = CreateMaterial(m_SSAOShader);
		}
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!base.gameObject.activeInHierarchy || !base.enabled || !m_Supported)
		{
			Graphics.Blit(source, destination);
			return;
		}
		CreateMaterials();
		if (m_SSAOMaterial == null)
		{
			Graphics.Blit(source, destination);
			return;
		}
		if (m_BleedingIntensity > 0f)
		{
			keywords[1] = "COLORBLEEDING_ON";
		}
		keywords[0] = ((m_Quality == Quality.Low) ? "SAMPLES_2" : ((m_Quality == Quality.Medium) ? "SAMPLES_4" : ((m_Quality == Quality.High) ? "SAMPLES_6" : ((m_Quality == Quality.Ultrahigh) ? "SAMPLES_8" : "SAMPLES_2"))));
		m_SSAOMaterial.shaderKeywords = keywords;
		float farClipPlane = cam.farClipPlane;
		float num;
		float x;
		if (cam.orthographic)
		{
			num = 2f * cam.orthographicSize;
			x = num * cam.aspect;
		}
		else
		{
			num = 2f * Mathf.Tan(cam.fieldOfView * (MathF.PI / 180f) * 0.5f) * farClipPlane;
			x = num * cam.aspect;
		}
		m_SSAOMaterial.SetVector("_FarCorner", new Vector3(x, num, farClipPlane));
		m_SSAOMaterial.SetVector("_Params", new Vector4(m_Radius, m_OcclusionBias, m_OcclusionOffset, 1f / (m_Radius * m_Radius * 10f)));
		m_SSAOMaterial.SetVector("_Params2", new Vector4(m_OcclusionIntensity, m_OcclusionExponent, 0f, m_RadiusRange.x));
		m_SSAOMaterial.SetVector("_Params3", new Vector4(m_BleedingIntensity, 0f, m_BlurDepthThreshold, m_BlurNormalThreshold));
		m_SSAOMaterial.SetVector("_Params4", new Vector4(m_RadiusRange.y, m_LuminanceModulation, 0f, 0f));
		m_SSAOMaterial.SetVector("_InputSize", new Vector2((int)((float)cam.pixelWidth * 0.5f), (int)((float)cam.pixelHeight * 0.5f)));
		m_SSAOMaterial.SetColor("_OcclusionColor", m_OcclusionColor);
		m_SSAOMaterial.SetTexture("_AxisTexture", m_AxisPattern);
		RenderTexture renderTexture = RenderTexture.GetTemporary(source.width / m_Downsampling, source.height / m_Downsampling, 0, RenderTextureFormat.ARGBHalf);
		int num2;
		int num3;
		if ((bool)m_AxisPattern)
		{
			num2 = m_AxisPattern.width;
			num3 = m_AxisPattern.height;
		}
		else
		{
			num2 = 1;
			num3 = 1;
		}
		m_SSAOMaterial.SetVector("_InterleavePatternScale", new Vector2((float)renderTexture.width / (float)num2, (float)renderTexture.height / (float)num3));
		RenderTexture renderTexture2 = null;
		if (m_BleedingIntensity > 0f)
		{
			renderTexture2 = RenderTexture.GetTemporary(renderTexture.width / 2, renderTexture.height / 2, 0, source.format);
			renderTexture2.wrapMode = TextureWrapMode.Clamp;
			renderTexture2.filterMode = FilterMode.Bilinear;
			Graphics.Blit(source, renderTexture2);
			m_SSAOMaterial.SetTexture("_ColorBuffer", renderTexture2);
		}
		Graphics.Blit(source, renderTexture, m_SSAOMaterial, 0);
		if (m_BleedingIntensity > 0f)
		{
			RenderTexture.ReleaseTemporary(renderTexture2);
		}
		if (m_Blur)
		{
			RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0);
			m_SSAOMaterial.SetVector("_TexelOffsetScale", new Vector4(1f / (float)renderTexture.width, 0f, 0f, 0f));
			m_SSAOMaterial.SetTexture("_SSAO", renderTexture);
			Graphics.Blit(null, temporary, m_SSAOMaterial, 1);
			RenderTexture.ReleaseTemporary(renderTexture);
			RenderTexture temporary2 = RenderTexture.GetTemporary(source.width, source.height, 0);
			m_SSAOMaterial.SetVector("_TexelOffsetScale", new Vector4(0f, 1f / (float)renderTexture.height, 0f, 0f));
			m_SSAOMaterial.SetTexture("_SSAO", temporary);
			Graphics.Blit(null, temporary2, m_SSAOMaterial, 1);
			RenderTexture.ReleaseTemporary(temporary);
			renderTexture = temporary2;
		}
		m_SSAOMaterial.SetTexture("_SSAO", renderTexture);
		Graphics.Blit(source, destination, m_SSAOMaterial, (int)(2 + m_Visualization));
		RenderTexture.ReleaseTemporary(renderTexture);
	}
}
