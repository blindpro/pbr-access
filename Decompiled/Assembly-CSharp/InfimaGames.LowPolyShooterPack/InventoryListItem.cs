using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class InventoryListItem : MonoBehaviour
{
	public Text lv;

	public Text description;

	public Image image;

	public PickupsManager.Item item;

	public CanvasGroup canvasGroup;

	private void Start()
	{
	}

	private void Update()
	{
	}
}
