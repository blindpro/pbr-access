using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class DisplayAnimationName : MonoBehaviour
{
	[Tooltip("Text Display.")]
	[SerializeField]
	private TextMeshProUGUI currentAnimationText;

	private Animator cachedAnimator;

	private void Start()
	{
		cachedAnimator = base.gameObject.GetComponent<Animator>();
	}

	private void Update()
	{
		AnimatorClipInfo[] currentAnimatorClipInfo = cachedAnimator.GetCurrentAnimatorClipInfo(0);
		currentAnimationText.text = currentAnimatorClipInfo[0].clip.name;
	}
}
