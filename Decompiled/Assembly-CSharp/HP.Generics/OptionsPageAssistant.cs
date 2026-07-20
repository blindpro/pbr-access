using UnityEngine;

namespace HP.Generics;

public class OptionsPageAssistant : MonoBehaviour
{
	public GameObject objHuDInfo;

	public GameObject objFPSInfo;

	public void UpdateHudInfoState()
	{
		if ((bool)objHuDInfo)
		{
			objHuDInfo.SetActive(!objHuDInfo.activeSelf);
		}
	}

	public void UpdateFPSInfoState()
	{
		if ((bool)objFPSInfo)
		{
			objFPSInfo.SetActive(!objFPSInfo.activeSelf);
		}
	}

	public void QuitTheApplication()
	{
		Application.Quit();
	}
}
