using System;
using System.Collections.Generic;

namespace InfimaGames.LowPolyShooterPack;

public class ServiceLocator
{
	private readonly Dictionary<string, IGameService> services = new Dictionary<string, IGameService>();

	public static ServiceLocator Current { get; private set; }

	public static void Initialize()
	{
		Current = new ServiceLocator();
	}

	public T Get<T>() where T : IGameService
	{
		string name = typeof(T).Name;
		if (!services.ContainsKey(name))
		{
			Log.kill(name + " not registered with " + GetType().Name);
			throw new InvalidOperationException();
		}
		return (T)services[name];
	}

	public void Register<T>(T service) where T : IGameService
	{
		string name = typeof(T).Name;
		if (services.ContainsKey(name))
		{
			Log.kill("Attempted to register service of type " + name + " which is already registered with the " + GetType().Name + ".");
		}
		else
		{
			services.Add(name, service);
		}
	}

	public void Unregister<T>() where T : IGameService
	{
		string name = typeof(T).Name;
		if (!services.ContainsKey(name))
		{
			Log.kill("Attempted to unregister service of type " + name + " which is not registered with the " + GetType().Name + ".");
		}
		else
		{
			services.Remove(name);
		}
	}
}
