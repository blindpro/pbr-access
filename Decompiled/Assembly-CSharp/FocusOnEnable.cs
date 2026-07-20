using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FocusOnEnable : MonoBehaviour
{
	private Button m_Button;

	private void OnEnable()
	{
		StartCoroutine(Focus());
	}

	private IEnumerator Focus()
	{
		yield return new WaitForSecondsRealtime(1f);
		m_Button.Select();
	}

	private void Awake()
	{
		m_Button = GetComponent<Button>();
	}
}
