using UnityEngine;

public class MoveMe : MonoBehaviour
{
	private bool forwarding;

	private float delta = 0.5f;

	private void Start()
	{
	}

	private void Update()
	{
		if (forwarding)
		{
			if (base.gameObject.transform.position.z > 0f)
			{
				forwarding = false;
			}
		}
		else if (base.gameObject.transform.position.z < -260f)
		{
			forwarding = true;
		}
		base.gameObject.transform.Translate(0f, 0f, forwarding ? delta : (0f - delta));
	}
}
