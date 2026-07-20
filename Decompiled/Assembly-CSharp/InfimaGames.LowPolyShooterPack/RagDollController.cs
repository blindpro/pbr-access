using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class RagDollController : MonoBehaviour
{
	public Animator animator;

	public bool UseRagDoll = true;

	private new bool enabled;

	private CharacterMultiplayer characterMultiplayer;

	private InputSimulator inputSimulator;

	private void Awake()
	{
		characterMultiplayer = GetComponent<CharacterMultiplayer>();
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void EnableRagDoll(bool enable)
	{
		if (enable && !UseRagDoll)
		{
			enable = false;
		}
		if ((bool)animator)
		{
			animator.enabled = !enable;
			if (animator.enabled)
			{
				animator.Update(Time.deltaTime);
			}
			Rigidbody[] componentsInChildren = animator.GetComponentsInChildren<Rigidbody>();
			foreach (Rigidbody obj in componentsInChildren)
			{
				obj.useGravity = enable;
				obj.angularVelocity = Vector3.zero;
				obj.velocity = Vector3.zero;
				obj.isKinematic = !enable;
				obj.angularDrag = 0f;
				obj.drag = 4f;
			}
			Collider[] componentsInChildren2 = animator.GetComponentsInChildren<Collider>();
			foreach (Collider obj2 in componentsInChildren2)
			{
				obj2.enabled = false;
				obj2.enabled = true;
			}
			ConfigurableJoint[] componentsInChildren3 = animator.GetComponentsInChildren<ConfigurableJoint>();
			foreach (ConfigurableJoint obj3 in componentsInChildren3)
			{
				obj3.enableCollision = false;
				obj3.xMotion = ConfigurableJointMotion.Locked;
				obj3.yMotion = ConfigurableJointMotion.Locked;
				obj3.zMotion = ConfigurableJointMotion.Locked;
			}
			CharacterJoint[] componentsInChildren4 = animator.GetComponentsInChildren<CharacterJoint>();
			foreach (CharacterJoint obj4 in componentsInChildren4)
			{
				obj4.enableCollision = false;
				obj4.enableProjection = true;
			}
		}
	}
}
