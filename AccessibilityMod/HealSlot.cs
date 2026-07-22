using InfimaGames.LowPolyShooterPack;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Healing had no keyboard route at all. The game uses a health item in exactly
    /// two places - a HUD button wired to PickupsManager.UseHealthAuto in the scene,
    /// and dragging the item onto the Use zone of the inventory screen. Both are
    /// mouse-only, and neither is bound to an input action, so a player working from
    /// the keyboard simply could not heal. Ammo and grenades were never in this
    /// position: reloading calls UseAmmoAuto, and a picked-up grenade is used at once.
    ///
    /// This makes heals a third weapon slot rather than a one-shot key. 3 draws them,
    /// the same Left Control that fires drinks one, and 1 or 2 puts the gun back. The
    /// slot is the point: the fire key keeps its meaning of "use what I am holding",
    /// so there is one less binding to remember, and being armed is a state the player
    /// chose and can be told about rather than a key they have to find under pressure.
    ///
    /// Left Control cannot both fire and heal in the same frame - AccessibleInputController
    /// stands down whenever this slot is drawn.
    /// </summary>
    public class HealSlot
    {
        /// <summary>
        /// True while heals are drawn. The fire key belongs to this slot for as long
        /// as it is, and the weapon is not fired.
        /// </summary>
        public static bool IsArmed { get; private set; }

        private bool _wasHealing;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead())
            {
                IsArmed = false;
                _wasHealing = false;
                return;
            }

            if (MatchmakingManager.Instance == null
                || MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
            {
                IsArmed = false;
                return;
            }

            AnnounceFinishedHeal(player);

            // The loot list owns the keyboard while it is open.
            if (LootMenu.IsOpen) return;

            if (Pressed(KeyCode.Alpha3, KeyCode.Keypad3))
            {
                Toggle(player);
                return;
            }

            // Selecting a real weapon puts the heals away. The game reads 1 and 2 too,
            // so the gun comes back on its own - this only keeps our state honest.
            if (IsArmed && (Pressed(KeyCode.Alpha1, KeyCode.Keypad1)
                            || Pressed(KeyCode.Alpha2, KeyCode.Keypad2)))
            {
                IsArmed = false;
                ScreenReaderManager.Speak("Weapon");
                return;
            }

            if (IsArmed && Input.GetKeyDown(KeyCode.LeftControl))
                Use(player);
        }

        private void Toggle(CharacterMultiplayer player)
        {
            if (IsArmed)
            {
                IsArmed = false;
                ScreenReaderManager.Speak("Weapon");
                return;
            }

            int count = CountHeals(player);
            if (count == 0)
            {
                // Arming an empty slot would only take the fire key away for nothing.
                ScreenReaderManager.Speak("No heals in bag");
                return;
            }

            IsArmed = true;
            ScreenReaderManager.Speak($"Heals, {count}. Control to use");
        }

        private void Use(CharacterMultiplayer player)
        {
            if (player.isHealing)
            {
                // UseHealthItem refuses a second heal while one is running, and a
                // refusal the player cannot hear reads as a dead key.
                ScreenReaderManager.Speak("Already healing");
                return;
            }

            var pickupsMgr = GetPickupsManager();
            if (pickupsMgr == null) return;

            int before = CountHeals(player);
            if (before == 0)
            {
                Disarm("No heals left");
                return;
            }

            // The game's own path: it walks the bag, uses the first health item,
            // removes it and refreshes the inventory HUD. Reimplementing that here
            // would be a second copy of it to keep in step.
            pickupsMgr.UseHealthAuto();

            int after = CountHeals(player);
            if (after == before)
            {
                ScreenReaderManager.Speak("Could not heal");
                return;
            }

            _wasHealing = true;

            if (after == 0)
            {
                Disarm("Healing. Last heal, weapon back");
                return;
            }

            ScreenReaderManager.Speak($"Healing. {after} left");
        }

        /// <summary>
        /// Healing is a channel of roughly three quarters of a second, not an instant
        /// gain, so the useful moment to report health is when the bar completes.
        /// </summary>
        private void AnnounceFinishedHeal(CharacterMultiplayer player)
        {
            if (player.isHealing)
            {
                _wasHealing = true;
                return;
            }

            if (!_wasHealing) return;
            _wasHealing = false;

            int pct = player.health * 100 / 255;
            ScreenReaderManager.Speak($"Healed. Health {pct} percent");
        }

        private static void Disarm(string message)
        {
            IsArmed = false;
            ScreenReaderManager.Speak(message);
        }

        private static int CountHeals(CharacterMultiplayer player)
        {
            var charInv = player.GetComponent<CharacterInventory>();
            if (charInv == null || charInv.inventory == null) return 0;

            int count = 0;
            foreach (var item in charInv.inventory)
            {
                if (item != null && item.type == PickupsManager.ItemType.health)
                    count++;
            }
            return count;
        }

        private static bool Pressed(KeyCode a, KeyCode b)
        {
            return Input.GetKeyDown(a) || Input.GetKeyDown(b);
        }

        private static PickupsManager GetPickupsManager()
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.GetComponent<PickupsManager>();
        }
    }
}
