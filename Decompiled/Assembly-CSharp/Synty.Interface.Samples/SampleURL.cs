using UnityEngine;

namespace Synty.Interface.Samples;

public class SampleURL : MonoBehaviour
{
	public void OpenURL(string url)
	{
		Application.OpenURL(url);
	}
}
