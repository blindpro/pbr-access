using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class Statics : MonoBehaviour
{
	public static void SetLayer(GameObject gameObject, int new_layer_int, string new_layer, string current_layer = "", bool includeChildren = true)
	{
		if (gameObject.layer == new_layer_int)
		{
			return;
		}
		Debug.LogWarning("SetLayer " + gameObject?.ToString() + " " + new_layer);
		string text = LayerMask.LayerToName(gameObject.layer);
		if (text == new_layer)
		{
			return;
		}
		if ((text != new_layer && current_layer == "") || (text == current_layer && current_layer != ""))
		{
			gameObject.layer = LayerMask.NameToLayer(new_layer);
		}
		if (!includeChildren)
		{
			return;
		}
		Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			text = LayerMask.LayerToName(transform.gameObject.layer);
			if ((text != new_layer && current_layer == "") || (text == current_layer && current_layer != ""))
			{
				transform.gameObject.layer = LayerMask.NameToLayer(new_layer);
			}
		}
	}

	public static void EnableCamera(Camera camera, bool enable)
	{
		if (camera.enabled != enable)
		{
			camera.enabled = enable;
			if ((bool)camera.GetComponent<AudioListener>())
			{
				camera.GetComponent<AudioListener>().enabled = enable;
			}
			if ((bool)camera.GetComponent<AudioReverbZone>())
			{
				camera.GetComponent<AudioReverbZone>().enabled = false;
			}
			Debug.LogWarning("EnableCamera " + camera.name + " " + enable);
		}
	}

	public static void SetLayerRecursively(GameObject obj, int newLayer)
	{
		if (!(obj == null))
		{
			obj.layer = newLayer;
			for (int i = 0; i < obj.transform.childCount; i++)
			{
				SetLayerRecursively(obj.transform.GetChild(i).gameObject, newLayer);
			}
		}
	}

	public static void SetActive(GameObject obj, bool active)
	{
		if (obj.activeSelf != active)
		{
			obj.SetActive(active);
		}
	}

	public static string ElapsedTimeToTimeFormat(float elapsed)
	{
		int num = Mathf.FloorToInt(elapsed / 60f);
		int num2 = Mathf.FloorToInt(elapsed % 60f);
		return $"{num:00} : {num2:00}";
	}

	public static void DestroyAllChildren(GameObject parent)
	{
		if (!(parent == null))
		{
			for (int num = parent.transform.childCount - 1; num >= 0; num--)
			{
				Object.DestroyImmediate(parent.transform.GetChild(num).gameObject);
			}
		}
	}
}
