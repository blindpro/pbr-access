using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class TextTutorial : ElementText
{
	[Tooltip("Tutorial prompt text.")]
	[SerializeField]
	private TextMeshProUGUI prompt;

	[Tooltip("Tutorial text.")]
	[SerializeField]
	private TextMeshProUGUI tutorial;

	protected override void Awake()
	{
		base.Awake();
		prompt.enabled = true;
		tutorial.enabled = false;
	}

	protected override void Tick()
	{
		bool flag = characterBehaviour.IsTutorialTextVisible();
		prompt.enabled = !flag;
		tutorial.enabled = flag;
	}
}
