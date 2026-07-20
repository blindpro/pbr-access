using UnityEngine;
using UnityEngine.Events;

public class LifecycleEvents : MonoBehaviour
{
	[Header("Unity Events Called During Unity Lifecycle")]
	public UnityEvent onAwake;

	public UnityEvent onStart;

	public UnityEvent onEnableEvent;

	public UnityEvent onDisableEvent;

	public UnityEvent onUpdate;

	private void Awake()
	{
		onAwake?.Invoke();
	}

	private void Start()
	{
		onStart?.Invoke();
	}

	private void OnEnable()
	{
		onEnableEvent?.Invoke();
	}

	private void OnDisable()
	{
		onDisableEvent?.Invoke();
	}

	private void Update()
	{
		onUpdate?.Invoke();
	}
}
