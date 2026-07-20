using UnityEngine;

namespace HP.Generics;

public class ConditionsToPauseGameAssistant : MonoBehaviour
{
	public void CheckSpawnSystem(ConditionsToPauseGame conditions)
	{
		bool isPauseAllowed = !Object.FindObjectOfType<SpawnSystem>().isNewSpawnPosInProgress;
		conditions.isPauseAllowed = isPauseAllowed;
		conditions.isSubProcessDone = true;
	}
}
