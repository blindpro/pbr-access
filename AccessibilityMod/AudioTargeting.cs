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
        private const float LockTargetHalfWidth = 0.7f;
        private const float LockAngleMin = 2f;
        private const float LockAngleMax = 10f;
        private const float LockExitMultiplier = 1.5f; // hysteresis so lock doesn't flicker

        // Positional beep pacing: slower when the enemy is off to the side,
        // faster as the crosshair approaches center.
        private const float BeepSlowInterval = 0.9f;
        private const float BeepFastInterval = 0.15f;
        private const float BeepCenteringWindow = 90f; // degrees over which the rate ramps
        private float _beepTimer;

        private AudioClip _enemyBeep;
        private AudioClip _lockTone;
        private AudioSource _lockSource;
        private bool _locked;

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
            if (_scanTimer > 0f) return;
            _scanTimer = ScanInterval;

            GetAim(player, out Vector3 aimOrigin, out Vector3 aimDir);

            float maxRange = Mathf.Min(GetWeaponRange(player), DetectionRadius);

            CharacterMultiplayer best = null;
            float bestAngle = float.MaxValue;
            float bestDist = 0f;
            Vector3 bestPos = Vector3.zero;

            foreach (var other in CharacterMultiplayer.characters)
            {
                if (other == null || other == player || other.IsDead()) continue;
                if (other.isMainPlayer) continue;

                Vector3 targetPos = other.transform.position + Vector3.up * 1.2f;
                Vector3 toTarget = targetPos - aimOrigin;
                float dist = toTarget.magnitude;
                if (dist > maxRange || dist < 0.5f) continue;

                // Must have clear line of sight - if a wall blocks it, we can't hit them.
                if (Physics.Raycast(aimOrigin, toTarget.normalized, dist - 0.5f,
                    GetObstacleMask(), QueryTriggerInteraction.Ignore))
                    continue;

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
                Reset();
                return;
            }

            // Announce a newly acquired target with direction and distance (once).
            if (best != _currentTarget)
            {
                _currentTarget = best;
                _locked = false;
                string dir = RelativeDirection(player.transform, bestPos);
                ScreenReaderManager.Speak($"Enemy {dir}, {Mathf.RoundToInt(bestDist)} meters", false);
            }

            // Angular size of the enemy at this distance = when a shot would land.
            float lockAngle = Mathf.Clamp(
                Mathf.Atan2(LockTargetHalfWidth, bestDist) * Mathf.Rad2Deg,
                LockAngleMin, LockAngleMax);

            // Hysteresis: easier to keep the lock than to acquire it.
            bool wantLock = _locked
                ? bestAngle <= lockAngle * LockExitMultiplier
                : bestAngle <= lockAngle;

            if (wantLock)
            {
                StartLock();
            }
            else
            {
                StopLock();

                // Beep faster as the crosshair nears the enemy.
                float centering = 1f - Mathf.Clamp01(bestAngle / BeepCenteringWindow);
                float interval = Mathf.Lerp(BeepSlowInterval, BeepFastInterval, centering);
                if (_beepTimer <= 0f)
                {
                    _beepTimer = interval;
                    PlaySpatialBeep(_enemyBeep, bestPos, 0.7f);
                }
            }
        }

        private void Reset()
        {
            StopLock();
            _currentTarget = null;
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

        private void PlaySpatialBeep(AudioClip clip, Vector3 position, float volume)
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
            source.Play();
            Object.Destroy(tempObj, clip.length + 0.1f);
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

        private static int GetObstacleMask()
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
