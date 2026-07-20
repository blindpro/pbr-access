using Steamworks.Data;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public static class SteamImageExtensions
{
	public static Texture2D Convert(this Image image)
	{
		Texture2D texture2D = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, mipChain: false);
		texture2D.filterMode = FilterMode.Trilinear;
		for (int i = 0; i < image.Width; i++)
		{
			for (int j = 0; j < image.Height; j++)
			{
				Steamworks.Data.Color pixel = image.GetPixel(i, j);
				texture2D.SetPixel(i, (int)image.Height - j, new UnityEngine.Color((float)(int)pixel.r / 255f, (float)(int)pixel.g / 255f, (float)(int)pixel.b / 255f, (float)(int)pixel.a / 255f));
			}
		}
		texture2D.Apply();
		return texture2D;
	}
}
