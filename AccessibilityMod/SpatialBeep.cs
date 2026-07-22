using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Tones played at a point in the world, so they arrive from the direction of the
    /// thing they are about. Walking onto a sound is more precise than steering off a
    /// spoken bearing, and it keeps working in the last few metres where "front left"
    /// stops meaning anything useful.
    ///
    /// Generated rather than shipped as assets: a sine and an envelope are a few lines,
    /// and it keeps the mod a single dll.
    /// </summary>
    public static class SpatialBeep
    {
        private const int SampleRate = 44100;

        /// <summary>
        /// A tone of one or more pulses. Each pulse fades out over its own length, and
        /// restarts the waveform, which is what makes a two-pulse tone read as two beeps
        /// rather than one interrupted one.
        /// </summary>
        public static AudioClip Tone(string name, float frequency, float pulseLength,
            int pulses = 1, float gap = 0f)
        {
            float cycle = pulseLength + gap;
            float duration = pulses * pulseLength + (pulses - 1) * gap;

            int sampleCount = (int)(SampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;

                int pulse = (int)(t / cycle);
                float local = t - pulse * cycle;

                if (pulse >= pulses || local >= pulseLength) continue;

                float envelope = 1f - local / pulseLength;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * local) * 0.5f * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Plays a clip at a world position as 3D audio, on a throwaway object that
        /// destroys itself once the sound has finished.
        /// </summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume, float maxDistance)
        {
            if (clip == null) return;

            var temp = new GameObject("AccessibilityBeep");
            temp.transform.position = position;

            var source = temp.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f; // fully 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = maxDistance;
            source.volume = volume;
            source.Play();

            Object.Destroy(temp, clip.length + 0.1f);
        }
    }
}
