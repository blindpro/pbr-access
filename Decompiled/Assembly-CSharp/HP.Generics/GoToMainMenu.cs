using UnityEngine;

namespace HP.Generics;

public class GoToMainMenu : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			GetComponent<LoadAScene>().LoadASceneAsync("MainMenu");
		}
	}
}
