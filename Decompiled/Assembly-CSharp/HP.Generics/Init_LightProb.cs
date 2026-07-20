using UnityEngine;

namespace HP.Generics;

public class Init_LightProb : MonoBehaviour
{
	private void Start()
	{
		LightProbes.TetrahedralizeAsync();
	}
}
