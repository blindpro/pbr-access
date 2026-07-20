using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class CustomizeRow : MonoBehaviour
{
	public bool isWeapon;

	public Text gradeTxt;

	public Text coinsTxt;

	public Transform unlockUI;

	public Text weaponNameTxt;

	public Text weaponShortDescTxt;

	public Text weaponGrade;

	public Slider weaponExp;

	public Button rowButton;

	private ProgressionManager progressionManager;

	private WeaponsSkinsManager weaponsSkinsManager;

	private void OnEnable()
	{
	}

	private void Start()
	{
		UpdateWeapon();
	}

	private void Update()
	{
	}

	public void OnSelectWeapon()
	{
		GameManager.Instance.GetComponent<PickupsManager>().GetWeaponShortDesc(weaponNameTxt.text);
		progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
		weaponsSkinsManager = GameManager.Instance.GetComponent<WeaponsSkinsManager>();
		weaponsSkinsManager.OnSelectWeapon(weaponNameTxt.text, resetSkin: true);
	}

	public void UpdateWeapon()
	{
		if ((bool)GameManager.Instance)
		{
			progressionManager = GameManager.Instance.GetComponent<ProgressionManager>();
			weaponsSkinsManager = GameManager.Instance.GetComponent<WeaponsSkinsManager>();
			if (isWeapon)
			{
				string weaponShortDesc = GameManager.Instance.GetComponent<PickupsManager>().GetWeaponShortDesc(weaponNameTxt.text);
				weaponShortDescTxt.text = weaponShortDesc;
				weaponExp.value = (float)progressionManager.GetWeaponExp(weaponShortDesc) / (float)progressionManager.GetWeaponExpToGrade(weaponShortDesc);
				weaponGrade.text = "Lv " + progressionManager.GetWeaponGrade(weaponShortDesc);
			}
		}
	}
}
