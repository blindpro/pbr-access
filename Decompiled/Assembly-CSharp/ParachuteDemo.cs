using UnityEngine;
using UnityEngine.UI;

public class ParachuteDemo : MonoBehaviour
{
	public Animator animator;

	public Animator parachuteAnimator;

	public SkinnedMeshRenderer dummyParachute;

	public Slider horizontal;

	public Slider vertical;

	private bool isDropship = true;

	public bool freefalling;

	private void Start()
	{
		dummyParachute.enabled = false;
	}

	private void Update()
	{
		animator.SetFloat("Locomotion X", horizontal.value);
		animator.SetFloat("Locomotion Y", vertical.value);
		parachuteAnimator.SetFloat("Locomotion X", horizontal.value);
		parachuteAnimator.SetFloat("Locomotion Y", vertical.value);
		if (Input.GetKeyDown("space"))
		{
			if (isDropship)
			{
				animator.SetTrigger("DropJump");
				isDropship = false;
				freefalling = true;
				return;
			}
			if (freefalling)
			{
				animator.SetBool("Parachute", value: true);
				parachuteAnimator.SetBool("Parachute", value: true);
				dummyParachute.enabled = true;
				freefalling = false;
				return;
			}
			if (!freefalling)
			{
				animator.SetBool("Parachute", value: false);
				parachuteAnimator.SetBool("Parachute", value: false);
				freefalling = true;
			}
		}
		if (Input.GetKeyDown("v") && !freefalling)
		{
			animator.SetTrigger("Land");
			parachuteAnimator.SetBool("Parachute", value: false);
		}
		if (Input.GetKeyDown("b") && !freefalling)
		{
			animator.SetBool("Grounded", value: true);
		}
	}

	public void HideParachute()
	{
		dummyParachute.enabled = false;
	}
}
