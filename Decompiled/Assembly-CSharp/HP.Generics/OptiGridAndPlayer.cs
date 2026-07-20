using System.Collections;
using UnityEngine;

namespace HP.Generics;

public class OptiGridAndPlayer : MonoBehaviour, IInitable
{
	public GameObject character;

	public AP_Cam_Follow cam;

	private void Start()
	{
		InstantiateCharacter();
	}

	public void InstantiateCharacter()
	{
		StartCoroutine(InstantiateCharacterRoutine());
	}

	private IEnumerator InstantiateCharacterRoutine()
	{
		GameObject newChara = Object.Instantiate(character, new Vector3(996f, 31f, 830f), Quaternion.identity);
		yield return new WaitUntil(() => newChara.transform.position == new Vector3(996f, 31f, 830f));
		cam.target = newChara.transform.GetChild(4).GetChild(1);
		yield return new WaitForSeconds(2f);
		TSOptiGrid.instance.Init();
		yield return null;
	}

	public bool IsInitDone()
	{
		if (TSOptiGrid.instance.isInitDone)
		{
			return true;
		}
		return false;
	}
}
