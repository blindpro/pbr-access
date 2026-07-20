using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Cooldhands;

public class UICursorVirtualMouseInput : MonoBehaviour, IUICursor
{
	private Vector3 m_lastMousePosition = Vector3.zero;

	private float m_referenceResolutionWidth = 800f;

	private bool m_FirstEnabled;

	private GameObject m_lastGameObjectOver;

	private float m_ScreenWidth;

	private float m_ScreenHeight;

	private bool m_UpdateCursor = true;

	private bool m_SetPosition;

	private Vector2 m_GoToPosition = Vector2.zero;

	private MouseState mouseState;

	[Header("Cursor")]
	[SerializeField]
	private UICursorBehaviour m_cursor;

	[Header("Motion")]
	[SerializeField]
	private float m_ScrollSpeed = 45f;

	[Space(10f)]
	[SerializeField]
	private InputActionProperty m_StickAction;

	[SerializeField]
	private InputActionProperty m_ClickAction;

	[SerializeField]
	private InputActionProperty m_ScrollWheelAction;

	private Mouse m_VirtualMouse;

	private Action m_AfterInputUpdateDelegate;

	private Action<InputAction.CallbackContext> m_ButtonActionTriggeredDelegate;

	public UICursorBehaviour Cursor
	{
		get
		{
			return m_cursor;
		}
		set
		{
			m_cursor = value;
		}
	}

	public float scrollSpeed
	{
		get
		{
			return m_ScrollSpeed;
		}
		set
		{
			m_ScrollSpeed = value;
		}
	}

	public Mouse virtualMouse => m_VirtualMouse;

	public InputActionProperty stickAction
	{
		get
		{
			return m_StickAction;
		}
		set
		{
			SetAction(ref m_StickAction, value);
		}
	}

	public InputActionProperty clickAction
	{
		get
		{
			return m_ClickAction;
		}
		set
		{
			if (m_ButtonActionTriggeredDelegate != null)
			{
				SetActionCallback(m_ClickAction, m_ButtonActionTriggeredDelegate, install: false);
			}
			SetAction(ref m_ClickAction, value);
			if (m_ButtonActionTriggeredDelegate != null)
			{
				SetActionCallback(m_ClickAction, m_ButtonActionTriggeredDelegate);
			}
		}
	}

	public InputActionProperty scrollWheelAction
	{
		get
		{
			return m_ScrollWheelAction;
		}
		set
		{
			SetAction(ref m_ScrollWheelAction, value);
		}
	}

	public Vector2 CursorPosition
	{
		get
		{
			if (m_cursor != null)
			{
				return m_cursor.RectTransform.position;
			}
			return Vector2.zero;
		}
		set
		{
			SetCursorPosition(value);
		}
	}

	internal void SetCursorPosition(Vector2 position)
	{
		m_SetPosition = true;
		m_GoToPosition = position;
	}

	protected void OnEnable()
	{
		UICursorController.current = this;
		m_FirstEnabled = true;
		m_ScreenWidth = Screen.width;
		m_ScreenHeight = Screen.height;
		if (m_VirtualMouse == null)
		{
			m_VirtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
		}
		else if (!m_VirtualMouse.added)
		{
			InputSystem.AddDevice(m_VirtualMouse);
		}
		if (m_AfterInputUpdateDelegate == null)
		{
			m_AfterInputUpdateDelegate = OnAfterInputUpdate;
		}
		InputSystem.onAfterUpdate += m_AfterInputUpdateDelegate;
		if (m_ButtonActionTriggeredDelegate == null)
		{
			m_ButtonActionTriggeredDelegate = OnButtonActionTriggered;
		}
		SetActionCallback(m_ClickAction, m_ButtonActionTriggeredDelegate);
		m_StickAction.action?.Enable();
		m_ClickAction.action?.Enable();
		m_ScrollWheelAction.action?.Enable();
	}

	protected void OnDisable()
	{
		UICursorController.current = null;
		m_FirstEnabled = false;
		if (m_StickAction.reference == null)
		{
			m_StickAction.action?.Disable();
		}
		if (m_ClickAction.reference == null)
		{
			m_ClickAction.action?.Disable();
		}
		if (m_ScrollWheelAction.reference == null)
		{
			m_ScrollWheelAction.action?.Disable();
		}
		if (m_ButtonActionTriggeredDelegate != null)
		{
			SetActionCallback(m_ClickAction, m_ButtonActionTriggeredDelegate, install: false);
		}
		if (m_VirtualMouse != null && m_VirtualMouse.added)
		{
			InputSystem.RemoveDevice(m_VirtualMouse);
		}
		if (m_AfterInputUpdateDelegate != null)
		{
			InputSystem.onAfterUpdate -= m_AfterInputUpdateDelegate;
		}
	}

	private void ProcessOverGameObject(PointerEventData pointerEvent)
	{
		if (pointerEvent != null && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
		{
			if (m_lastGameObjectOver != pointerEvent.pointerCurrentRaycast.gameObject)
			{
				GameObject eventHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(pointerEvent.pointerCurrentRaycast.gameObject);
				if (eventHandler == null)
				{
					eventHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerEvent.pointerCurrentRaycast.gameObject);
				}
				if (eventHandler != null)
				{
					m_cursor.ClickableElement = pointerEvent.pointerCurrentRaycast.gameObject;
				}
				else
				{
					m_cursor.ClickableElement = null;
				}
				m_cursor.OnClickableElementChanged();
				m_lastGameObjectOver = pointerEvent.pointerCurrentRaycast.gameObject;
			}
		}
		else
		{
			if (m_lastGameObjectOver != null)
			{
				m_cursor.ClickableElement = null;
				m_cursor.OnClickableElementChanged();
			}
			m_lastGameObjectOver = null;
		}
	}

	private void OnApplicationFocus(bool focusStatus)
	{
		m_UpdateCursor = focusStatus;
	}

	private void UpdateMotion()
	{
		if (m_VirtualMouse == null)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		if (Mouse.current != null)
		{
			vector = Mouse.current.position.ReadValue();
		}
		if (m_FirstEnabled)
		{
			m_FirstEnabled = false;
			Vector3 position = m_cursor.RectTransform.position;
			InputState.Change(m_VirtualMouse.position, position);
		}
		InputAction action = m_StickAction.action;
		if (action == null)
		{
			return;
		}
		Vector2 vector2 = action.ReadValue<Vector2>();
		bool flag = Mathf.Approximately(0f, vector2.x) && Mathf.Approximately(0f, vector2.y);
		if ((flag && m_lastMousePosition != vector) || Mouse.current == null)
		{
			flag = false;
		}
		if (!flag || m_SetPosition)
		{
			_ = InputState.currentTime;
			Vector2 vector3 = m_ScreenWidth * m_cursor.m_Speed / m_referenceResolutionWidth * vector2 * Time.unscaledDeltaTime;
			Vector2 vector4 = m_VirtualMouse.position.ReadValue();
			if (Mouse.current != null && m_lastMousePosition != vector)
			{
				vector3 = vector - (Vector3)vector4;
			}
			if (m_SetPosition)
			{
				vector3 = m_GoToPosition - vector4;
				m_SetPosition = false;
			}
			Vector2 vector5 = vector4 + vector3;
			vector5.x = Mathf.Clamp(vector5.x, 0f, m_ScreenWidth);
			vector5.y = Mathf.Clamp(vector5.y, 0f, m_ScreenHeight);
			InputState.Change(m_VirtualMouse.position, vector5);
			InputState.Change(m_VirtualMouse.delta, vector3);
			ProcessOverGameObject(UICursorOverRaycaster.LastPointerEventData);
			if (m_cursor != null)
			{
				m_cursor.RectTransform.position = vector5;
			}
		}
		m_lastMousePosition = vector;
		InputAction action2 = m_ScrollWheelAction.action;
		if (action2 != null)
		{
			Vector2 state = action2.ReadValue<Vector2>();
			state.x *= m_ScrollSpeed;
			state.y *= m_ScrollSpeed;
			InputState.Change(m_VirtualMouse.scroll, state);
		}
	}

	private void SendEventAction(InputAction.CallbackContext context)
	{
		if (m_VirtualMouse == null)
		{
			return;
		}
		InputAction action = context.action;
		MouseButton? mouseButton = null;
		if (action == m_ClickAction.action)
		{
			mouseButton = MouseButton.Left;
		}
		if (!mouseButton.HasValue)
		{
			return;
		}
		bool flag = context.control.IsPressed();
		if (m_VirtualMouse.leftButton.isPressed != flag || context.canceled)
		{
			m_VirtualMouse.CopyState<MouseState>(out mouseState);
			mouseState.WithButton(mouseButton.Value, flag);
			InputState.Change(m_VirtualMouse, mouseState);
			if (m_cursor != null && !flag)
			{
				m_cursor.OnClick();
				InputState.Change(m_VirtualMouse.position, m_cursor.RectTransform.position);
				ProcessOverGameObject(UICursorOverRaycaster.LastPointerEventData);
			}
		}
	}

	private void OnButtonActionTriggered(InputAction.CallbackContext context)
	{
		if (EventSystem.current != null && !EventSystem.current.sendNavigationEvents)
		{
			SendEventAction(context);
		}
	}

	private static void SetActionCallback(InputActionProperty field, Action<InputAction.CallbackContext> callback, bool install = true)
	{
		InputAction action = field.action;
		if (action != null)
		{
			if (install)
			{
				action.performed += callback;
			}
			else
			{
				action.performed -= callback;
			}
		}
	}

	private static void SetAction(ref InputActionProperty field, InputActionProperty value)
	{
		InputActionProperty inputActionProperty = field;
		field = value;
		if (!(inputActionProperty.reference == null))
		{
			return;
		}
		InputAction action = inputActionProperty.action;
		if (action != null && action.enabled)
		{
			action.Disable();
			if (value.reference == null)
			{
				value.action?.Enable();
			}
		}
	}

	private void Update()
	{
		m_ScreenWidth = Screen.width;
		m_ScreenHeight = Screen.height;
	}

	private void OnAfterInputUpdate()
	{
		if (EventSystem.current != null && !EventSystem.current.sendNavigationEvents && m_UpdateCursor)
		{
			UpdateMotion();
		}
	}
}
