using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class DestroyAfterDelay : MonoBehaviour
{
	public float Delay = 5f;

	public bool Deactivate;

	public bool DeactivateRendrers;

	private void Update()
	{
		if (!IsInvoking("onDelete"))
		{
			Invoke("onDelete", Delay);
		}
	}

	private void onDelete()
	{
		if (Deactivate)
		{
			if (DeactivateRendrers)
			{
				Renderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<Renderer>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = false;
				}
			}
			else
			{
				base.gameObject.SetActive(value: false);
			}
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
