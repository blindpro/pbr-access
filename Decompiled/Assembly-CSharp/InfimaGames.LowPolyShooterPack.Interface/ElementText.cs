using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

[RequireComponent(typeof(TextMeshProUGUI))]
public abstract class ElementText : Element
{
	protected TextMeshProUGUI textMesh;

	protected override void Awake()
	{
		base.Awake();
		textMesh = GetComponent<TextMeshProUGUI>();
	}
}
