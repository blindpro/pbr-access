using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class Crosshair : Element
{
	[Tooltip("Object to which all crosshair pieces are parented.")]
	[SerializeField]
	private CanvasGroup crosshairCanvasGroup;

	[Tooltip("Little Dot!")]
	[SerializeField]
	private CanvasGroup dotCanvasGroup;

	[Tooltip("This is the rect transform of the object that actually gets scaled to make the crosshair look bigger.")]
	[SerializeField]
	private RectTransform mainRectTransform;

	[Tooltip("Minimum and maximum scales for the crosshair.")]
	[SerializeField]
	private Vector2 minMaxScale = new Vector2(50f, 200f);

	[Tooltip("Default size of the crosshair. This is the size at which the crosshair stays when nothing is really happening.")]
	[SerializeField]
	private float defaultScale = 50f;

	[Tooltip("Interpolation speed of the crosshair' size.")]
	[SerializeField]
	private float interpolationSpeed = 7f;

	[Tooltip("Interpolation speed of the dot's visibility.")]
	[SerializeField]
	private float interpolationSpeedDot = 50f;

	[Tooltip("Delta size interpolation settings.")]
	[SerializeField]
	private SpringSettings interpolationSizeDelta = SpringSettings.Default();

	[Tooltip("Value used to increase the crosshair' scale while jumping/falling.")]
	[SerializeField]
	private float jumpingScaleAddition = 50f;

	[Tooltip("Value used to increase the crosshair' scale while crouching.")]
	[SerializeField]
	private float crouchingScaleAddition = -20f;

	[Tooltip("Value used to increase the crosshair' scale while moving.")]
	[SerializeField]
	private float movementScaleAddition = 25f;

	[Tooltip("Determines the alpha value of the crosshair while the character is performing some action that disables it.")]
	[SerializeField]
	private float disabledVisibility = 0.6f;

	[Tooltip("Value used to increase the crosshair' scale while running.")]
	[SerializeField]
	private float runningScaleAddition = 15f;

	[Tooltip("Animation curve dictating how the crosshair scales as the character shoots more and more.")]
	[SerializeField]
	private AnimationCurve spreadIncrease;

	private MovementBehaviour movementBehaviour;

	private float crosshairLocalScale;

	private float crosshairVisibility;

	private float dotVisibility;

	private Spring springCrosshairSizeDelta;

	protected override void Awake()
	{
		base.Awake();
		springCrosshairSizeDelta = new Spring();
		crosshairVisibility = 1f;
	}

	protected override void Tick()
	{
		if (crosshairCanvasGroup == null || dotCanvasGroup == null || mainRectTransform == null || characterBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		if ((object)movementBehaviour == null)
		{
			movementBehaviour = characterBehaviour.GetComponent<MovementBehaviour>();
		}
		if (movementBehaviour == null)
		{
			Log.ReferenceError(this, base.gameObject);
			return;
		}
		int shotsFired = characterBehaviour.GetShotsFired();
		float num = characterBehaviour.GetInputMovement().sqrMagnitude * movementScaleAddition;
		float num2 = defaultScale + spreadIncrease.Evaluate(shotsFired);
		float num3 = 1f;
		float num4 = 1f;
		float value = 1f;
		if (characterBehaviour.IsAiming())
		{
			num3 = (value = (num4 = 0f));
		}
		else
		{
			float num5 = ((movementBehaviour.GetVelocity().y >= 0f) ? Mathf.Clamp01(Mathf.Abs(movementBehaviour.GetVelocity().y)) : 1f) * jumpingScaleAddition;
			num2 += (characterBehaviour.IsCrouching() ? crouchingScaleAddition : 0f);
			if (characterBehaviour.IsHolstered())
			{
				num3 = (num4 = 0f);
				value = 1f;
			}
			else if (characterBehaviour.IsRunning())
			{
				num2 += (movementBehaviour.IsGrounded() ? 0f : num5);
				num4 = disabledVisibility;
				num3 = 1f;
				num2 += runningScaleAddition;
			}
			else
			{
				num2 += (movementBehaviour.IsGrounded() ? num : num5);
				num3 = (value = 1f);
				bool flag = characterBehaviour.IsInspecting() || characterBehaviour.IsReloading() || characterBehaviour.IsMeleeing() || characterBehaviour.IsThrowingGrenade();
				if (characterBehaviour.IsLowered())
				{
					flag = true;
				}
				num4 = (flag ? disabledVisibility : 1f);
			}
		}
		dotVisibility = Mathf.Lerp(dotVisibility, Mathf.Clamp01(value), Time.deltaTime * interpolationSpeedDot);
		crosshairLocalScale = Mathf.Lerp(crosshairLocalScale, Mathf.Clamp01(num3), Time.deltaTime * interpolationSpeed);
		crosshairVisibility = Mathf.Lerp(crosshairVisibility, Mathf.Clamp01(num4), Time.deltaTime * interpolationSpeed);
		num2 = Mathf.Clamp(num2, minMaxScale.x, minMaxScale.y);
		springCrosshairSizeDelta.UpdateEndValue(num2 * Vector3.one);
		mainRectTransform.sizeDelta = springCrosshairSizeDelta.Evaluate(interpolationSizeDelta);
		mainRectTransform.localScale = crosshairLocalScale * Vector3.one;
		crosshairCanvasGroup.alpha = crosshairVisibility;
		dotCanvasGroup.alpha = dotVisibility;
	}
}
