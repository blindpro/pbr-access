using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ControllerCursorManager : MonoBehaviour
{
	[Header("Tracked UI Screens")]
	[Tooltip("List of UI panels that require the fake cursor when visible.")]
	public GameObject[] uiScreens;

	[Header("References")]
	[Tooltip("Reference to your fake cursor GameObject (the plugin cursor).")]
	public GameObject fakeCursor;

	[Tooltip("Reference to your default EventSystem (used for mouse/keyboard).")]
	public GameObject defaultEventSystem;

	[Header("Controller Events")]
	public UnityEvent OnControllerConnected;

	public UnityEvent OnControllerDisconnected;

	[Header("Debug")]
	public static bool UICursorVisible;

	[SerializeField]
	private bool controllerConnected;

	private bool lastVisibleState;

	private void OnEnable()
	{
		InputSystem.onDeviceChange += OnDeviceChange;
		controllerConnected = Gamepad.current != null;
		UpdateCursorVisibility();
		lastVisibleState = false;
		if (fakeCursor != null)
		{
			fakeCursor.SetActive(value: false);
		}
		if (defaultEventSystem != null)
		{
			defaultEventSystem.SetActive(value: true);
		}
	}

	private void OnDisable()
	{
		InputSystem.onDeviceChange -= OnDeviceChange;
	}

	private void Start()
	{
		lastVisibleState = false;
		if (fakeCursor != null)
		{
			fakeCursor.SetActive(value: false);
		}
		if (defaultEventSystem != null)
		{
			defaultEventSystem.SetActive(value: true);
		}
	}

	private void Update()
	{
		UpdateCursorVisibility();
	}

	private void OnDeviceChange(InputDevice device, InputDeviceChange change)
	{
		if (!(device is Gamepad))
		{
			return;
		}
		switch (change)
		{
		case InputDeviceChange.Added:
		case InputDeviceChange.Reconnected:
			if (!controllerConnected)
			{
				controllerConnected = true;
				Debug.Log("[ControllerCursorManager] Controller connected: " + device.displayName);
				OnControllerConnected.Invoke();
			}
			break;
		case InputDeviceChange.Removed:
		case InputDeviceChange.Disconnected:
			if (controllerConnected)
			{
				controllerConnected = false;
				Debug.Log("[ControllerCursorManager] Controller disconnected: " + device.displayName);
				OnControllerDisconnected.Invoke();
			}
			break;
		}
	}

	private void UpdateCursorVisibility()
	{
		bool flag = IsAnyUIScreenVisible();
		bool flag2 = controllerConnected && flag;
		if (flag2 != lastVisibleState)
		{
			lastVisibleState = flag2;
			UICursorVisible = flag2;
			if (fakeCursor != null)
			{
				fakeCursor.SetActive(flag2);
			}
			if (defaultEventSystem != null)
			{
				defaultEventSystem.SetActive(!flag2);
			}
			Debug.Log($"[ControllerCursorManager] Fake cursor visible = {flag2}");
		}
	}

	private bool IsAnyUIScreenVisible()
	{
		if (uiScreens == null || uiScreens.Length == 0)
		{
			return false;
		}
		GameObject[] array = uiScreens;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null && gameObject.activeInHierarchy)
			{
				return true;
			}
		}
		return false;
	}
}
