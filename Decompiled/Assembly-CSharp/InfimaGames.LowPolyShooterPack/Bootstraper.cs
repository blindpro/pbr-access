using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public static class Bootstraper
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Initialize()
	{
		ServiceLocator.Initialize();
		ServiceLocator.Current.Register((IGameModeService)new GameModeService());
		GameObject gameObject = new GameObject("Sound Manager");
		AudioManagerService service = gameObject.AddComponent<AudioManagerService>();
		Object.DontDestroyOnLoad(gameObject);
		ServiceLocator.Current.Register((IAudioManagerService)service);
	}
}
