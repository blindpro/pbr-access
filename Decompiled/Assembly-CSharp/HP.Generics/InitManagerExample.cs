using UnityEngine;

namespace HP.Generics;

public class InitManagerExample : MonoBehaviour, IInitable
{
	public bool toggle;

	public void DisplayTextWhenSceneStart()
	{
		Debug.Log("Gameplay scene starts");
	}

	public void DoSomethingWhenSceneIsInitialized()
	{
		Debug.Log("Gameplay scene is initialized");
	}

	public bool IsInitDone()
	{
		return toggle;
	}
}
