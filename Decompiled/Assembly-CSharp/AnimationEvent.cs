using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
	public GameObject SendEventTo;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnAnimationEvent(string Event)
	{
		if ((bool)SendEventTo)
		{
			SendEventTo.SendMessage(Event, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void OnPlaySoundEvent(Object clip)
	{
		if ((bool)SendEventTo)
		{
			SendEventTo.SendMessage("OnPlaySoundEvent", clip, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void HideParachute()
	{
		if ((bool)SendEventTo)
		{
			SendEventTo.SendMessage("HideParachute", SendMessageOptions.DontRequireReceiver);
		}
	}
}
