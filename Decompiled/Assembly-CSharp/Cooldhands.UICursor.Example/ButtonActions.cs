using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooldhands.UICursor.Example;

public class ButtonActions : MonoBehaviour
{
	public void SetCursorToCenter()
	{
		if (UICursorController.current != null)
		{
			UICursorController.current.CursorPosition = new Vector2(Screen.width / 2, Screen.height / 2);
		}
	}

	public void Log(string value)
	{
		Debug.Log(value);
	}

	public void SetNavigation(bool value)
	{
		EventSystem.current.sendNavigationEvents = value;
	}

	public void Pause(bool value)
	{
		if (value)
		{
			Time.timeScale = 0f;
		}
		else
		{
			Time.timeScale = 1f;
		}
	}
}
