using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooldhands;

public abstract class UICursorInputModule : BaseInputModule
{
	protected class ButtonState
	{
		private PointerEventData.InputButton m_Button;

		private MouseButtonEventData m_EventData;

		public MouseButtonEventData eventData
		{
			get
			{
				return m_EventData;
			}
			set
			{
				m_EventData = value;
			}
		}

		public PointerEventData.InputButton button
		{
			get
			{
				return m_Button;
			}
			set
			{
				m_Button = value;
			}
		}
	}

	protected class MouseState
	{
		private List<ButtonState> m_TrackedButtons = new List<ButtonState>();

		public bool AnyPressesThisFrame()
		{
			for (int i = 0; i < m_TrackedButtons.Count; i++)
			{
				if (m_TrackedButtons[i].eventData.PressedThisFrame())
				{
					return true;
				}
			}
			return false;
		}

		public bool AnyReleasesThisFrame()
		{
			for (int i = 0; i < m_TrackedButtons.Count; i++)
			{
				if (m_TrackedButtons[i].eventData.ReleasedThisFrame())
				{
					return true;
				}
			}
			return false;
		}

		public ButtonState GetButtonState(PointerEventData.InputButton button)
		{
			ButtonState buttonState = null;
			for (int i = 0; i < m_TrackedButtons.Count; i++)
			{
				if (m_TrackedButtons[i].button == button)
				{
					buttonState = m_TrackedButtons[i];
					break;
				}
			}
			if (buttonState == null)
			{
				buttonState = new ButtonState
				{
					button = button,
					eventData = new MouseButtonEventData()
				};
				m_TrackedButtons.Add(buttonState);
			}
			return buttonState;
		}

		public void SetButtonState(PointerEventData.InputButton button, PointerEventData.FramePressState stateForMouseButton, PointerEventData data)
		{
			ButtonState buttonState = GetButtonState(button);
			buttonState.eventData.buttonState = stateForMouseButton;
			buttonState.eventData.buttonData = data;
		}
	}

	public class MouseButtonEventData
	{
		public PointerEventData.FramePressState buttonState;

		public PointerEventData buttonData;

		public bool PressedThisFrame()
		{
			if (buttonState != PointerEventData.FramePressState.Pressed)
			{
				return buttonState == PointerEventData.FramePressState.PressedAndReleased;
			}
			return true;
		}

		public bool ReleasedThisFrame()
		{
			if (buttonState != PointerEventData.FramePressState.Released)
			{
				return buttonState == PointerEventData.FramePressState.PressedAndReleased;
			}
			return true;
		}
	}

	protected Dictionary<int, PointerEventData> m_PointerData = new Dictionary<int, PointerEventData>();

	private readonly MouseState m_MouseState = new MouseState();

	private bool m_previousSendNavigationEvents;

	public const int kMouseLeftId = -1;

	public const int kMouseRightId = -2;

	public const int kMouseMiddleId = -3;

	public const int kFakeTouchesId = -4;

	[SerializeField]
	[Tooltip("Name of the horizontal axis for cursor movement")]
	protected string m_HorizontalAxis = "Horizontal";

	[SerializeField]
	[Tooltip("Name of the vertical axis for cursor movement")]
	protected string m_VerticalAxis = "Vertical";

	[SerializeField]
	[Tooltip("Name of the button for click")]
	protected string m_ClickButton = "Submit";

	[SerializeField]
	[Tooltip("Name of the axis for scroll")]
	protected string m_ScrollAxis = "Mouse ScrollWheel";

	protected float m_inputX;

	protected float m_inputY;

	protected Vector2 m_Scroll = Vector2.zero;

	protected Vector2 m_CursorPosition;

	[Tooltip("Cursor image")]
	public UICursorBehaviour m_cursor;

	private Vector2 m_PreviousMousePosition;

	[Tooltip("Speed on scrolling")]
	public float m_scrollSpeed = 5f;

	public string horizontalAxis
	{
		get
		{
			return m_HorizontalAxis;
		}
		set
		{
			m_HorizontalAxis = value;
		}
	}

	public string verticalAxis
	{
		get
		{
			return m_VerticalAxis;
		}
		set
		{
			m_VerticalAxis = value;
		}
	}

	public string clickButton
	{
		get
		{
			return m_ClickButton;
		}
		set
		{
			m_ClickButton = value;
		}
	}

	public string scrollAxis
	{
		get
		{
			return m_ScrollAxis;
		}
		set
		{
			m_ScrollAxis = value;
		}
	}

	protected bool GetPointerData(int id, out PointerEventData data, bool create)
	{
		if (m_PointerData.TryGetValue(id, out data) || !create)
		{
			return false;
		}
		data = new PointerEventData(base.eventSystem)
		{
			pointerId = id
		};
		m_PointerData.Add(id, data);
		return true;
	}

	protected virtual float GetHorizontalAxis()
	{
		return Input.GetAxis(horizontalAxis);
	}

	protected virtual float GetVerticalAxis()
	{
		return Input.GetAxis(verticalAxis);
	}

	protected virtual float GetScrollAxis()
	{
		return Input.GetAxisRaw(scrollAxis);
	}

	protected virtual bool GetClickButtonDown()
	{
		return Input.GetButtonDown(clickButton);
	}

	protected virtual bool GetClickButtonUp()
	{
		return Input.GetButtonUp(clickButton);
	}

	protected void RemovePointerData(PointerEventData data)
	{
		m_PointerData.Remove(data.pointerId);
	}

	protected PointerEventData GetTouchPointerEventData(Touch input, out bool pressed, out bool released)
	{
		PointerEventData data;
		bool pointerData = GetPointerData(input.fingerId, out data, create: true);
		data.Reset();
		pressed = pointerData || input.phase == TouchPhase.Began;
		released = input.phase == TouchPhase.Canceled || input.phase == TouchPhase.Ended;
		if (pointerData)
		{
			data.position = input.position;
		}
		data.delta = ((!pressed) ? (input.position - data.position) : Vector2.zero);
		data.position = input.position;
		data.button = PointerEventData.InputButton.Left;
		base.eventSystem.RaycastAll(data, m_RaycastResultCache);
		RaycastResult pointerCurrentRaycast = BaseInputModule.FindFirstRaycast(m_RaycastResultCache);
		data.pointerCurrentRaycast = pointerCurrentRaycast;
		m_CursorPosition = data.position;
		m_RaycastResultCache.Clear();
		return data;
	}

	protected void CopyFromTo(PointerEventData from, PointerEventData to)
	{
		to.position = from.position;
		to.delta = from.delta;
		to.scrollDelta = from.scrollDelta;
		to.pointerCurrentRaycast = from.pointerCurrentRaycast;
		to.pointerEnter = from.pointerEnter;
	}

	protected static PointerEventData.FramePressState StateForMouseButton(int buttonId, UICursorInputModule inputModule)
	{
		bool flag = false;
		bool flag2 = false;
		if (!inputModule.eventSystem.sendNavigationEvents)
		{
			flag = inputModule.GetClickButtonDown();
			flag2 = inputModule.GetClickButtonUp();
		}
		else if (!inputModule.m_previousSendNavigationEvents)
		{
			flag2 = true;
		}
		inputModule.m_previousSendNavigationEvents = inputModule.eventSystem.sendNavigationEvents;
		if (Input.mousePresent)
		{
			if (!flag || buttonId > 0)
			{
				flag = Input.GetMouseButtonDown(buttonId);
			}
			if (!flag2 || buttonId > 0)
			{
				flag2 = Input.GetMouseButtonUp(buttonId);
			}
		}
		if (flag && flag2)
		{
			return PointerEventData.FramePressState.PressedAndReleased;
		}
		if (flag)
		{
			if (buttonId == 0)
			{
				inputModule.OnClickPress();
			}
			return PointerEventData.FramePressState.Pressed;
		}
		if (!flag2)
		{
			return PointerEventData.FramePressState.NotChanged;
		}
		return PointerEventData.FramePressState.Released;
	}

	protected virtual MouseState GetMousePointerEventData()
	{
		return GetMousePointerEventData(0);
	}

	protected abstract void OnClickPress();

	protected virtual MouseState GetMousePointerEventData(int id)
	{
		if (!base.eventSystem.sendNavigationEvents)
		{
			m_inputX = GetHorizontalAxis();
			m_inputY = GetVerticalAxis();
		}
		m_Scroll.y = GetScrollAxis();
		if (m_inputX == 0f && m_inputY == 0f && base.input.mousePosition != m_PreviousMousePosition)
		{
			m_CursorPosition = base.input.mousePosition;
		}
		m_PreviousMousePosition = base.input.mousePosition;
		SetCursorBounds();
		PointerEventData data;
		bool pointerData = GetPointerData(-1, out data, create: true);
		data.Reset();
		Vector2 cursorPosition = m_CursorPosition;
		if (pointerData)
		{
			data.position = cursorPosition;
		}
		data.delta = cursorPosition - data.position;
		data.position = cursorPosition;
		data.scrollDelta = Input.mouseScrollDelta;
		if (!Mathf.Approximately(m_Scroll.sqrMagnitude, 0f))
		{
			m_Scroll.y *= m_scrollSpeed;
			data.scrollDelta = m_Scroll;
		}
		data.button = PointerEventData.InputButton.Left;
		base.eventSystem.RaycastAll(data, m_RaycastResultCache);
		RaycastResult pointerCurrentRaycast = BaseInputModule.FindFirstRaycast(m_RaycastResultCache);
		data.pointerCurrentRaycast = pointerCurrentRaycast;
		m_RaycastResultCache.Clear();
		GetPointerData(-2, out var data2, create: true);
		CopyFromTo(data, data2);
		data2.button = PointerEventData.InputButton.Right;
		GetPointerData(-3, out var data3, create: true);
		CopyFromTo(data, data3);
		data3.button = PointerEventData.InputButton.Middle;
		m_MouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0, this), data);
		m_MouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1, this), data2);
		m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2, this), data3);
		return m_MouseState;
	}

	protected void SetCursorBounds()
	{
		if (m_CursorPosition.x < 0f)
		{
			m_CursorPosition.x = 0f;
		}
		if (m_CursorPosition.y < 0f)
		{
			m_CursorPosition.y = 0f;
		}
		if (m_CursorPosition.x > (float)Screen.width)
		{
			m_CursorPosition.x = Screen.width;
		}
		if (m_CursorPosition.y > (float)Screen.height)
		{
			m_CursorPosition.y = Screen.height;
		}
	}

	protected PointerEventData GetLastPointerEventData(int id)
	{
		GetPointerData(id, out var data, create: false);
		return data;
	}

	private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
	{
		if (!useDragThreshold)
		{
			return true;
		}
		return (double)(pressPos - currentPos).sqrMagnitude >= (double)threshold * (double)threshold;
	}

	protected virtual void ProcessMove(PointerEventData pointerEvent)
	{
		GameObject newEnterTarget = pointerEvent.pointerCurrentRaycast.gameObject;
		HandlePointerExitAndEnter(pointerEvent, newEnterTarget);
	}

	protected virtual void ProcessDrag(PointerEventData pointerEvent)
	{
		bool flag = pointerEvent.IsPointerMoving();
		if (flag && pointerEvent.pointerDrag != null && !pointerEvent.dragging && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, base.eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
		{
			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
			pointerEvent.dragging = true;
		}
		if (pointerEvent.dragging && flag && pointerEvent.pointerDrag != null)
		{
			if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
			{
				ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
				pointerEvent.eligibleForClick = false;
				pointerEvent.pointerPress = null;
				pointerEvent.rawPointerPress = null;
			}
			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
		}
	}

	public override bool IsPointerOverGameObject(int pointerId)
	{
		PointerEventData lastPointerEventData = GetLastPointerEventData(pointerId);
		if (lastPointerEventData != null)
		{
			return lastPointerEventData.pointerEnter != null;
		}
		return false;
	}

	protected void ClearSelection()
	{
		BaseEventData baseEventData = GetBaseEventData();
		foreach (PointerEventData value in m_PointerData.Values)
		{
			HandlePointerExitAndEnter(value, null);
		}
		m_PointerData.Clear();
		base.eventSystem.SetSelectedGameObject(null, baseEventData);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder("<b>Pointer Input Module of type: </b>" + GetType());
		stringBuilder.AppendLine();
		foreach (KeyValuePair<int, PointerEventData> pointerDatum in m_PointerData)
		{
			if (pointerDatum.Value != null)
			{
				stringBuilder.AppendLine("<B>Pointer:</b> " + pointerDatum.Key);
				stringBuilder.AppendLine(pointerDatum.Value.ToString());
			}
		}
		return stringBuilder.ToString();
	}

	protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
	{
		if (ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo) != base.eventSystem.currentSelectedGameObject)
		{
			base.eventSystem.SetSelectedGameObject(null, pointerEvent);
		}
	}
}
