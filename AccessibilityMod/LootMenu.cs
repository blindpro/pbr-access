using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Resolves the "several items in one pile" problem.
    ///
    /// A loot box holds three to six items and the game only ever offers whichever
    /// one the camera ray happens to cross, which a blind player cannot steer. E
    /// now gathers every item within pick range instead:
    /// - one item, it is simply taken
    /// - more than one, a spoken list opens. Up and Down move through it, Enter
    ///   takes the selected item, and Left Arrow, Escape or E closes it.
    ///
    /// The list rebuilds after each take, so the player can empty a pile without
    /// reopening anything.
    /// </summary>
    public class LootMenu
    {
        private struct Entry
        {
            public AmmoBox Box;
            public PickupsManager.Item Item;
        }

        /// <summary>
        /// True while the list has focus. MenuNavigator honours this so Up, Down
        /// and Enter drive the loot list rather than whatever HUD buttons happen
        /// to be on screen.
        /// </summary>
        public static bool IsOpen { get; private set; }

        // A take announces itself here, so NavigationAssistant must not also
        // announce it. Its own detection cannot see menu-chosen items reliably.
        private static float _suppressPickupSpeechUntil;
        public static bool SuppressesPickupSpeech => Time.time < _suppressPickupSpeechUntil;

        /// <summary>
        /// Set only around our own call into the game's pick. PickupPatches blocks
        /// every other caller, so the game's interact key cannot take an item out
        /// from under the list.
        /// </summary>
        public static bool IsPicking { get; private set; }

        /// <summary>
        /// Escape closes the list and must not also open the pause menu behind it.
        /// The game reads Escape in its own Update, which may run after ours in the
        /// same frame, so the block outlives the close by a moment.
        /// </summary>
        private static float _blockPauseUntil;
        public static bool BlocksPause => IsOpen || Time.time < _blockPauseUntil;

        private readonly List<Entry> _entries = new List<Entry>();
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

            var pickupsMgr = GetPickupsManager();
            if (pickupsMgr == null) return;

            // E must not open a pile behind an open inventory.
            if (!IsOpen && InventoryMenu.IsOpen) return;

            if (IsOpen)
                HandleOpen(player, pickupsMgr);
            else if (Input.GetKeyDown(KeyCode.E))
                Open(player, pickupsMgr);
        }

        private void HandleOpen(CharacterMultiplayer player, PickupsManager pickupsMgr)
        {
            // Left Arrow is the quick way out - it is already the "back" hand
            // position, and it cannot be confused with Up, Down or Enter.
            if (Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.E))
            {
                Close("Loot closed");
                return;
            }

            // Walking away from the pile closes the list rather than leaving a
            // menu open over items that are no longer reachable.
            Rebuild(player, pickupsMgr);
            if (_entries.Count == 0)
            {
                Close("Nothing left");
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Move(-1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Move(1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                Take(pickupsMgr);
        }

        private void Open(CharacterMultiplayer player, PickupsManager pickupsMgr)
        {
            Rebuild(player, pickupsMgr);

            if (_entries.Count == 0)
            {
                ScreenReaderManager.Speak("Nothing in reach");
                return;
            }

            // A single item needs no menu - that would only add a keypress.
            if (_entries.Count == 1)
            {
                _index = 0;
                Take(pickupsMgr);
                return;
            }

            IsOpen = true;
            _index = 0;
            ScreenReaderManager.Speak($"Loot, {_entries.Count} items. {Describe(_index)}");
        }

        private void Close(string message)
        {
            if (!IsOpen) return;
            IsOpen = false;
            _blockPauseUntil = Time.time + 0.25f;
            _entries.Clear();
            _index = 0;
            if (message != null)
                ScreenReaderManager.Speak(message);
        }

        private void Move(int direction)
        {
            _index += direction;
            if (_index < 0) _index = _entries.Count - 1;
            if (_index >= _entries.Count) _index = 0;
            ScreenReaderManager.Speak(Describe(_index));
        }

        private void Take(PickupsManager pickupsMgr)
        {
            if (_index < 0 || _index >= _entries.Count) return;

            var entry = _entries[_index];
            if (entry.Box == null || entry.Item == null) return;

            string label = Label(entry.Item);

            // AmmoBox.Update rewrites currentItem every frame from the camera ray,
            // so the choice has to be planted immediately before the pick.
            entry.Box.currentItem = entry.Item;
            pickupsMgr.pickAmmoBox = entry.Box;
            pickupsMgr.pickItem = entry.Item;

            IsPicking = true;
            try { pickupsMgr.PickCurrentItem(); }
            finally { IsPicking = false; }

            bool taken = entry.Box.items == null || !entry.Box.items.Contains(entry.Item);
            _suppressPickupSpeechUntil = Time.time + 0.6f;

            if (!taken)
            {
                // Refused - a full bag, or a slot the item cannot go into.
                ScreenReaderManager.Speak($"Could not take {label}");
                return;
            }

            ScreenReaderManager.Speak($"Took {label}");

            if (!IsOpen) return;

            // Keep the list open on the item that slid into the taken one's place,
            // so emptying a pile is Enter, Enter, Enter.
            _entries.RemoveAt(_index);
            if (_entries.Count == 0)
            {
                Close(null);
                return;
            }
            if (_index >= _entries.Count) _index = _entries.Count - 1;
        }

        /// <summary>
        /// Every item in every box within pick range, nearest box first. Loot piles
        /// often sit close enough together that treating them as one list is what
        /// the player actually experiences.
        /// </summary>
        private void Rebuild(CharacterMultiplayer player, PickupsManager pickupsMgr)
        {
            var selected = _index >= 0 && _index < _entries.Count ? _entries[_index].Item : null;
            _entries.Clear();

            Vector3 playerPos = player.transform.position;

            var boxes = new List<AmmoBox>();
            foreach (var box in pickupsMgr.ammoBoxes)
            {
                if (box == null || box.items == null || box.items.Count == 0) continue;
                if (Vector3.Distance(playerPos, box.transform.position) >= pickupsMgr.pickRange) continue;
                boxes.Add(box);
            }

            boxes.Sort((a, b) =>
                Vector3.Distance(playerPos, a.transform.position)
                    .CompareTo(Vector3.Distance(playerPos, b.transform.position)));

            foreach (var box in boxes)
            {
                foreach (var item in box.items)
                {
                    if (item == null) continue;
                    _entries.Add(new Entry { Box = box, Item = item });
                }
            }

            // Hold the selection on the same item when the pile changes underneath.
            if (selected != null)
            {
                int idx = _entries.FindIndex(e => e.Item == selected);
                if (idx >= 0) _index = idx;
            }

            if (_index >= _entries.Count) _index = Mathf.Max(0, _entries.Count - 1);
        }

        private string Describe(int index)
        {
            return $"{index + 1} of {_entries.Count}, {Label(_entries[index].Item)}";
        }

        private static string Label(PickupsManager.Item item)
        {
            return ItemText.Describe(item);
        }

        private static PickupsManager GetPickupsManager()
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.GetComponent<PickupsManager>();
        }
    }
}
