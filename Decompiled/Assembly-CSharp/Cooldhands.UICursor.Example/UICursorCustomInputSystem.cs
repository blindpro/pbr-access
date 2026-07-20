using UnityEngine;

namespace Cooldhands.UICursor.Example;

public class UICursorCustomInputSystem : UICursorStandaloneInputModule
{
	protected override float GetHorizontalAxis()
	{
		return Input.GetAxis(base.horizontalAxis);
	}

	protected override float GetVerticalAxis()
	{
		return Input.GetAxis(base.verticalAxis);
	}

	protected override float GetScrollAxis()
	{
		return Input.GetAxisRaw(base.scrollAxis);
	}

	protected override bool GetClickButtonDown()
	{
		if (!Input.GetButtonDown(base.clickButton))
		{
			return Input.GetButtonDown(base.submitButton);
		}
		return true;
	}

	protected override bool GetClickButtonUp()
	{
		if (!Input.GetButtonUp(base.clickButton))
		{
			return Input.GetButtonUp(base.submitButton);
		}
		return true;
	}
}
