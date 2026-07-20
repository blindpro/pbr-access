using System;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    public class HudReader
    {
        private byte _lastHealth = byte.MaxValue;
        private int _lastKills;
        private bool _wasInGame;

        // Health thresholds for automatic announcements
        private const byte LowHealthThreshold = 64;   // ~25%
        private const byte CriticalHealthThreshold = 25; // ~10%
        private bool _announcedLow;
        private bool _announcedCritical;

        public void Tick()
        {
            var player = GetMainPlayer();
            if (player == null)
            {
                if (_wasInGame)
                {
                    _wasInGame = false;
                    _lastHealth = byte.MaxValue;
                    _lastKills = 0;
                    _announcedLow = false;
                    _announcedCritical = false;
                }
                return;
            }

            if (!_wasInGame)
            {
                _wasInGame = true;
                _lastHealth = player.health;
                _lastKills = player.kills;
                ScreenReaderManager.Speak("In game");
            }

            MonitorHealth(player);
            MonitorKills(player);
            HandleKeybinds(player);
        }

        private void MonitorHealth(CharacterMultiplayer player)
        {
            byte health = player.health;

            // Announce damage taken
            if (health < _lastHealth && _lastHealth > 0)
            {
                int pct = health * 100 / 255;

                if (health <= CriticalHealthThreshold && !_announcedCritical)
                {
                    _announcedCritical = true;
                    ScreenReaderManager.Speak($"Critical! Health {pct} percent");
                }
                else if (health <= LowHealthThreshold && !_announcedLow)
                {
                    _announcedLow = true;
                    ScreenReaderManager.Speak($"Low health! {pct} percent");
                }
            }

            // Reset thresholds when healed
            if (health > LowHealthThreshold)
            {
                _announcedLow = false;
                _announcedCritical = false;
            }
            else if (health > CriticalHealthThreshold)
            {
                _announcedCritical = false;
            }

            if (health <= 0 && _lastHealth > 0)
            {
                ScreenReaderManager.Speak("You died");
            }

            _lastHealth = health;
        }

        private void MonitorKills(CharacterMultiplayer player)
        {
            if (player.kills > _lastKills)
            {
                int diff = player.kills - _lastKills;
                if (diff == 1)
                    ScreenReaderManager.Speak("Kill!");
                else
                    ScreenReaderManager.Speak($"{diff} kills!");
            }
            _lastKills = player.kills;
        }

        private void HandleKeybinds(CharacterMultiplayer player)
        {
            // H - Read health
            if (Input.GetKeyDown(KeyCode.H))
            {
                int pct = player.health * 100 / 255;
                ScreenReaderManager.Speak($"Health {pct} percent");
            }

            // J - Read ammo
            if (Input.GetKeyDown(KeyCode.J))
            {
                ReadAmmo(player);
            }

            // K - Read kills and remaining players
            if (Input.GetKeyDown(KeyCode.K))
            {
                ReadGameStatus(player);
            }

            // L - Read full status summary
            if (Input.GetKeyDown(KeyCode.L))
            {
                ReadFullStatus(player);
            }
        }

        private void ReadAmmo(CharacterMultiplayer player)
        {
            try
            {
                var character = player.GetComponent<Character>();
                if (character == null)
                {
                    ScreenReaderManager.Speak("No weapon info");
                    return;
                }

                var weapon = character.GetEquippedWeapon();
                if (weapon == null)
                {
                    ScreenReaderManager.Speak("No weapon equipped");
                    return;
                }

                int current = weapon.GetAmmunitionCurrent();
                int magSize = weapon.GetAmmunitionTotal();
                int reserveMags = weapon.GetCurrentMags();
                string ammoText = $"{current} of {magSize}, {reserveMags} magazines";

                int grenades = character.GetGrenadesCurrent();
                ScreenReaderManager.Speak($"Ammo {ammoText}. {grenades} grenades");
            }
            catch (Exception)
            {
                ScreenReaderManager.Speak("Ammo info unavailable");
            }
        }

        private void ReadGameStatus(CharacterMultiplayer player)
        {
            try
            {
                string status = $"{player.kills} kills";

                // Count remaining players
                int remaining = 0;
                foreach (var c in CharacterMultiplayer.characters)
                {
                    if (c != null && !c.IsDead())
                        remaining++;
                }
                status += $", {remaining} players remaining";

                if (player.match_rank > 0)
                    status += $", rank {player.match_rank}";

                ScreenReaderManager.Speak(status);
            }
            catch (Exception)
            {
                ScreenReaderManager.Speak($"{player.kills} kills");
            }
        }

        private void ReadFullStatus(CharacterMultiplayer player)
        {
            try
            {
                int healthPct = player.health * 100 / 255;
                string summary = $"Health {healthPct} percent";

                // Armor check
                var charInv = player.GetComponent<CharacterInventory>();
                if (charInv != null)
                {
                    if (charInv.vest.id > 0)
                        summary += ", vest equipped";
                    if (charInv.helmet.id > 0)
                        summary += ", helmet equipped";
                }

                summary += $", {player.kills} kills";

                // Remaining players
                int remaining = 0;
                foreach (var c in CharacterMultiplayer.characters)
                {
                    if (c != null && !c.IsDead())
                        remaining++;
                }
                summary += $", {remaining} alive";

                // Healing status
                if (player.isHealing)
                    summary += ", healing";

                ScreenReaderManager.Speak(summary);
            }
            catch (Exception)
            {
                int healthPct = player.health * 100 / 255;
                ScreenReaderManager.Speak($"Health {healthPct} percent, {player.kills} kills");
            }
        }

        private static CharacterMultiplayer GetMainPlayer()
        {
            try
            {
                return CharacterMultiplayer.GetMainPlayer();
            }
            catch
            {
                return null;
            }
        }
    }
}
