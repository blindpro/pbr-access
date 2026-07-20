using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class LoginManager : MonoBehaviour
{
	public GameObject LoginPopup;

	public GameObject LoginPopupButton;

	public InputField loginField;

	public InputField passwordField;

	public InputField emailField;

	public Text log;

	public Text error;

	private void Start()
	{
	}

	private void OnSplashScreenHidden()
	{
		AutoLogin();
	}

	private void Update()
	{
		if (DataManager.IsLoggedIn())
		{
			LoginPopup.SetActive(value: false);
			LoginPopupButton.SetActive(value: false);
		}
	}

	public void AutoLogin()
	{
	}

	public void Login(bool create = false)
	{
	}

	private void OnLoginCallBack(bool success)
	{
	}
}
