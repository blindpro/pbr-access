using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Audio targeting system for blind/low-vision players.
    ///
    /// While an enemy is within a detection radius AND within the current weapon's
    /// effective range (and has clear line of sight), this plays 3D positional
    /// "radar" beeps at the enemy's world position so the player can turn toward
    /// the sound and center it. The beeps speed up the closer the crosshair gets
    /// to the enemy. Once the crosshair is inside the enemy's silhouette (i.e. a
    /// shot would connect), the beeps stop and a steady solid LOCK tone plays,
    /// telling the player they can fire and hit.
    ///
    /// It always targets the single most-centered eligible enemy so the audio
    /// stays readable even with several enemies around.
    ///
    /// Separately, it speaks where the nearest enemy is - including ones behind
    /// cover that cannot currently be shot - so the player knows the threat is
    /// there even when no targeting beeps are playing. Callouts name the bearing
    /// ("front left"), elevation when it differs ("right above"), and the cover
    /// state, and they call out the transitions that change what the player can do:
    /// "Enemy took cover", "Enemy in the open", "Enemy close, behind".
    ///
    /// Enemies behind cover also get a slow, dull 3D pip at their position, so
    /// their direction is audible and not only spoken.
    /// </summary>
    public class AudioTargeting
    {
        private const float ScanInterval = 0.08f;
        private float _scanTimer;

        // Absolute cap on how far an enemy can be to register at all. The actual
        // gate is min(this, current weapon range).
        private const float DetectionRadius = 80f;

        // "Can I hit it" test: the crosshair must fall within the enemy silhouette.
        // Half-width in meters, converted to an angular threshold at the target's
        // distance and clamped so it is neither impossibly tight nor too loose.
        // Deliberately generous - aim assist nudges the rest of the way once you
        // fire, so a tight geometric threshold made the lock nearly unreachable.
        private const float LockTargetHalfWidth = 1.2f;
        private const float LockAngleMin = 5f;
        private const float LockAngleMax = 18f;
        private const float LockExitMultiplier = 1.5f; // hysteresis so lock doesn't flicker

        // Forgiveness radius for the "would this shot land" sweep, in meters. A
        // plain ray only locks on a pixel-perfect line, which at 20 m is a three
        // degree window - unfindable by ear. Sweeping a sphere of roughly torso
        // width means the lock engages when the shot lands or very nearly does.
        private const float LockSweepRadius = 0.5f;

        // Near-lock: the missing final approach cue. Between the lock threshold and
        // this multiple of it, a pulsing tone plays - "one nudge away" - so the last
        // few degrees are audible instead of a silent gap before the solid tone.
        private const float NearLockMultiplier = 2.5f;

        // Positional beep pacing: slower and lower when the enemy is off to the
        // side, faster and higher as the crosshair approaches center.
        private const float BeepSlowInterval = 0.9f;
        private const float BeepFastInterval = 0.1f;
        private const float BeepCenteringWindow = 90f; // degrees over which the ramp happens
        private const float BeepPitchFar = 1f;
        private const float BeepPitchNear = 1.8f;
        private float _beepTimer;

        // Spoken situational awareness. Unlike targeting, this ignores line of
        // sight so the player is told about enemies hiding behind cover too.
        private const float AwarenessRadius = 45f;
        private const float AwarenessInterval = 1f;   // how often we re-evaluate
        private const float AwarenessReannounce = 5f; // refresh even when nothing changed
        private const float CloseRange = 12f;         // inside this, callouts are urgent
        private const float CloseReannounce = 2f;     // and refresh far more often
        private float _awarenessTimer;
        private float _awarenessReTimer;
        private string _awareKey;
        private int _awareEnemyId;
        private bool _awareBehindCover;

        // Behind-cover enemies get their own slow, dull pip at their world position,
        // so "someone is over there but you can't shoot them yet" is audible as
        // direction, not just as a spoken sentence.
        private const float BlockedBeepRange = 35f;
        private const float BlockedBeepInterval = 1.4f;
        private float _blockedBeepTimer;
        private bool _blockedFound;
        private Vector3 _blockedPos;

        private AudioClip _enemyBeep;
        private AudioClip _blockedBeep;
        private AudioClip _lockTone;
        private AudioClip _nearLockTone;
        private AudioSource _lockSource;
        private AudioSource _nearLockSource;
        private bool _locked;
        private bool _nearLocked;
        private bool _spokeNoAmmo;

        private CharacterMultiplayer _currentTarget;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead()) { Reset(); return; }

            if (MatchmakingManager.Instance == null) { Reset(); return; }
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing) { Reset(); return; }

            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isParachuting) { Reset(); return; }

            EnsureClips();

            _scanTimer -= Time.deltaTime;
            _beepTimer -= Time.deltaTime;
            _blockedBeepTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = ScanInterval;

            GetAim(player, out Vector3 aimOrigin, out Vector3 aimDir);

            // Spoken awareness runs on its own slower cadence and covers enemies
            // we cannot currently shoot (behind cover, out of the aim cone).
            _awarenessTimer -= ScanInterval;
            _awarenessReTimer -= ScanInterval;
            if (_awarenessTimer <= 0f)
            {
                _awarenessTimer = AwarenessInterval;
                UpdateAwareness(player, aimOrigin);
            }

            float maxRange = Mathf.Min(GetWeaponRange(player), DetectionRadius);

            CharacterMultiplayer best = null;
            float bestAngle = float.MaxValue;
            float bestDist = 0f;
            Vector3 bestPos = Vector3.zero;

            // Nearest enemy we can see is behind cover (drives the dull pip).
            float blockedDist = float.MaxValue;
            _blockedFound = false;

            foreach (var other in CharacterMultiplayer.characters)
            {
                if (!IsHostile(player, other)) continue;

                Vector3 targetPos = other.transform.position + Vector3.up * 1.2f;
                Vector3 toTarget = targetPos - aimOrigin;
                float dist = toTarget.magnitude;
                if (dist < 0.5f) continue;
                if (dist > maxRange && dist > BlockedBeepRange) continue;

                // Must have clear line of sight - if a wall blocks it, we can't hit
                // them, but they still get a behind-cover pip so the player can hear
                // which way the threat is.
                if (Physics.Raycast(aimOrigin, toTarget.normalized, dist - 0.5f,
                    GetObstacleMask(), QueryTriggerInteraction.Ignore))
                {
                    if (dist <= BlockedBeepRange && dist < blockedDist)
                    {
                        blockedDist = dist;
                        _blockedFound = true;
                        _blockedPos = targetPos;
                    }
                    continue;
                }

                if (dist > maxRange) continue; // visible, but out of weapon range

                float angle = Vector3.Angle(aimDir, toTarget);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = other;
                    bestDist = dist;
                    bestPos = targetPos;
                }
            }

            if (best == null)
            {
                ClearTarget();

                // Nothing shootable: pip the nearest enemy we know is behind cover.
                if (_blockedFound && _blockedBeepTimer <= 0f)
                {
                    _blockedBeepTimer = BlockedBeepInterval;
                    PlaySpatialBeep(_blockedBeep, _blockedPos, 0.45f);
                }
                return;
            }

            // Spoken callouts are handled by UpdateAwareness; switching targets
            // just drops any existing lock.
            if (best != _currentTarget)
            {
                _currentTarget = best;
                _locked = false;
            }

            // Angular size of the enemy at this distance = when a shot would land.
            float lockAngle = Mathf.Clamp(
                Mathf.Atan2(LockTargetHalfWidth, bestDist) * Mathf.Rad2Deg,
                LockAngleMin, LockAngleMax);

            // Hysteresis: easier to keep the lock than to acquire it. A sweep that
            // actually strikes the enemy always locks, however wide the angle is -
            // the tone must never be missing when the shot genuinely connects.
            bool wantLock = _locked
                ? bestAngle <= lockAngle * LockExitMultiplier
                : bestAngle <= lockAngle;
            if (!wantLock)
                wantLock = IsShotOnTarget(player, aimOrigin, aimDir, maxRange, best);

            // Just outside the lock: pulse, so the player can hear the last few
            // degrees closing instead of hunting a silent window.
            bool wantNearLock = !wantLock && bestAngle <= lockAngle * NearLockMultiplier;

            // An empty gun cannot land a shot, so the lock tone would be a lie.
            // Say "reload" once instead and keep the tone silent.
            bool canFire = HasAmmunition(player);
            if (!canFire)
            {
                if (wantLock && !_spokeNoAmmo)
                {
                    _spokeNoAmmo = true;
                    ScreenReaderManager.Speak("Reload");
                }
                wantLock = false;
                wantNearLock = false;
            }
            else
            {
                _spokeNoAmmo = false;
            }

            if (wantLock)
            {
                StopNearLock();
                StartLock();
            }
            else if (wantNearLock)
            {
                StopLock();
                StartNearLock();
            }
            else
            {
                StopLock();
                StopNearLock();

                // Beep faster AND higher as the crosshair nears the enemy, so
                // there are two independent cues for closing in on the lock.
                float centering = 1f - Mathf.Clamp01(bestAngle / BeepCenteringWindow);
                float interval = Mathf.Lerp(BeepSlowInterval, BeepFastInterval, centering);
                float pitch = Mathf.Lerp(BeepPitchFar, BeepPitchNear, centering);
                if (_beepTimer <= 0f)
                {
                    _beepTimer = interval;
                    PlaySpatialBeep(_enemyBeep, bestPos, 0.7f, pitch);
                }
            }
        }

        /// <summary>Full stop: leaving gameplay, dying, parachuting.</summary>
        private void Reset()
        {
            ClearTarget();
            _awareKey = null;
            _awareEnemyId = 0;
            _spokeNoAmmo = false;
            _blockedFound = false;
        }

        /// <summary>
        /// Drops the shootable target only. Spoken-awareness state is deliberately
        /// kept - clearing it here would make every behind-cover enemy re-announce
        /// on the next awareness tick, once per second, forever.
        /// </summary>
        private void ClearTarget()
        {
            StopLock();
            StopNearLock();
            _currentTarget = null;
        }

        /// <summary>
        /// Enemies only: not us, not the local player, not a squad mate, not a
        /// spectator, and still alive.
        /// </summary>
        internal static bool IsHostile(CharacterMultiplayer player, CharacterMultiplayer other)
        {
            if (other == null || other == player) return false;
            if (other.isMainPlayer || other.isSpectating) return false;
            if (other.IsDead()) return false;
            if (player.IsSquadMember(other)) return false;
            return true;
        }

        /// <summary>
        /// True when a shot fired straight down the aim direction would strike this
        /// enemy before anything solid - the most honest "you can fire now" test.
        /// </summary>
        private static bool IsShotOnTarget(CharacterMultiplayer player, Vector3 origin,
            Vector3 dir, float range, CharacterMultiplayer target)
        {
            // Start just ahead of the camera so the player's own body, arms and
            // weapon model are never mistaken for cover.
            Vector3 start = origin + dir * 0.5f;
            RaycastHit[] hits = Physics.SphereCastAll(start, LockSweepRadius, dir, range,
                ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                var character = hit.collider.GetComponentInParent<CharacterMultiplayer>();

                // Anything hanging off our own rig sits in front of every shot we
                // take - held weapons included, which have no character component.
                if (character == player) continue;
                if (hit.collider.transform.IsChildOf(player.transform)) continue;
                // First character the sweep touches decides it: the target means the
                // shot lands, anyone else means they are in the way.
                if (character != null) return character == target;

                // Geometry is deliberately ignored here. The sweep has width, so it
                // clips walls merely *beside* a clear shot; the precise line of sight
                // ray in the scan loop has already ruled out real cover.
            }

            return false;
        }

        /// <summary>False only when we can positively confirm the gun is empty.</summary>
        private static bool HasAmmunition(CharacterMultiplayer player)
        {
            var character = player.GetComponent<Character>();
            if (character == null) return true;

            var inventory = character.GetInventory();
            if (inventory == null) return true;

            var weapon = inventory.GetEquipped();
            if (weapon == null) return true;

            return weapon.HasAmmunition();
        }

        /// <summary>
        /// Speaks where the nearest enemy is, including when they are unshootable.
        /// Line of sight is reported as "behind cover" so the player understands
        /// why no targeting beeps are playing. Re-announces only when the enemy,
        /// their direction, or their cover state actually changes, plus a periodic
        /// refresh so the distance stays current.
        /// </summary>
        private void UpdateAwareness(CharacterMultiplayer player, Vector3 aimOrigin)
        {
            CharacterMultiplayer nearest = null;
            float nearestDist = float.MaxValue;
            Vector3 nearestPos = Vector3.zero;

            foreach (var other in CharacterMultiplayer.characters)
            {
                if (!IsHostile(player, other)) continue;

                Vector3 pos = other.transform.position + Vector3.up * 1.2f;
                float dist = Vector3.Distance(aimOrigin, pos);
                if (dist > AwarenessRadius) continue;

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = other;
                    nearestPos = pos;
                }
            }

            if (nearest == null)
            {
                _awareKey = null;
                _awareEnemyId = 0;
                return;
            }

            bool behindCover = Physics.Raycast(aimOrigin, (nearestPos - aimOrigin).normalized,
                nearestDist - 0.5f, GetObstacleMask(), QueryTriggerInteraction.Ignore);

            string direction = RelativeDirection(player.transform, nearestPos);
            string elevation = Elevation(player.transform.position.y, nearestPos.y);
            string where = elevation == null ? direction : direction + " " + elevation;
            bool isClose = nearestDist <= CloseRange;

            int id = nearest.GetInstanceID();
            string key = id + "|" + where + "|" + behindCover;

            bool sameEnemy = id == _awareEnemyId;
            bool coverChanged = sameEnemy && behindCover != _awareBehindCover;
            bool changed = key != _awareKey;
            // Close enemies refresh far more often - at knife range, a five second
            // old bearing is worse than useless.
            bool periodic = _awarenessReTimer <= 0f;
            if (!changed && !periodic) return;

            _awareKey = key;
            _awareEnemyId = id;
            _awareBehindCover = behindCover;
            _awarenessReTimer = isClose ? CloseReannounce : AwarenessReannounce;

            int meters = Mathf.RoundToInt(nearestDist);
            string phrase;

            if (isClose)
            {
                // Urgent, and short enough to hear before it matters.
                phrase = behindCover
                    ? $"Enemy close behind cover, {where}"
                    : $"Enemy close, {where}, {meters} meters";
            }
            else if (coverChanged)
            {
                // The one transition worth naming: it flips whether shooting works.
                phrase = behindCover
                    ? $"Enemy took cover, {where}, {meters} meters"
                    : $"Enemy in the open, {where}, {meters} meters";
            }
            else
            {
                phrase = behindCover
                    ? $"Enemy behind cover, {where}, {meters} meters"
                    : $"Enemy {where}, {meters} meters";
            }

            // Close-range threats interrupt; everything else waits its turn behind
            // navigation and loot callouts.
            ScreenReaderManager.Speak(phrase, isClose);
        }

        private static string Elevation(float playerY, float targetY)
        {
            float dy = targetY - playerY;
            if (dy > 3f) return "above";
            if (dy < -3f) return "below";
            return null;
        }

        private void StartLock()
        {
            if (_locked && _lockSource != null && _lockSource.isPlaying) return;
            _locked = true;
            EnsureLockSource();
            if (_lockSource != null && !_lockSource.isPlaying)
                _lockSource.Play();
        }

        private void StopLock()
        {
            _locked = false;
            if (_lockSource != null && _lockSource.isPlaying)
                _lockSource.Stop();
        }

        private void StartNearLock()
        {
            if (_nearLocked && _nearLockSource != null && _nearLockSource.isPlaying) return;
            _nearLocked = true;
            EnsureNearLockSource();
            if (_nearLockSource != null && !_nearLockSource.isPlaying)
                _nearLockSource.Play();
        }

        private void StopNearLock()
        {
            _nearLocked = false;
            if (_nearLockSource != null && _nearLockSource.isPlaying)
                _nearLockSource.Stop();
        }

        private void GetAim(CharacterMultiplayer player, out Vector3 origin, out Vector3 dir)
        {
            var tp = player.GetComponent<ThirdPerson>();
            if (tp != null && tp.fps_camera != null)
            {
                origin = tp.fps_camera.transform.position;
                dir = tp.fps_camera.transform.forward;
            }
            else
            {
                origin = player.transform.position + Vector3.up * 1.5f;
                dir = player.transform.forward;
            }
        }

        private float GetWeaponRange(CharacterMultiplayer player)
        {
            var inv = player.GetComponent<CharacterInventory>();
            string name = null;
            if (inv != null)
            {
                int slot = inv.GetCurrentWeapon();
                var w = slot == 0 ? inv.weapon1 : inv.weapon2;
                if (w != null) name = w.name;
            }

            if (string.IsNullOrEmpty(name)) return 35f; // default starter handgun
            if (name.Contains("Sniper")) return 120f;
            if (name.Contains("Assault")) return 70f;
            if (name.Contains("Shotgun")) return 20f;
            if (name.Contains("SMG")) return 45f;
            if (name.Contains("Rocket Launcher") || name.Contains("Grenade Launcher")) return 60f;
            if (name.Contains("Handgun")) return 35f;
            return 50f;
        }

        private void EnsureClips()
        {
            const int sampleRate = 44100;

            // Enemy radar beep: a short low-ish pip (520 Hz), distinct from the
            // loot (880 Hz) and ammo (1245 Hz double) cues.
            if (_enemyBeep == null)
            {
                float duration = 0.08f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1f - (t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.5f * env;
                }
                _enemyBeep = AudioClip.Create("EnemyBeep", sampleCount, 1, sampleRate, false);
                _enemyBeep.SetData(samples, 0);
            }

            // Behind-cover pip: a dull, slow 300 Hz tone with a soft attack. It has
            // to be unmistakable against the bright 520 Hz targeting pip, because it
            // means the opposite thing - "there, but you cannot shoot them".
            if (_blockedBeep == null)
            {
                float duration = 0.18f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float attack = Mathf.Clamp01(t / 0.04f);
                    float env = attack * (1f - (t / duration));
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.5f * env;
                }
                _blockedBeep = AudioClip.Create("EnemyBlockedBeep", sampleCount, 1, sampleRate, false);
                _blockedBeep.SetData(samples, 0);
            }

            // Near-lock tone: the same 990 Hz voice as the lock, but chopped into
            // fast pulses. Sharing the pitch is the point - the player hears the
            // pulses fuse into the solid lock tone as they settle onto the target.
            if (_nearLockTone == null)
            {
                const float freq = 990f;
                const float pulse = 0.06f; // 59.4 whole cycles - starts and ends near zero
                float duration = pulse * 2f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    if (t >= pulse) { samples[i] = 0f; continue; }
                    // Soft edges so the loop does not click at the pulse boundary.
                    float edge = Mathf.Min(t, pulse - t) / 0.01f;
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * Mathf.Clamp01(edge);
                }
                _nearLockTone = AudioClip.Create("NearLockTone", sampleCount, 1, sampleRate, false);
                _nearLockTone.SetData(samples, 0);
            }

            // Lock tone: a steady, seamlessly-looping 990 Hz tone. 495 whole cycles
            // over 0.5s start and end at zero so the loop has no click.
            if (_lockTone == null)
            {
                const float freq = 990f;
                float duration = 0.5f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f;
                }
                _lockTone = AudioClip.Create("LockTone", sampleCount, 1, sampleRate, false);
                _lockTone.SetData(samples, 0);
            }
        }

        private void EnsureNearLockSource()
        {
            if (_nearLockSource != null) return;

            var obj = new GameObject("AudioTargetNearLock");
            Object.DontDestroyOnLoad(obj);
            _nearLockSource = obj.AddComponent<AudioSource>();
            _nearLockSource.clip = _nearLockTone;
            _nearLockSource.loop = true;
            _nearLockSource.spatialBlend = 0f; // 2D, same as the lock it leads into
            _nearLockSource.volume = 0.35f;    // quieter - it is a hint, not the signal
            _nearLockSource.playOnAwake = false;
        }

        private void EnsureLockSource()
        {
            if (_lockSource != null) return;

            var obj = new GameObject("AudioTargetLock");
            Object.DontDestroyOnLoad(obj);
            _lockSource = obj.AddComponent<AudioSource>();
            _lockSource.clip = _lockTone;
            _lockSource.loop = true;
            _lockSource.spatialBlend = 0f; // 2D - steady "you can fire" signal
            _lockSource.volume = 0.5f;
            _lockSource.playOnAwake = false;
        }

        private void PlaySpatialBeep(AudioClip clip, Vector3 position, float volume, float pitch = 1f)
        {
            if (clip == null) return;

            var tempObj = new GameObject("EnemyBeepTemp");
            tempObj.transform.position = position;
            var source = tempObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f; // fully 3D so direction is audible
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = DetectionRadius + 5f;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            // Higher pitch plays the clip back faster, so scale the cleanup delay.
            Object.Destroy(tempObj, clip.length / Mathf.Max(0.01f, pitch) + 0.1f);
        }

        private static string RelativeDirection(Transform playerTransform, Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - playerTransform.position;
            toTarget.y = 0;

            float angle = Vector3.SignedAngle(playerTransform.forward, toTarget, Vector3.up);

            if (angle >= -22.5f && angle < 22.5f) return "ahead";
            if (angle >= 22.5f && angle < 67.5f) return "front right";
            if (angle >= 67.5f && angle < 112.5f) return "right";
            if (angle >= 112.5f && angle < 157.5f) return "behind right";
            if (angle >= -67.5f && angle < -22.5f) return "front left";
            if (angle >= -112.5f && angle < -67.5f) return "left";
            if (angle >= -157.5f && angle < -112.5f) return "behind left";
            return "behind";
        }

        internal static int GetObstacleMask()
        {
            int mask = 1 << 0; // Default
            int ground = LayerMask.NameToLayer("Ground");
            if (ground >= 0) mask |= 1 << ground;
            int terrain = LayerMask.NameToLayer("Terrain");
            if (terrain >= 0) mask |= 1 << terrain;
            int building = LayerMask.NameToLayer("Building");
            if (building >= 0) mask |= 1 << building;
            int env = LayerMask.NameToLayer("Environment");
            if (env >= 0) mask |= 1 << env;
            return mask;
        }
    }
}
