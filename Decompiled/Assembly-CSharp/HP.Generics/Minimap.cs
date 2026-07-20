using UnityEngine;

namespace HP.Generics;

public class Minimap : MonoBehaviour
{
	public RectTransform minimapZoneRect;

	public RectTransform characterRect;

	public Transform minimapOrigin;

	private TSCharacterTag character;

	public Vector2 mapSize = new Vector2(2100f, 2100f);

	public Transform camPosition;

	private void Start()
	{
		character = Object.FindObjectOfType<TSCharacterTag>();
		if (!camPosition)
		{
			camPosition = GameObject.FindWithTag("MainCamera").transform;
		}
	}

	private void OnDisable()
	{
		characterRect.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if ((bool)character)
		{
			UpdateCharacterDirection();
			UpdateCharacterPosition();
		}
	}

	private void UpdateCharacterPosition()
	{
		float x = character.transform.position.x;
		float x2 = minimapOrigin.position.x;
		float num = minimapOrigin.position.x + mapSize.x;
		float x3 = (x - x2) / (num - x2);
		float z = character.transform.position.z;
		float z2 = minimapOrigin.position.z;
		float num2 = minimapOrigin.position.z + mapSize.y;
		float y = (z - z2) / (num2 - z2);
		characterRect.pivot = new Vector2(x3, y);
		if (!characterRect.gameObject.activeSelf)
		{
			characterRect.gameObject.SetActive(value: true);
		}
	}

	private void UpdateCharacterDirection()
	{
		if ((bool)camPosition)
		{
			characterRect.GetChild(0).localEulerAngles = new Vector3(characterRect.GetChild(0).localEulerAngles.x, characterRect.GetChild(0).localEulerAngles.y, 0f - camPosition.eulerAngles.y);
		}
	}
}
