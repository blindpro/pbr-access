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

        // Match end / victory. The result screen is almost all static Text, which the
        // menu navigator does not read, so a win, a loss and the next-match countdown
        // were all silent.
        private bool _announcedFinish;
        private bool _announcedNext10;
        private bool _announcedNext5;

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
                    ResetMatchEndState();
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
                ResetMatchEndState();
                ScreenReaderManager.Speak("In game");
            }

            MonitorHealth(player);
            MonitorKills(player);
            MonitorParachute(player);
            MonitorRemaining();
            MonitorZone(player);
            MonitorMatchEnd(player);
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

        /// <summary>
        /// Speaks the outcome the moment the match reaches Finish, then the countdown
        /// to the next match. Winning is decided exactly the way the game decides it in
        /// GameManager.OnMatchFinished: the local player wins if they are still alive
        /// when the match ends. A player who died earlier already heard their rank, but
        /// still needs to know the match is over and when the next one starts.
        /// </summary>
        private void MonitorMatchEnd(CharacterMultiplayer player)
        {
            if (MatchmakingManager.Instance == null) return;
            var status = MatchmakingManager.Instance.GetRoomStatus();

            if (status != MatchmakingManager.RoomStatus.Finish)
            {
                // Back in a live match (or between matches): arm for the next Finish.
                ResetMatchEndState();
                return;
            }

            if (!_announcedFinish)
            {
                _announcedFinish = true;
                AnnounceOutcome(player);
            }

            MonitorNextMatchCountdown();
        }

        private void AnnounceOutcome(CharacterMultiplayer player)
        {
            int seconds = NextMatchSeconds();
            string tail = seconds > 0 ? $". Next match in {seconds} seconds" : "";

            if (!player.IsDead())
            {
                ScreenReaderManager.Speak($"Victory! You win with {player.kills} kills{tail}");
                return;
            }

            string winner = FindWinnerName(player);
            string who = winner == null ? "" : $". {winner} won";
            ScreenReaderManager.Speak(
                $"Match over. You finished rank {player.match_rank}, {player.kills} kills{who}{tail}");
        }

        /// <summary>
        /// Reads the same next-match countdown the sighted player sees on the results
        /// screen and calls it out at ten and five seconds, so the return to the lobby
        /// isn't a surprise.
        /// </summary>
        private void MonitorNextMatchCountdown()
        {
            int seconds = NextMatchSeconds();
            if (seconds <= 0) return;

            if (seconds <= 10 && seconds > 5 && !_announcedNext10)
            {
                _announcedNext10 = true;
                ScreenReaderManager.Speak("Next match in 10 seconds");
            }
            else if (seconds <= 5 && !_announcedNext5)
            {
                _announcedNext5 = true;
                ScreenReaderManager.Speak($"Next match in {seconds} seconds");
            }
        }

        /// <summary>
        /// The live countdown from the results screen (WinnersManager.nextMatchTimeTxt),
        /// or 0 when it isn't showing a number yet.
        /// </summary>
        private static int NextMatchSeconds()
        {
            try
            {
                if (MatchmakingManager.Instance == null) return 0;
                var winners = MatchmakingManager.Instance.GetComponent<WinnersManager>();
                if (winners == null || winners.nextMatchTimeTxt == null) return 0;
                if (int.TryParse(winners.nextMatchTimeTxt.text, out int s)) return s;
            }
            catch (Exception) { }
            return 0;
        }

        /// <summary>
        /// The surviving enemy, for the "X won" flourish on a loss. In a solo match
        /// exactly one player is alive at Finish; in squads this names one of the
        /// winning side. Null if nobody obvious is left to name.
        /// </summary>
        private static string FindWinnerName(CharacterMultiplayer main)
        {
            foreach (var c in CharacterMultiplayer.characters)
            {
                if (c == null || c == main) continue;
                if (c.isSpectating) continue;
                if (c.IsDead()) continue;
                if (!string.IsNullOrEmpty(c.Nickname)) return c.Nickname;
            }
            return null;
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
                var charInv = player.GetComponent<CharacterInventory>();
                var character = player.GetComponent<Character>();
                if (character == null || charInv == null)
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

                // The pistol every player spawns with used to be reported as "No
                // weapon", because the asset key is "Handgun 01" and that was read
                // as the game's placeholder. It is a real, loaded, usable gun, and
                // saying otherwise told a player with nothing else that they were
                // unarmed. short_description is the name it has always had: Glock 19.
                int currentWeaponSlot = charInv.GetCurrentWeapon();
                var invItem = currentWeaponSlot == 0 ? charInv.weapon1 : charInv.weapon2;
                string weaponName = invItem != null
                    ? ItemText.Name(invItem)
                    : (weapon.GetWeaponName() ?? "Unknown");

                int current = weapon.GetAmmunitionCurrent();
                int magSize = weapon.GetAmmunitionTotal();
                int reserveMags = weapon.GetCurrentMags();
                string ammoText = $"{weaponName}, {current} of {magSize}, {reserveMags} magazines";

                // A scope only does anything while aiming, so knowing one is fitted
                // is what makes X worth pressing.
                string scope = ScopeInfo.FittedScopeName(charInv);
                if (scope != null) ammoText += $", {scope}";

                int grenades2 = character.GetGrenadesCurrent();
                ScreenReaderManager.Speak($"{ammoText}. {grenades2} grenades");
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

                // Armour. These slots are null until something is found, and reading
                // .id off them threw every time the player was unarmoured - which is
                // most of a match, and exactly when the rest of this summary matters.
                var charInv = player.GetComponent<CharacterInventory>();
                if (charInv != null)
                {
                    summary += charInv.vest != null
                        ? $", vest level {charInv.vest.level}" : ", no vest";
                    summary += charInv.helmet != null
                        ? $", helmet level {charInv.helmet.level}" : ", no helmet";

                    // What the armour is worth, since the HUD's own durability
                    // numbers are not what the damage math reads.
                    if (ArmorPatches.IsActive)
                    {
                        int pct = Mathf.RoundToInt(ArmorPatches.ReductionFor(player) * 100f);
                        if (pct > 0) summary += $", armour {pct} percent";
                    }
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

        private void ResetMatchEndState()
        {
            _announcedFinish = false;
            _announcedNext10 = false;
            _announcedNext5 = false;
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
