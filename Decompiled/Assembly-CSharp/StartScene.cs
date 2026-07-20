using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScene : MonoBehaviour
{
	public string startScene = "PolygonBitBR_Desert";

	private void Start()
	{
		string text = PlayerPrefs.GetString("RestartScene", startScene);
		Debug.LogWarning("starting from scene " + text);
		SceneManager.LoadScene(text);
	}
}
