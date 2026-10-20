using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Sound effects and music. Every sound lives in Art/Resources/SFX/&lt;name&gt;.bytes as a
    /// 16-bit WAV (made by Tools/make_sfx.py) and is decoded here, so Unity's audio import
    /// settings never matter.
    ///
    ///   Sfx.At("explosion", pos)      3D sound in the world (louder when close)
    ///   Sfx.Ui("ui_click")            2D sound in your ears
    ///   Sfx.Ui("fanfare", delay: 0.5f)
    /// Music switches itself between the menu loop and the in-game loop.
    /// Volumes (music / effects) are saved in PlayerPrefs.
    /// </summary>
    public static class Sfx
    {
        const string MusicKey = "kr_vol_music", EffectsKey = "kr_vol_sfx";

        /// <summary>Per-sound loudness so the set sits together (1 = as generated).</summary>
        static readonly Dictionary<string, float> Gains = new Dictionary<string, float>
        {
            { "go_horn", 0.55f }, { "end_buzzer", 0.5f }, { "count_beep", 0.45f }, { "hit_heavy", 0.75f },
            { "explosion", 0.9f }, { "shotgun", 0.8f }, { "jump", 0.35f }, { "double_jump", 0.4f }, { "land", 0.45f },
            { "dive", 0.5f }, { "coin", 0.5f }, { "cash_lose", 0.55f }, { "tile_crack", 0.7f }, { "tile_fall", 0.6f },
            { "beep", 0.4f }, { "bounce", 0.6f }, { "ui_click", 0.5f }, { "ui_equip", 0.6f }, { "applause", 0.6f },
            { "fall_whistle", 0.55f }, { "siren", 0.45f }, { "bomb_whistle", 0.5f }, { "sniper", 1f }, { "pistol", 0.7f },
            { "crusher", 0.7f }, { "hitmarker", 0.6f }, { "empty_click", 0.8f },
        };

        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        static SfxRunner runner;
        static float musicVolume = -1f, effectsVolume = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            clips.Clear();
            lastPlayed.Clear();
            runner = null;
            musicVolume = effectsVolume = -1f;
        }

        public static float MusicVolume
        {
            get { if (musicVolume < 0f) musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.6f); return musicVolume; }
            set { musicVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, musicVolume); }
        }

        public static float EffectsVolume
        {
            get { if (effectsVolume < 0f) effectsVolume = PlayerPrefs.GetFloat(EffectsKey, 0.85f); return effectsVolume; }
            set { effectsVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(EffectsKey, effectsVolume); }
        }

        public static void Ensure()
        {
            if (runner != null) return;
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<SfxRunner>();
        }

        /// <summary>A sound at a point in the world.</summary>
        public static void At(string name, Vector3 pos, float volume = 1f, float pitchJitter = 0.08f, float range = 45f)
        {
            if (!Allowed(name)) return;
            Ensure();
            var clip = Clip(name);
            if (clip == null) return;
            runner.Play3D(clip, pos, volume * Gain(name) * EffectsVolume, Pitch(pitchJitter), range);
        }

        /// <summary>A sound in your ears (UI, your own events).</summary>
        public static void Ui(string name, float volume = 1f, float pitchJitter = 0f, float delay = 0f)
        {
            if (delay <= 0f && !Allowed(name)) return;
            Ensure();
            var clip = Clip(name);
            if (clip == null) return;
            runner.Play2D(clip, volume * Gain(name) * EffectsVolume, Pitch(pitchJitter), delay);
        }

        static float Gain(string name) => Gains.TryGetValue(name, out float g) ? g : 1f;
        static float Pitch(float jitter) => jitter > 0f ? 1f + Random.Range(-jitter, jitter) : 1f;

        /// <summary>The same sound at most once every 40 ms (shotgun pellets, explosions, etc. don't stack up).</summary>
        static bool Allowed(string name)
        {
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(name, out float t) && now - t < 0.04f) return false;
            lastPlayed[name] = now;
            return true;
        }

        public static AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            var data = Resources.Load<TextAsset>("SFX/" + name);
            c = data != null ? Wav.ToClip(name, data.bytes) : null;
            if (data == null) Debug.LogWarning("[Knockback Rumble] Missing sound: SFX/" + name + ".bytes");
            clips[name] = c;
            return c;
        }
    }

    /// <summary>Minimal WAV reader: 16-bit PCM, mono or stereo.</summary>
    public static class Wav
    {
        public static AudioClip ToClip(string name, byte[] b)
        {
            try
            {
                if (b.Length < 44 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') return Bad(name);
                int channels = 1, rate = 22050, bits = 16;
                int pos = 12;
                while (pos + 8 <= b.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                    int size = System.BitConverter.ToInt32(b, pos + 4);
                    int body = pos + 8;
                    if (id == "fmt ")
                    {
                        channels = System.BitConverter.ToInt16(b, body + 2);
                        rate = System.BitConverter.ToInt32(b, body + 4);
                        bits = System.BitConverter.ToInt16(b, body + 14);
                    }
                    else if (id == "data")
                    {
                        if (bits != 16) return Bad(name);
                        size = Mathf.Min(size, b.Length - body);
                        int count = size / 2;
                        var samples = new float[count];
                        for (int i = 0; i < count; i++)
                            samples[i] = System.BitConverter.ToInt16(b, body + i * 2) / 32768f;
                        var clip = AudioClip.Create(name, count / channels, channels, rate, false);
                        clip.SetData(samples, 0);
                        return clip;
                    }
                    pos = body + size + (size & 1);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Knockback Rumble] Couldn't read sound " + name + ": " + e.Message);
            }
            return Bad(name);
        }

        static AudioClip Bad(string name)
        {
            Debug.LogWarning("[Knockback Rumble] " + name + " isn't a 16-bit WAV.");
            return null;
        }
    }
}
