using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooldhands.UICursor.Example;

[RequireComponent(typeof(Renderer))]
public class Selectable3DCollider : MonoBehaviour, IPointerExitHandler, IEventSystemHandler, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
	private Renderer _rend;

	private Color _defaultColor;

	public Color SelectedColor = Color.red;

	public Color OverColor = Color.cyan;

	private void Start()
	{
		_rend = GetComponent<Renderer>();
		_defaultColor = _rend.material.color;
	}

	public void OnSelect(BaseEventData eventData)
	{
		_rend.material.color = SelectedColor;
	}

	public void OnDeselect(BaseEventData eventData)
	{
		_rend.material.color = _defaultColor;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (EventSystem.current.currentSelectedGameObject != base.gameObject)
		{
			_rend.material.color = _defaultColor;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (EventSystem.current.currentSelectedGameObject != base.gameObject)
		{
			_rend.material.color = OverColor;
		}
	}

	public void OnPointerDown(PointerEventData pointerEventData)
	{
		EventSystem.current.SetSelectedGameObject(base.gameObject, pointerEventData);
	}
}
