using UnityEngine;

namespace HP.Generics;

public class PauseManagerAssistant : MonoBehaviour
{
	public void DisableCharacterMovement()
	{
		TSCharacterTag[] array = Object.FindObjectsOfType<TSCharacterTag>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((bool)array[i].GetComponent<Rigidbody>())
			{
				array[i].GetComponent<Rigidbody>().isKinematic = true;
			}
			if ((bool)array[i].GetComponent<characterMovement>())
			{
				array[i].GetComponent<characterMovement>().isMovementAllowed = false;
			}
		}
	}

	public void EnableCharacterMovement()
	{
		TSCharacterTag[] array = Object.FindObjectsOfType<TSCharacterTag>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((bool)array[i].GetComponent<Rigidbody>())
			{
				array[i].GetComponent<Rigidbody>().isKinematic = false;
			}
			if ((bool)array[i].GetComponent<characterMovement>())
			{
				array[i].GetComponent<characterMovement>().isMovementAllowed = true;
			}
		}
	}

	public void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}
