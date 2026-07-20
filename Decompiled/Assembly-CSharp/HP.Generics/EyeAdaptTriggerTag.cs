using UnityEngine;

namespace HP.Generics;

public class EyeAdaptTriggerTag : MonoBehaviour
{
	public EyeAdaptationTrigger eyeAdaptation;

	public bool insideOnly;

	private void OnTriggerEnter(Collider other)
	{
		eyeAdaptation.EyeAdaptationTransition(insideOnly);
	}
}
