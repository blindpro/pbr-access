using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class DisplayMaterialName : MonoBehaviour
{
	[Tooltip("Mesh.")]
	[SerializeField]
	private Renderer mesh;

	[Tooltip("Text.")]
	[SerializeField]
	private TextMeshProUGUI materialText;

	private Material meshMaterial;

	private void Start()
	{
		string text = mesh.sharedMaterial.name;
		materialText.text = text;
	}
}
