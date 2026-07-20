using UnityEngine;

namespace HP.Generics;

public class WaitUntilBoolTrue : MonoBehaviour, IInitable
{
	public bool isInitDone;

	public bool IsInitDone()
	{
		return isInitDone;
	}
}
