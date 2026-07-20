using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cooldhands.UICursor.Example;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerExitHandler
{
	public Image m_fillImage;

	public float m_waitTimeToHold = 0.2f;

	public UnityEvent OnHold;

	private float m_waitTime;

	private float m_progressWait = 1f;

	private bool m_holdAction;

	private bool m_holdFinish;

	public void OnPointerDown(PointerEventData pointerEventData)
	{
		m_waitTime = Time.time + m_waitTimeToHold;
		m_holdAction = true;
		m_holdFinish = false;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		m_holdAction = false;
	}

	public void OnPointerUp(PointerEventData pointerEventData)
	{
		m_holdAction = false;
	}

	private void Update()
	{
		if (m_holdAction)
		{
			if (Time.time > m_waitTime && !m_holdFinish)
			{
				m_fillImage.fillAmount += 1f / m_progressWait * Time.deltaTime;
				if (m_fillImage.fillAmount >= 1f && OnHold != null)
				{
					m_holdFinish = true;
					OnHold.Invoke();
				}
			}
		}
		else if (m_fillImage.fillAmount > 0f)
		{
			m_fillImage.fillAmount = 0f;
		}
	}
}
