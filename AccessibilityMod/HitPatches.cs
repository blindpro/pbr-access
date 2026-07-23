using HarmonyLib;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Spoken hit markers. The game applies damage through the networked
    /// CharacterMultiplayer.Damage RPC (which also runs on the shooter's own
    /// client), so patching it lets us report every hit the local player lands:
    /// who was tagged, how hard, and how much health they have left.
    ///
    /// The killing blow does NOT go through Damage - RPC_Damage routes lethal
    /// damage to RPC_Dead instead - so OnDead is patched separately to announce
    /// the elimination.
    /// </summary>
    [HarmonyPatch]
    public static class HitPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterMultiplayer), "Damage")]
        static void Damage_Postfix(CharacterMultiplayer __instance, byte damage, byte shooterActorId)
        {
            // Mirror the guard inside Damage - if it early-returned, health never changed.
            if (MatchmakingManager.Instance == null) return;
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing) return;
            if (__instance == null || __instance.IsDead()) return;

            var main = CharacterMultiplayer.GetMainPlayer();
            if (main == null) return;

            // Taking fire ourselves: the game shows a sighted player a directional
            // damage cursor here (Damage calls HitCursorsManager.ShowDamageCursor for
            // the local main player). Speak the direction instead. Actor 0 is the
            // damage zone, not an attacker - it has its own outside-zone warning.
            if (__instance == main)
            {
                if (shooterActorId == 0) return;
                if ((byte)main.ActorNumber == shooterActorId) return; // self / fall damage
                IncomingFireAnnouncer.Report(main, shooterActorId);
                return;
            }

            // Only our own shots land hit markers, and never our own body.
            if ((byte)main.ActorNumber != shooterActorId) return;

            HitAnnouncer.ReportHit(__instance, damage, __instance.health);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterMultiplayer), "OnDead")]
        static void OnDead_Postfix(CharacterMultiplayer __instance, byte shooterActorId)
        {
            if (__instance == null || !__instance.IsDead()) return;

            var main = CharacterMultiplayer.GetMainPlayer();
            if (main == null) return;

            if ((byte)main.ActorNumber != shooterActorId) return;
            if (__instance == main) return;

            HitAnnouncer.ReportKill(__instance);
        }
    }

    /// <summary>
    /// Speaks the direction incoming fire is coming from - the audible equivalent of
    /// the red damage cursor a sighted player sees the instant a bullet lands. Bursts
    /// are throttled so an automatic weapon does not machine-gun the screen reader, but
    /// a shot from a NEW direction is spoken promptly so a flanker is never masked by
    /// the throttle on the enemy already in front of you.
    /// </summary>
    public static class IncomingFireAnnouncer
    {
        // Steady fire from one direction repeats no faster than this.
        private const float MinInterval = 1.5f;
        // Even a direction change will not re-fire faster than this, to stop a target
        // crossing a bucket boundary from stuttering.
        private const float MinReannounce = 0.6f;

        private static float _lastSpeakTime;
        private static string _lastDirection;
        private static byte _lastShooterId;

        public static void Report(CharacterMultiplayer victim, byte shooterActorId)
        {
            var shooter = CharacterMultiplayer.GetPlayer(shooterActorId);

            string direction = shooter != null
                ? Bearings.Relative(victim.transform, Targeting.ChestOf(shooter))
                : null;

            float now = Time.time;
            bool newSource = shooterActorId != _lastShooterId || direction != _lastDirection;
            bool floor = now - _lastSpeakTime >= (newSource ? MinReannounce : MinInterval);
            if (!floor) return;

            _lastSpeakTime = now;
            _lastDirection = direction;
            _lastShooterId = shooterActorId;

            // Urgent: it should win over loot and landmark chatter, the same as a
            // close-enemy callout.
            ScreenReaderManager.Speak(direction == null ? "Taking fire" : $"Taking fire, {direction}", true);
        }

        public static void Reset()
        {
            _lastSpeakTime = 0f;
            _lastDirection = null;
            _lastShooterId = 0;
        }
    }

    /// <summary>
    /// Throttles and speaks hit markers. Automatic weapons land hits far faster
    /// than speech can keep up, so hits are accumulated and spoken at a readable
    /// rate rather than one announcement per bullet.
    /// </summary>
    public static class HitAnnouncer
    {
        private const float MinInterval = 0.6f; // fastest we will speak hit markers
        private const float FullHealth = 255f;  // health is a byte, 255 == full

        private static CharacterMultiplayer _pendingVictim;
        private static int _pendingDamage;
        private static int _pendingHealth;
        private static bool _hasPending;
        private static float _lastSpeakTime;

        public static void ReportHit(CharacterMultiplayer victim, int damage, int healthLeft)
        {
            // Switching targets: get the previous target's tally out first.
            if (_hasPending && _pendingVictim != null && _pendingVictim != victim)
                SpeakPending();

            _pendingVictim = victim;
            _pendingDamage += damage;
            _pendingHealth = healthLeft;
            _hasPending = true;

            if (Time.time - _lastSpeakTime >= MinInterval)
                SpeakPending();
        }

        public static void ReportKill(CharacterMultiplayer victim)
        {
            // A kill supersedes any unspoken damage tally for that victim.
            if (_pendingVictim == victim)
            {
                _hasPending = false;
                _pendingDamage = 0;
            }

            _lastSpeakTime = Time.time;
            ScreenReaderManager.Speak($"Eliminated {NameOf(victim)}");
        }

        /// <summary>
        /// Flushes a tally that was accumulated while throttled, so the last hits
        /// of a burst still get announced once the player stops firing.
        /// </summary>
        public static void Tick()
        {
            if (!_hasPending) return;
            if (Time.time - _lastSpeakTime < MinInterval) return;
            SpeakPending();
        }

        private static void SpeakPending()
        {
            if (!_hasPending) return;
            _hasPending = false;

            var victim = _pendingVictim;
            int damage = _pendingDamage;
            _pendingDamage = 0;

            if (victim == null) return;

            // Health is a 0-255 byte; percentages are far more meaningful to speak.
            int damagePct = Mathf.RoundToInt(damage / FullHealth * 100f);
            int healthPct = Mathf.CeilToInt(_pendingHealth / FullHealth * 100f);
            if (damagePct < 1) damagePct = 1;

            _lastSpeakTime = Time.time;
            ScreenReaderManager.Speak($"Tagged {NameOf(victim)} for {damagePct}, {healthPct} percent left");
        }

        private static string NameOf(CharacterMultiplayer c)
        {
            if (c == null) return "enemy";
            return string.IsNullOrEmpty(c.Nickname) ? "enemy" : c.Nickname;
        }

        public static void Reset()
        {
            _pendingVictim = null;
            _pendingDamage = 0;
            _hasPending = false;
        }
    }
}
