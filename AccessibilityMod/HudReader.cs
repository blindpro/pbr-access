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

        // Parachute state tracking
        private bool _wasParachuting;
        private bool _wasOnAirplane;
        private bool _parachuteOpened;
        private const float HeightAnnounceInterval = 2f;
        private float _heightAnnounceTimer;

        // Remaining player milestone tracking
        private int _lastRemaining;
        private bool _announced50;
        private bool _announced25;
        private bool _announced10;
        private bool _announced5;
        private bool _announced3;

        // Zone announcements
        private bool _zoneAppearing;
        private bool _zoneShrinkIn;
        private bool _zoneShrinking;

        // Outside zone tracking
        private bool _wasOutsideZone;
        private float _outsideZoneTimer;
        private const float OutsideZoneInterval = 10f;

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
                    ResetParachuteState();
                    ResetMilestones();
                    ResetZoneState();
                }
                return;
            }

            if (!_wasInGame)
            {
                _wasInGame = true;
                _lastHealth = player.health;
                _lastKills = player.kills;
                ResetParachuteState();
                ResetMilestones();
                ResetZoneState();
                ScreenReaderManager.Speak("In game");
            }

            MonitorHealth(player);
            MonitorKills(player);
            MonitorParachute(player);
            MonitorRemaining();
            MonitorZone(player);
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
                int remaining = CountRemaining();
                ScreenReaderManager.Speak($"You died. Finished rank {player.match_rank}, {remaining} remaining");
            }

            _lastHealth = health;
        }

        private void MonitorKills(CharacterMultiplayer player)
        {
            if (player.kills > _lastKills)
            {
                int diff = player.kills - _lastKills;
                int remaining = CountRemaining();
                if (diff == 1)
                    ScreenReaderManager.Speak($"Kill! {remaining} remaining");
                else
                    ScreenReaderManager.Speak($"{diff} kills! {remaining} remaining");
            }
            _lastKills = player.kills;
        }

        private void MonitorParachute(CharacterMultiplayer player)
        {
            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute == null) return;

            bool onAirplane = parachute.isOnAirplane;
            bool isParachuting = parachute.isParachuting;
            bool isOpen = parachute.isParachuteOpen;

            // Entered airplane (match start)
            if (onAirplane && !_wasOnAirplane)
            {
                ScreenReaderManager.Speak("In helicopter. Press space to jump when ready");
            }

            // Jumped from airplane
            if (_wasOnAirplane && !onAirplane && isParachuting)
            {
                ScreenReaderManager.Speak("Jumped! Free falling");
            }

            // Parachute opened
            if (isOpen && !_parachuteOpened && isParachuting)
            {
                ScreenReaderManager.Speak("Parachute open");
            }

            // Landed
            if (_wasParachuting && !isParachuting && !onAirplane)
            {
                ScreenReaderManager.Speak("Landed");
            }

            // Height readouts while parachuting (not on airplane)
            if (isParachuting && !onAirplane)
            {
                _heightAnnounceTimer -= Time.deltaTime;
                if (_heightAnnounceTimer <= 0f)
                {
                    _heightAnnounceTimer = HeightAnnounceInterval;
                    float height = player.transform.position.y;
                    int rounded = Mathf.RoundToInt(height);
                    ScreenReaderManager.Speak($"{rounded} meters");
                }
            }

            _wasOnAirplane = onAirplane;
            _wasParachuting = isParachuting;
            _parachuteOpened = isOpen;
        }

        private void MonitorRemaining()
        {
            if (MatchmakingManager.Instance == null) return;
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing) return;

            int remaining = CountRemaining();

            if (remaining == _lastRemaining) return;

            // Announce milestones as player count drops through them
            if (remaining <= 50 && _lastRemaining > 50 && !_announced50)
            {
                _announced50 = true;
                ScreenReaderManager.Speak($"Top 50! {remaining} players remaining");
            }
            if (remaining <= 25 && !_announced25)
            {
                _announced25 = true;
                ScreenReaderManager.Speak($"Top 25! {remaining} players remaining");
            }
            if (remaining <= 10 && !_announced10)
            {
                _announced10 = true;
                ScreenReaderManager.Speak($"Top 10! {remaining} players remaining");
            }
            if (remaining <= 5 && !_announced5)
            {
                _announced5 = true;
                ScreenReaderManager.Speak($"Top 5! {remaining} players remaining");
            }
            if (remaining <= 3 && !_announced3)
            {
                _announced3 = true;
                ScreenReaderManager.Speak($"Final 3! {remaining} players remaining");
            }

            _lastRemaining = remaining;
        }

        private void MonitorZone(CharacterMultiplayer player)
        {
            if (GameManager.Instance == null) return;
            var zoneMgr = GameManager.Instance.GetComponent<DamageZoneManager>();
            if (zoneMgr == null) return;

            // Zone appearing
            bool appearing = zoneMgr.safeZoneAppearsMsg.alpha > 0.5f;
            if (appearing && !_zoneAppearing)
            {
                ScreenReaderManager.Speak("Safe zone appearing soon");
            }
            _zoneAppearing = appearing;

            // Zone shrink warning
            bool shrinkIn = zoneMgr.safeZoneShrinkInMsg.alpha > 0.5f;
            if (shrinkIn && !_zoneShrinkIn)
            {
                ScreenReaderManager.Speak("Zone shrinking soon. Get to the safe zone");
            }
            _zoneShrinkIn = shrinkIn;

            // Zone actively shrinking
            bool shrinking = zoneMgr.IsShrinking();
            if (shrinking && !_zoneShrinking)
            {
                ScreenReaderManager.Speak("Zone shrinking now!");
            }
            _zoneShrinking = shrinking;

            // Outside zone warning
            if (MatchmakingManager.Instance.GetRoomStatus() == MatchmakingManager.RoomStatus.Playing)
            {
                bool outsideZone = !player.IsInsideSafeZone() && !player.IsDead()
                    && (shrinkIn || shrinking || _zoneShrinkIn);

                if (outsideZone)
                {
                    _outsideZoneTimer -= Time.deltaTime;
                    if (!_wasOutsideZone || _outsideZoneTimer <= 0f)
                    {
                        _outsideZoneTimer = OutsideZoneInterval;
                        ScreenReaderManager.Speak("Outside the safe zone! Taking damage");
                    }
                }
                _wasOutsideZone = outsideZone;
            }
        }

        private void HandleKeybinds(CharacterMultiplayer player)
        {
            // H - Read health
            if (Input.GetKeyDown(KeyCode.H))
            {
                int pct = player.health * 100 / 255;
                ScreenReaderManager.Speak($"Health {pct} percent");
            }

            // Z - Read ammo (changed from J)
            if (Input.GetKeyDown(KeyCode.Z))
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

            // J - Read current height/altitude
            if (Input.GetKeyDown(KeyCode.J))
            {
                float height = player.transform.position.y;
                int rounded = Mathf.RoundToInt(height);
                ScreenReaderManager.Speak($"Height {rounded} meters");
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
                string weaponName = weapon.GetWeaponName() ?? "Unknown";
                string ammoText = $"{weaponName}, {current} of {magSize}, {reserveMags} magazines";

                int grenades = character.GetGrenadesCurrent();
                ScreenReaderManager.Speak($"{ammoText}. {grenades} grenades");
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
                int remaining = CountRemaining();
                string status = $"{player.kills} kills, {remaining} players remaining";

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

                int remaining = CountRemaining();
                summary += $", {remaining} alive";

                // Zone status
                if (GameManager.Instance != null)
                {
                    var zoneMgr = GameManager.Instance.GetComponent<DamageZoneManager>();
                    if (zoneMgr != null)
                    {
                        if (zoneMgr.IsShrinking())
                            summary += ", zone shrinking";
                        if (!player.IsInsideSafeZone() && !player.IsDead())
                            summary += ", outside zone";
                    }
                }

                // Healing status
                if (player.isHealing)
                    summary += ", healing";

                // Parachute status
                var parachute = player.GetComponent<CharacterParachute>();
                if (parachute != null && parachute.isParachuting && !parachute.isOnAirplane)
                {
                    float height = player.transform.position.y;
                    summary += $", parachuting at {Mathf.RoundToInt(height)} meters";
                }

                ScreenReaderManager.Speak(summary);
            }
            catch (Exception)
            {
                int healthPct = player.health * 100 / 255;
                ScreenReaderManager.Speak($"Health {healthPct} percent, {player.kills} kills");
            }
        }

        private static int CountRemaining()
        {
            int count = 0;
            foreach (var c in CharacterMultiplayer.characters)
            {
                if (c != null && !c.IsDead())
                    count++;
            }
            return count;
        }

        private void ResetParachuteState()
        {
            _wasParachuting = false;
            _wasOnAirplane = false;
            _parachuteOpened = false;
            _heightAnnounceTimer = 0f;
        }

        private void ResetMilestones()
        {
            _lastRemaining = 0;
            _announced50 = false;
            _announced25 = false;
            _announced10 = false;
            _announced5 = false;
            _announced3 = false;
        }

        private void ResetZoneState()
        {
            _zoneAppearing = false;
            _zoneShrinkIn = false;
            _zoneShrinking = false;
            _wasOutsideZone = false;
            _outsideZoneTimer = 0f;
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
