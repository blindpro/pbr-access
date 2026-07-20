using System.Collections;
using UnityEngine;

namespace HP.Generics;

public class Character : MonoBehaviour
{
	public static Character instance;

	private characterMovement charaMovement;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		StartCoroutine(changeLockStateLock());
		if ((bool)base.gameObject.GetComponent<characterMovement>())
		{
			charaMovement = base.gameObject.GetComponent<characterMovement>();
		}
	}

	private void FixedUpdate()
	{
		charaMovement.charaGeneralMovementController();
	}

	public IEnumerator changeLockStateLock()
	{
		yield return new WaitForEndOfFrame();
		Cursor.lockState = CursorLockMode.None;
		yield return new WaitForEndOfFrame();
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		yield return null;
	}
}
