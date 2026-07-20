using UnityEngine;

namespace HardShellStudios.CompleteControl;

[CreateAssetMenu(fileName = "KeyBindings", menuName = "Input Asset/Default Config")]
public class hScheme : ScriptableObject
{
	public hInputDetails[] inputs;

	public KeyCode rebindRemoveKey = KeyCode.Escape;

	public bool forceResetInEditor = true;
}
