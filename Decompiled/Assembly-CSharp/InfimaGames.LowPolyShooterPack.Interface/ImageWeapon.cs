using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack.Interface;

public class ImageWeapon : Element
{
	[Tooltip("Color applied to all images.")]
	[SerializeField]
	private Color imageColor = Color.white;

	[Tooltip("Weapon Body Image.")]
	[SerializeField]
	private Image imageWeaponBody;

	[Tooltip("Weapon Grip Image.")]
	[SerializeField]
	private Image imageWeaponGrip;

	[Tooltip("Weapon Laser Image.")]
	[SerializeField]
	private Image imageWeaponLaser;

	[Tooltip("Weapon Silencer Image.")]
	[SerializeField]
	private Image imageWeaponMuzzle;

	[Tooltip("Weapon Magazine Image.")]
	[SerializeField]
	private Image imageWeaponMagazine;

	[Tooltip("Weapon Scope Image.")]
	[SerializeField]
	private Image imageWeaponScope;

	[Tooltip("Weapon Scope Default Image.")]
	[SerializeField]
	private Image imageWeaponScopeDefault;

	private WeaponAttachmentManagerBehaviour attachmentManagerBehaviour;

	protected override void Tick()
	{
		Color color = imageColor;
		Image[] components = GetComponents<Image>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].color = color;
		}
		if (!(equippedWeaponBehaviour == null))
		{
			attachmentManagerBehaviour = equippedWeaponBehaviour.GetAttachmentManager();
			imageWeaponBody.sprite = equippedWeaponBehaviour.GetSpriteBody();
			Sprite sprite = null;
			ScopeBehaviour equippedScopeDefault = attachmentManagerBehaviour.GetEquippedScopeDefault();
			if (equippedScopeDefault != null)
			{
				sprite = equippedScopeDefault.GetSprite();
			}
			AssignSprite(imageWeaponScopeDefault, sprite, equippedScopeDefault == null);
			ScopeBehaviour equippedScope = attachmentManagerBehaviour.GetEquippedScope();
			if (equippedScope != null)
			{
				sprite = equippedScope.GetSprite();
			}
			AssignSprite(imageWeaponScope, sprite, equippedScope == null || equippedScope == equippedScopeDefault);
			MagazineBehaviour equippedMagazine = attachmentManagerBehaviour.GetEquippedMagazine();
			if (equippedMagazine != null)
			{
				sprite = equippedMagazine.GetSprite();
			}
			AssignSprite(imageWeaponMagazine, sprite, equippedMagazine == null);
			LaserBehaviour equippedLaser = attachmentManagerBehaviour.GetEquippedLaser();
			if (equippedLaser != null)
			{
				sprite = equippedLaser.GetSprite();
			}
			AssignSprite(imageWeaponLaser, sprite, equippedLaser == null);
			GripBehaviour equippedGrip = attachmentManagerBehaviour.GetEquippedGrip();
			if (equippedGrip != null)
			{
				sprite = equippedGrip.GetSprite();
			}
			AssignSprite(imageWeaponGrip, sprite, equippedGrip == null);
			MuzzleBehaviour equippedMuzzle = attachmentManagerBehaviour.GetEquippedMuzzle();
			if (equippedMuzzle != null)
			{
				sprite = equippedMuzzle.GetSprite();
			}
			AssignSprite(imageWeaponMuzzle, sprite, equippedMuzzle == null);
		}
	}

	private static void AssignSprite(Image image, Sprite sprite, bool forceHide = false)
	{
		image.sprite = sprite;
		image.enabled = sprite != null && !forceHide;
	}
}
