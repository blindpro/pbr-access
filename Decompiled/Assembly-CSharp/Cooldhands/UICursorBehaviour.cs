using UnityEngine;

namespace Cooldhands;

[RequireComponent(typeof(RectTransform))]
public abstract class UICursorBehaviour : MonoBehaviour
{
	[Tooltip("Cursor speed for movement")]
	public float m_Speed = 450f;

	private RectTransform m_RectTransform;

	public GameObject ClickableElement { get; set; }

	public RectTransform RectTransform => m_RectTransform;

	public bool IsOverClickeableElement
	{
		get
		{
			if (!(ClickableElement != null))
			{
				return false;
			}
			return true;
		}
	}

	protected virtual void Awake()
	{
		m_RectTransform = GetComponent<RectTransform>();
	}

	public abstract void OnClickableElementChanged();

	public abstract void OnClick();
}
