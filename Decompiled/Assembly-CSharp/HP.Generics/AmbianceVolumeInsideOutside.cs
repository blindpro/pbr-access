using System.Collections;
using UnityEngine;

namespace HP.Generics;

public class AmbianceVolumeInsideOutside : MonoBehaviour
{
	public float outsideVolume = 0.137f;

	public float insideVolume = 0.1f;

	public AudioSource aSource;

	public bool isOutside = true;

	private Vector3 lastPos;

	private bool lastCheckHitDetected;

	private void Update()
	{
		Detection();
	}

	private IEnumerator NewVolumeRoutine(float newVolume)
	{
		isOutside = !isOutside;
		float t = 0f;
		float duration = 0.5f;
		float startValue = aSource.volume;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			aSource.volume = Mathf.Lerp(startValue, newVolume, t);
			yield return null;
		}
		yield return null;
	}

	private void Detection()
	{
		if (Physics.Linecast(base.transform.position, lastPos, out var hitInfo))
		{
			if (!lastCheckHitDetected)
			{
				ChangeVolume(hitInfo);
			}
			lastCheckHitDetected = true;
		}
		else
		{
			lastCheckHitDetected = false;
		}
		lastPos = base.transform.position;
	}

	private void ChangeVolume(RaycastHit hit)
	{
		if ((bool)hit.transform.GetComponent<EyeAdaptTriggerTag>())
		{
			if ((bool)aSource && isOutside)
			{
				StopAllCoroutines();
				StartCoroutine(NewVolumeRoutine(insideVolume));
			}
			else if ((bool)aSource && !isOutside)
			{
				StopAllCoroutines();
				StartCoroutine(NewVolumeRoutine(outsideVolume));
			}
		}
	}
}
