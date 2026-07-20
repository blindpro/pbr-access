using Photon.Pun;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack;

public class WeaponNetworkSync : MonoBehaviourPun, IPunObservable
{
	[Header("Weapon State")]
	public int weapon = -1;

	public int skin = -1;

	public int grip = -1;

	public int laser = -1;

	public int sight = -1;

	public int muzzle = -1;

	public bool mainPlayerChanged = true;

	private CharacterBehaviour character;

	private InventoryBehaviour inventory;

	private void OnAwake()
	{
		character = GetComponent<CharacterBehaviour>();
		inventory = character.GetInventory();
	}

	private void Update()
	{
		if (character == null)
		{
			character = GetComponent<CharacterBehaviour>();
			inventory = character.GetInventory();
		}
		if (!inventory || !base.photonView.IsMine)
		{
			return;
		}
		int equippedIndex = inventory.GetEquippedIndex();
		WeaponBehaviour equipped = inventory.GetEquipped();
		if (!equipped)
		{
			return;
		}
		WeaponAttachmentManager weaponAttachmentManager = (WeaponAttachmentManager)equipped.GetAttachmentManager();
		if ((bool)weaponAttachmentManager)
		{
			int skinIndex = weaponAttachmentManager.skinIndex;
			int gripIndex = weaponAttachmentManager.gripIndex;
			int laserIndex = weaponAttachmentManager.laserIndex;
			int scopeIndex = weaponAttachmentManager.scopeIndex;
			int muzzleIndex = weaponAttachmentManager.muzzleIndex;
			if (equippedIndex != weapon || skinIndex != skin || gripIndex != grip || laserIndex != laser || scopeIndex != sight || muzzleIndex != muzzle)
			{
				mainPlayerChanged = true;
			}
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (this.character == null)
		{
			this.character = GetComponent<CharacterBehaviour>();
			inventory = this.character.GetInventory();
		}
		if (stream.IsWriting)
		{
			if (!base.photonView.IsMine)
			{
				return;
			}
			stream.SendNext((byte)(mainPlayerChanged ? 1u : 0u));
			if (!mainPlayerChanged)
			{
				return;
			}
			if ((bool)inventory)
			{
				weapon = inventory.GetEquippedIndex();
				WeaponBehaviour equipped = inventory.GetEquipped();
				if ((bool)equipped)
				{
					WeaponAttachmentManager weaponAttachmentManager = (WeaponAttachmentManager)equipped.GetAttachmentManager();
					if ((bool)weaponAttachmentManager)
					{
						skin = weaponAttachmentManager.skinIndex;
						grip = weaponAttachmentManager.gripIndex;
						laser = weaponAttachmentManager.laserIndex;
						sight = weaponAttachmentManager.scopeIndex;
						muzzle = weaponAttachmentManager.muzzleIndex;
					}
				}
			}
			stream.SendNext((byte)weapon);
			stream.SendNext((byte)skin);
			stream.SendNext((short)grip);
			stream.SendNext((short)laser);
			stream.SendNext((short)sight);
			stream.SendNext((byte)muzzle);
			mainPlayerChanged = false;
			Debug.Log("sent weapon sync " + this.character.gameObject.name);
		}
		else
		{
			if (base.photonView.IsMine || (byte)stream.ReceiveNext() != 1)
			{
				return;
			}
			weapon = (byte)stream.ReceiveNext();
			skin = (byte)stream.ReceiveNext();
			grip = (short)stream.ReceiveNext();
			laser = (short)stream.ReceiveNext();
			sight = (short)stream.ReceiveNext();
			muzzle = (byte)stream.ReceiveNext();
			WeaponBehaviour weaponBehaviour = inventory.GetWeapon(weapon);
			if ((bool)weaponBehaviour)
			{
				WeaponAttachmentManager weaponAttachmentManager2 = (WeaponAttachmentManager)weaponBehaviour.GetAttachmentManager();
				if ((bool)weaponAttachmentManager2)
				{
					weaponAttachmentManager2.skinIndex = skin;
					weaponAttachmentManager2.gripIndex = grip;
					weaponAttachmentManager2.laserIndex = laser;
					weaponAttachmentManager2.scopeIndex = sight;
					weaponAttachmentManager2.muzzleIndex = muzzle;
					Character character = (Character)this.character;
					if (inventory.GetEquippedIndex() != weapon)
					{
						character.OnSetInventoryWeapon(weapon);
					}
					else
					{
						weaponAttachmentManager2.UpdateWeapon();
					}
				}
			}
			Debug.Log("received weapon sync " + this.character.name);
		}
	}
}
