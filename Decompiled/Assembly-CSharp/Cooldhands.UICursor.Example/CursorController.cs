using UnityEngine;

namespace Cooldhands.UICursor.Example;

public class CursorController : UICursorBehaviour
{
	public Animator m_animator;

	public override void OnClickableElementChanged()
	{
		if (m_animator != null && m_animator.isActiveAndEnabled)
		{
			m_animator.SetBool("Hover", base.IsOverClickeableElement);
		}
	}

	public override void OnClick()
	{
		if (m_animator != null && m_animator.isActiveAndEnabled)
		{
			m_animator.SetTrigger("Click");
		}
	}
}
