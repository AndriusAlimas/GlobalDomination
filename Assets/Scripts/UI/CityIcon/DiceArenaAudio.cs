// Dice arena impact audio and Resources/procedural clips (used by CityIconUI physical roll).
using UnityEngine;

namespace GlobalDomination.UI
{
    /// <summary>Which surface the die collided with in the arena.</summary>
    public enum DiceSurfaceType
    {
        Floor,
        Wall
    }

    /// <summary>Collider tag for routing dice impact audio (floor vs wall).</summary>
    public sealed class DiceSurfaceTag : MonoBehaviour
    {
        public DiceSurfaceType surfaceType;
    }

    /// <summary>
    /// Plays short clack samples on physics contacts only (cooldown + capped gain).
    /// </summary>
    public sealed class DiceImpactAudio : MonoBehaviour
    {
        private const float MinImpactCooldown = 0.068f;
        private const float MinImpulse = 0.32f;
        private const float ImpactGain = 0.38f;
        private const float WallVolumeMax = 0.34f;
        private const float FloorVolumeMax = 0.28f;
        private const string GlobalAudioSourceName = "_CityIconUI_DiceAudioSource";

        private AudioSource audioSource;
        private AudioClip floorClip;
        private AudioClip wallClip;
        private float nextImpactTime;
        private static AudioSource globalAudioSource;

        public void Setup(AudioClip floor, AudioClip wall)
        {
            floorClip = floor;
            wallClip = wall;

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 80f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0.1f;
            audioSource.volume = 1f;
            audioSource.ignoreListenerPause = true;
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
            audioSource.priority = 24;
        }

        private static AudioSource EnsureGlobalAudioSource()
        {
            if (globalAudioSource != null)
            {
                globalAudioSource.volume = 0.92f;
                return globalAudioSource;
            }

            GameObject existing = GameObject.Find(GlobalAudioSourceName);
            GameObject host = existing != null ? existing : new GameObject(GlobalAudioSourceName);
            if (existing == null)
            {
                Object.DontDestroyOnLoad(host);
            }

            globalAudioSource = host.GetComponent<AudioSource>();
            if (globalAudioSource == null)
            {
                globalAudioSource = host.AddComponent<AudioSource>();
            }

            globalAudioSource.playOnAwake = false;
            globalAudioSource.spatialBlend = 0f;
            globalAudioSource.volume = 0.92f;
            globalAudioSource.ignoreListenerPause = true;
            globalAudioSource.bypassEffects = true;
            globalAudioSource.bypassListenerEffects = true;
            globalAudioSource.bypassReverbZones = true;
            globalAudioSource.priority = 0;
            return globalAudioSource;
        }

        private AudioSource ResolvePlaybackSource()
        {
            AudioSource global = EnsureGlobalAudioSource();
            if (global != null)
            {
                return global;
            }

            return audioSource;
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryPlayImpact(collision);
        }

        private void TryPlayImpact(Collision collision)
        {
            if (audioSource == null || Time.time < nextImpactTime || collision == null)
            {
                return;
            }

            float impulseMetric = Mathf.Max(
                collision.impulse.magnitude,
                collision.relativeVelocity.magnitude * 0.45f);
            if (impulseMetric < MinImpulse)
            {
                return;
            }

            DiceSurfaceType surfaceType = ResolveSurfaceType(collision);
            AudioClip clip = surfaceType == DiceSurfaceType.Wall ? wallClip : floorClip;
            if (clip == null)
            {
                return;
            }

            AudioSource playbackSource = ResolvePlaybackSource();
            if (playbackSource == null)
            {
                return;
            }

            float strength = Mathf.Clamp(impulseMetric, 0f, 14f);
            float rawVol = surfaceType == DiceSurfaceType.Wall
                ? Mathf.Clamp01(0.11f + strength * 0.022f)
                : Mathf.Clamp01(0.09f + strength * 0.018f);
            float cap = surfaceType == DiceSurfaceType.Wall ? WallVolumeMax : FloorVolumeMax;
            float volume = Mathf.Min(rawVol, cap) * ImpactGain;

            float basePitch = surfaceType == DiceSurfaceType.Wall ? 1.04f : 0.99f;
            playbackSource.pitch = basePitch + Random.Range(-0.035f, 0.045f);
            playbackSource.PlayOneShot(clip, volume);

            nextImpactTime = Time.time + MinImpactCooldown;
        }

        private static DiceSurfaceType ResolveSurfaceType(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint point = collision.GetContact(i);
                Collider other = point.otherCollider;
                if (other == null)
                {
                    continue;
                }

                DiceSurfaceTag tag = other.GetComponent<DiceSurfaceTag>();
                if (tag != null)
                {
                    return tag.surfaceType;
                }

                string name = other.gameObject.name;
                if (name.Contains("Wall") || name.Contains("Corner"))
                {
                    return DiceSurfaceType.Wall;
                }
            }

            return DiceSurfaceType.Floor;
        }
    }

    internal enum DiceClackKind
    {
        Floor,
        Wall
    }

    /// <summary>
    /// Loads Resources clips or synthesizes short clacks for dice impacts.
    /// </summary>
    internal static class CityIconDiceAudioClips
    {
        private static AudioClip floorClip;
        private static AudioClip wallClip;

        internal static void EnsureLoaded()
        {
            if (floorClip == null)
            {
                floorClip = LoadDiceClipOrPhysicalFallback("Audio/dice_floor_impact", DiceClackKind.Floor);
            }

            if (wallClip == null)
            {
                wallClip = LoadDiceClipOrPhysicalFallback("Audio/dice_wall_impact", DiceClackKind.Wall);
            }
        }

        internal static AudioClip FloorClip => floorClip;
        internal static AudioClip WallClip => wallClip;

        private static AudioClip LoadDiceClipOrPhysicalFallback(string resourcesPath, DiceClackKind kind)
        {
            AudioClip loaded = Resources.Load<AudioClip>(resourcesPath);
            if (loaded != null)
            {
                return loaded;
            }

            string fallbackName = kind == DiceClackKind.Floor ? "DiceFloorClack" : "DiceWallClack";
            return CreateRuntimeDiceClack(fallbackName, kind);
        }

        private static AudioClip CreateRuntimeDiceClack(string clipName, DiceClackKind kind)
        {
            const int sampleRate = 44100;
            float duration;
            float noiseTau;
            float brightness;
            float toneLo;
            float toneHi;
            float toneMix;
            float toneDecay;
            float ringFreq;
            float ringMix;
            float outGain;
            float attackRate;
            float ringDecayRate;

            if (kind == DiceClackKind.Wall)
            {
                duration = 0.052f;
                noiseTau = 0.0078f;
                brightness = 1.14f;
                toneLo = 1180f;
                toneHi = 2100f;
                toneMix = 0.095f;
                toneDecay = 48f;
                ringFreq = Random.Range(3200f, 5200f);
                ringMix = 0.22f;
                outGain = 1f;
                attackRate = 5200f;
                ringDecayRate = 62f;
            }
            else
            {
                duration = 0.07f;
                noiseTau = 0.012f;
                brightness = 1f;
                toneLo = 860f;
                toneHi = 1320f;
                toneMix = 0.12f;
                toneDecay = 34f;
                ringFreq = Random.Range(1800f, 3200f);
                ringMix = 0.11f;
                outGain = 1f;
                attackRate = 5200f;
                ringDecayRate = 62f;
            }

            float toneFreq = Random.Range(toneLo, toneHi);

            int sampleCount = Mathf.Max(320, Mathf.RoundToInt(duration * sampleRate));
            float[] samples = new float[sampleCount];
            float phase = Random.Range(0f, Mathf.PI * 2f);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float attack = 1f - Mathf.Exp(-t * attackRate);
                float nEnv = Mathf.Exp(-t / noiseTau);
                float w = (Random.value * 2f - 1f) * brightness;
                if (kind == DiceClackKind.Wall)
                {
                    float a = Mathf.Abs(w);
                    w = Mathf.Sign(w) * Mathf.Pow(a, 0.82f);
                }

                float noise = w * nEnv * attack;
                float tEnv = Mathf.Exp(-t * toneDecay);
                float body = Mathf.Sin(Mathf.PI * 2f * toneFreq * t + phase) * toneMix * tEnv * attack;
                float ring = Mathf.Sin(Mathf.PI * 2f * ringFreq * t + phase * 0.27f) * ringMix * Mathf.Exp(-t * ringDecayRate) * attack;
                float sample = (noise + body + ring) * outGain;
                sample = sample / (1f + Mathf.Abs(sample) * 0.72f);
                samples[i] = sample;
            }

            float peakAbs = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float a = Mathf.Abs(samples[i]);
                if (a > peakAbs)
                {
                    peakAbs = a;
                }
            }

            if (peakAbs > 0.0001f)
            {
                float normalize = 0.91f / peakAbs;
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[i] = Mathf.Clamp(samples[i] * normalize, -1f, 1f);
                }
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
