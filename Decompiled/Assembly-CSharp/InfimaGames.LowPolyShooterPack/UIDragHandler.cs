using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
	public enum DragSource
	{
		pickups,
		myInventory,
		equipments
	}

	public PickupsManager.Item item;

	public DragSource dragSource;

	public bool isWeapon1;

	[Header("Drag Settings")]
	public Canvas canvas;

	public Image draggableImage;

	private Image cloneImage;

	private RectTransform cloneRect;

	private Vector2 pointerOffset;

	private PickupsManager pickupsManager;

	private void Awake()
	{
		draggableImage = GetComponent<Image>();
	}

	private void Start()
	{
		pickupsManager = GameManager.Instance.GetComponent<PickupsManager>();
		canvas = GetComponentInParent<Canvas>();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		pickupsManager.OnBeginDrag(this);
		cloneImage = Object.Instantiate(draggableImage, canvas.transform, worldPositionStays: false);
		cloneRect = cloneImage.rectTransform;
		cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
		cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
		cloneRect.pivot = new Vector2(0.5f, 0.5f);
		cloneRect.sizeDelta = new Vector2(80f, 80f);
		cloneImage.raycastTarget = false;
		Color color = cloneImage.color;
		color.a = 0.8f;
		cloneImage.color = color;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(cloneRect, eventData.position, eventData.pressEventCamera, out pointerOffset);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (cloneRect != null)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
			cloneRect.anchoredPosition = localPoint;
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventData, list);
		Image image = null;
		foreach (RaycastResult item in list)
		{
			image = item.gameObject.GetComponent<Image>();
			if (image != null && image != cloneImage)
			{
				Debug.Log($"Dropped on UI Image: {image.name} dragSource:{dragSource} item:{this.item.type}");
				break;
			}
		}
		if (cloneImage != null)
		{
			Object.Destroy(cloneImage.gameObject);
		}
		pickupsManager.OnEndDrag(this, image);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		draggableImage.color = Color.yellow;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		draggableImage.color = Color.white;
	}
}
