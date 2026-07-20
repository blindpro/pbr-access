using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

[RequireComponent(typeof(MotionApplier))]
public abstract class Motion : MonoBehaviour
{
	[Tooltip("The Motion's alpha. Used to more easily control how much of the motion is applied.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float alpha = 1f;

	[Tooltip("The MotionApplier that will apply this Motion's values.")]
	[SerializeField]
	protected MotionApplier motionApplier;

	public float Alpha => alpha;

	public void SetAlpha(float a)
	{
		alpha = a;
	}

	protected virtual void Awake()
	{
		if (motionApplier == null)
		{
			motionApplier = GetComponent<MotionApplier>();
		}
		if (motionApplier != null)
		{
			motionApplier.Subscribe(this);
		}
	}

	public abstract void Tick();

	public abstract Vector3 GetLocation();

	public abstract Vector3 GetEulerAngles();
}
