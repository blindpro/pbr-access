using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// A keyboard inventory, because the game's own one cannot be reached at all.
    ///
    /// The vanilla screen is drag and drop over bare Images carrying UIDragHandler:
    /// there is not a single Selectable anywhere in it, so MenuNavigator - which
    /// walks Selectables - never sees a row of it, and every operation is a mouse
    /// gesture from a source rectangle to a target rectangle. Equipping a scope
    /// means dragging an icon onto the right one of ten small frames.
    ///
    /// So this does not try to drive that UI. It reads CharacterInventory, which is
    /// where the truth lives, and performs the same state changes PickupsManager's
    /// OnEndDrag performs for each drop zone, then calls Apply and refreshes the
    /// game's HUD so the visible screen agrees with what was just said.
    ///
    /// I opens and closes it. Up and Down move, Enter is the obvious thing to do
    /// with the row - fit it, use it, hold it - and Delete drops. Every row is
    /// spoken with its slot, so an empty scope mount announces itself as one; the
    /// player learns the shape of their kit by walking it, rather than having to
    /// remember which frame is which.
    /// </summary>
    public class InventoryMenu
    {
        private enum Kind
        {
            Weapon,
            Silencer,
            Scope,
            Grip,
            Laser,
            Vest,
            Helmet,
            Bag,
            BagItem,
        }

        private struct Row
        {
            public Kind Kind;
            public bool IsWeapon1;
            public PickupsManager.Item Item;
        }

        public static bool IsOpen { get; private set; }

        /// <summary>
        /// The game binds its own inventory screen to a key we cannot see from here,
        /// and if that key is ours the mouse-driven screen would open behind this
        /// one and unlock the cursor. The block outlives the toggle by a moment
        /// because the input system callback may run after our Update in the frame.
        /// </summary>
        private static float _blockGameUntil;
        public static bool BlocksGameInventory => IsOpen || Time.time < _blockGameUntil;

        private readonly List<Row> _rows = new List<Row>();
        private int _index;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead()) { Close(null); return; }

            if (MatchmakingManager.Instance == null
                || MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
            {
                Close(null);
                return;
            }

            // The loot list came first and keeps the keyboard while it is up.
            if (LootMenu.IsOpen) return;

            var charInv = player.GetComponent<CharacterInventory>();
            if (charInv == null) return;

            if (IsOpen)
                HandleOpen(player, charInv);
            else if (Input.GetKeyDown(KeyCode.I))
                Open(charInv);
        }

        private void Open(CharacterInventory charInv)
        {
            IsOpen = true;
            _blockGameUntil = Time.time + 0.25f;
            _index = 0;
            Rebuild(charInv);

            int used = charInv.inventory != null ? charInv.inventory.Count : 0;
            int capacity = charInv.bag != null ? charInv.bag.capacity : 0;
            ScreenReaderManager.Speak($"Inventory, bag {used} of {capacity}. {Describe(charInv, _index)}");
        }

        private void HandleOpen(CharacterMultiplayer player, CharacterInventory charInv)
        {
            if (Input.GetKeyDown(KeyCode.I)
                || Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.Escape))
            {
                Close("Inventory closed");
                return;
            }

            Rebuild(charInv);
            if (_rows.Count == 0) { Close("Inventory empty"); return; }

            if (Input.GetKeyDown(KeyCode.UpArrow)) { Move(charInv, -1); return; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { Move(charInv, 1); return; }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Activate(player, charInv);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                Drop(charInv);
        }

        private void Close(string message)
        {
            if (!IsOpen) return;
            IsOpen = false;
            _blockGameUntil = Time.time + 0.25f;
            _rows.Clear();
            _index = 0;
            if (message != null) ScreenReaderManager.Speak(message);
        }

        private void Move(CharacterInventory charInv, int direction)
        {
            _index += direction;
            if (_index < 0) _index = _rows.Count - 1;
            if (_index >= _rows.Count) _index = 0;
            ScreenReaderManager.Speak(Describe(charInv, _index));
        }

        /// <summary>
        /// Equipment slots first, in the order they sit on the HUD, then the bag.
        /// Attachment mounts are listed even when nothing is in them: an empty scope
        /// mount is information - it is somewhere a scope can go.
        /// </summary>
        private void Rebuild(CharacterInventory charInv)
        {
            _rows.Clear();

            AddWeapon(charInv, isWeapon1: true);
            if (charInv.weapon2 != null) AddWeapon(charInv, isWeapon1: false);

            _rows.Add(new Row { Kind = Kind.Vest, Item = charInv.vest });
            _rows.Add(new Row { Kind = Kind.Helmet, Item = charInv.helmet });
            _rows.Add(new Row { Kind = Kind.Bag, Item = charInv.bag });

            if (charInv.inventory != null)
            {
                foreach (var item in charInv.inventory)
                {
                    if (item == null) continue;
                    _rows.Add(new Row { Kind = Kind.BagItem, Item = item });
                }
            }

            if (_index >= _rows.Count) _index = Mathf.Max(0, _rows.Count - 1);
        }

        private void AddWeapon(CharacterInventory charInv, bool isWeapon1)
        {
            _rows.Add(new Row
            {
                Kind = Kind.Weapon,
                IsWeapon1 = isWeapon1,
                Item = isWeapon1 ? charInv.weapon1 : charInv.weapon2,
            });
            _rows.Add(new Row { Kind = Kind.Silencer, IsWeapon1 = isWeapon1, Item = Silencer(charInv, isWeapon1) });
            _rows.Add(new Row { Kind = Kind.Scope, IsWeapon1 = isWeapon1, Item = Scope(charInv, isWeapon1) });
            _rows.Add(new Row { Kind = Kind.Grip, IsWeapon1 = isWeapon1, Item = Grip(charInv, isWeapon1) });
            _rows.Add(new Row { Kind = Kind.Laser, IsWeapon1 = isWeapon1, Item = Laser(charInv, isWeapon1) });
        }

        private string Describe(CharacterInventory charInv, int index)
        {
            if (index < 0 || index >= _rows.Count) return "";
            Row row = _rows[index];
            string position = $", {index + 1} of {_rows.Count}";

            switch (row.Kind)
            {
                case Kind.Weapon:
                {
                    string slot = row.IsWeapon1 ? "Weapon 1" : "Weapon 2";
                    bool held = charInv.GetCurrentWeapon() == (row.IsWeapon1 ? 0 : 1);
                    return $"{slot}, {ItemText.Describe(row.Item)}{(held ? ", held" : "")}{position}";
                }

                case Kind.Vest:
                case Kind.Helmet:
                {
                    string slot = row.Kind == Kind.Vest ? "Vest" : "Helmet";
                    if (row.Item == null) return $"{slot}, none{position}";
                    return $"{slot}, {ItemText.Describe(row.Item)}, {row.Item.value} of {row.Item.capacity}{position}";
                }

                case Kind.Bag:
                {
                    int used = charInv.inventory != null ? charInv.inventory.Count : 0;
                    int capacity = row.Item != null ? row.Item.capacity : 0;
                    return $"Bag, {ItemText.Describe(row.Item)}, {used} of {capacity}{position}";
                }

                case Kind.BagItem:
                    return $"{ItemText.Describe(row.Item)}{position}";

                default:
                {
                    string weapon = row.IsWeapon1 ? "Weapon 1" : "Weapon 2";
                    string slot = row.Kind.ToString().ToLower();
                    if (row.Item == null) return $"{weapon} {slot}, empty{position}";
                    return $"{weapon} {slot}, {ItemText.Describe(row.Item)}{position}";
                }
            }
        }

        /// <summary>
        /// Enter is whatever the row obviously wants: hold a weapon, use or fit a bag
        /// item, take an attachment back off its mount.
        /// </summary>
        private void Activate(CharacterMultiplayer player, CharacterInventory charInv)
        {
            if (_index < 0 || _index >= _rows.Count) return;
            Row row = _rows[_index];

            switch (row.Kind)
            {
                case Kind.Weapon:
                    if (!row.IsWeapon1 && charInv.weapon2 == null)
                    {
                        ScreenReaderManager.Speak("No second weapon");
                        return;
                    }
                    charInv.SetCurrentWeapon(row.IsWeapon1 ? 0 : 1);
                    Refresh(charInv, $"Holding {ItemText.Name(row.Item)}");
                    return;

                case Kind.BagItem:
                    UseBagItem(player, charInv, row.Item);
                    return;

                case Kind.Vest:
                case Kind.Helmet:
                case Kind.Bag:
                    // Worn the moment they are picked up, and there is nowhere for
                    // them to go but off - which is Delete.
                    ScreenReaderManager.Speak(row.Item == null ? "Nothing worn" : "Worn. Delete to drop");
                    return;

                default:
                    UnfitAttachment(charInv, row);
                    return;
            }
        }

        private void UseBagItem(CharacterMultiplayer player, CharacterInventory charInv,
            PickupsManager.Item item)
        {
            if (item == null) return;
            string name = ItemText.Name(item);

            switch (item.type)
            {
                case PickupsManager.ItemType.health:
                    if (player.isHealing) { ScreenReaderManager.Speak("Already healing"); return; }
                    if (!charInv.UseHealthItem(item)) { ScreenReaderManager.Speak($"Could not use {name}"); return; }
                    charInv.RemoveInventoryItem(item);
                    Refresh(charInv, $"Healing with {name}");
                    return;

                case PickupsManager.ItemType.grenade:
                    if (!charInv.UseGrenadeItem(item)) { ScreenReaderManager.Speak($"Could not use {name}"); return; }
                    charInv.RemoveInventoryItem(item);
                    Refresh(charInv, $"Took {name}");
                    return;

                case PickupsManager.ItemType.silencer_attachment:
                case PickupsManager.ItemType.scope_attachment:
                case PickupsManager.ItemType.grip_attachment:
                case PickupsManager.ItemType.laser_attachment:
                    FitAttachment(charInv, item);
                    return;

                default:
                    if (item.type.ToString().Contains("ammo"))
                    {
                        // The game refuses ammo of the wrong calibre for the held
                        // weapon, which is worth hearing as a reason rather than
                        // as nothing happening.
                        bool isWeapon1 = charInv.GetCurrentWeapon() == 0;
                        if (!charInv.UseAmmoItem(item, isWeapon1))
                        {
                            ScreenReaderManager.Speak($"{name} does not fit this weapon");
                            return;
                        }
                        charInv.RemoveInventoryItem(item);
                        Refresh(charInv, $"Loaded {name}");
                        return;
                    }

                    ScreenReaderManager.Speak($"{name} cannot be used. Delete to drop");
                    return;
            }
        }

        /// <summary>
        /// Onto the weapon being held, mirroring the game's own rule that a pickup
        /// goes to whichever gun is out. Anything already on that mount comes back
        /// to the bag - unless the bag is full, in which case nothing moves at all
        /// rather than an attachment quietly ceasing to exist.
        /// </summary>
        private void FitAttachment(CharacterInventory charInv, PickupsManager.Item item)
        {
            bool isWeapon1 = charInv.GetCurrentWeapon() == 0;
            string name = ItemText.Name(item);
            PickupsManager.Item existing = AttachmentOn(charInv, item.type, isWeapon1);

            charInv.RemoveInventoryItem(item);
            if (existing != null && !charInv.AddInventoryItem(existing))
            {
                charInv.AddInventoryItem(item);
                ScreenReaderManager.Speak("Bag full, cannot swap");
                return;
            }

            SetAttachment(charInv, item.type, isWeapon1, item);
            string slot = SlotName(item.type);
            Refresh(charInv, existing == null
                ? $"Fitted {name} to {slot}"
                : $"Fitted {name}, {ItemText.Name(existing)} to bag");
        }

        private void UnfitAttachment(CharacterInventory charInv, Row row)
        {
            if (row.Item == null) { ScreenReaderManager.Speak("Nothing fitted"); return; }

            if (!charInv.AddInventoryItem(row.Item))
            {
                // AddInventoryItem already shows the bag-full banner on the HUD.
                ScreenReaderManager.Speak("Bag full");
                return;
            }

            SetAttachment(charInv, row.Item.type, row.IsWeapon1, null);
            Refresh(charInv, $"Removed {ItemText.Name(row.Item)} to bag");
        }

        private void Drop(CharacterInventory charInv)
        {
            if (_index < 0 || _index >= _rows.Count) return;
            Row row = _rows[_index];

            if (row.Item == null) { ScreenReaderManager.Speak("Nothing to drop"); return; }
            string name = ItemText.Name(row.Item);

            switch (row.Kind)
            {
                case Kind.Weapon:
                    // Apply reads weapon1 unconditionally, so slot 1 is only ever
                    // emptied by promoting slot 2 into it, exactly as the game does.
                    if (row.IsWeapon1)
                    {
                        if (charInv.weapon2 == null)
                        {
                            ScreenReaderManager.Speak("Cannot drop your only weapon");
                            return;
                        }
                        charInv.weapon1 = charInv.weapon2;
                        charInv.weapon2 = null;
                        charInv.SetCurrentWeapon(0);
                    }
                    else
                    {
                        charInv.weapon2 = null;
                    }
                    Refresh(charInv, $"Dropped {name}");
                    return;

                case Kind.Vest:
                    charInv.vest = null;
                    Refresh(charInv, $"Dropped {name}");
                    return;

                case Kind.Helmet:
                    charInv.helmet = null;
                    Refresh(charInv, $"Dropped {name}");
                    return;

                case Kind.Bag:
                    // Dropping the bag would strand every item in it; the game gives
                    // no way to do it either.
                    ScreenReaderManager.Speak("The bag cannot be dropped");
                    return;

                case Kind.BagItem:
                    charInv.RemoveInventoryItem(row.Item);
                    Refresh(charInv, $"Dropped {name}");
                    return;

                default:
                    SetAttachment(charInv, row.Item.type, row.IsWeapon1, null);
                    Refresh(charInv, $"Dropped {name}");
                    return;
            }
        }

        /// <summary>
        /// Push the change onto the character and the game's own HUD, then re-read
        /// the row the cursor is on so the player hears the result in place.
        /// </summary>
        private void Refresh(CharacterInventory charInv, string message)
        {
            charInv.Apply();

            var pickupsMgr = GameManager.Instance != null
                ? GameManager.Instance.GetComponent<PickupsManager>() : null;
            if (pickupsMgr != null) pickupsMgr.Init();

            Rebuild(charInv);
            ScreenReaderManager.Speak(message);
        }

        private static PickupsManager.Item AttachmentOn(CharacterInventory charInv,
            PickupsManager.ItemType type, bool isWeapon1)
        {
            switch (type)
            {
                case PickupsManager.ItemType.silencer_attachment: return Silencer(charInv, isWeapon1);
                case PickupsManager.ItemType.scope_attachment: return Scope(charInv, isWeapon1);
                case PickupsManager.ItemType.grip_attachment: return Grip(charInv, isWeapon1);
                case PickupsManager.ItemType.laser_attachment: return Laser(charInv, isWeapon1);
                default: return null;
            }
        }

        private static void SetAttachment(CharacterInventory charInv, PickupsManager.ItemType type,
            bool isWeapon1, PickupsManager.Item value)
        {
            switch (type)
            {
                case PickupsManager.ItemType.silencer_attachment:
                    if (isWeapon1) charInv.weapon1_silencer = value; else charInv.weapon2_silencer = value;
                    return;
                case PickupsManager.ItemType.scope_attachment:
                    if (isWeapon1) charInv.weapon1_scope = value; else charInv.weapon2_scope = value;
                    return;
                case PickupsManager.ItemType.grip_attachment:
                    if (isWeapon1) charInv.weapon1_grip = value; else charInv.weapon2_grip = value;
                    return;
                case PickupsManager.ItemType.laser_attachment:
                    if (isWeapon1) charInv.weapon1_laser = value; else charInv.weapon2_laser = value;
                    return;
            }
        }

        private static string SlotName(PickupsManager.ItemType type)
        {
            switch (type)
            {
                case PickupsManager.ItemType.silencer_attachment: return "silencer";
                case PickupsManager.ItemType.scope_attachment: return "scope";
                case PickupsManager.ItemType.grip_attachment: return "grip";
                case PickupsManager.ItemType.laser_attachment: return "laser";
                default: return "slot";
            }
        }

        private static PickupsManager.Item Silencer(CharacterInventory c, bool w1)
            => w1 ? c.weapon1_silencer : c.weapon2_silencer;

        private static PickupsManager.Item Scope(CharacterInventory c, bool w1)
            => w1 ? c.weapon1_scope : c.weapon2_scope;

        private static PickupsManager.Item Grip(CharacterInventory c, bool w1)
            => w1 ? c.weapon1_grip : c.weapon2_grip;

        private static PickupsManager.Item Laser(CharacterInventory c, bool w1)
            => w1 ? c.weapon1_laser : c.weapon2_laser;
    }
}
