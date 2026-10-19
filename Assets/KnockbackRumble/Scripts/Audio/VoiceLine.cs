using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>How a character sounds when placeholder voices are generated with text-to-speech.</summary>
    public class VoiceStyle
    {
        public bool female;
        [Tooltip("-10 (slow) to 10 (fast)")] public int rate;
        [Tooltip("x-low, low, medium, high or x-high")] public string pitch = "medium";

        public VoiceStyle(bool female, int rate, string pitch)
        {
            this.female = female;
            this.rate = rate;
            this.pitch = pitch;
        }
    }

    /// <summary>
    /// One recorded line. The audio file lives at Resources/VO/{id}.wav; until it exists
    /// the line just doesn't play. Every line is name-free so it works for any player.
    /// </summary>
    public class VoiceLine
    {
        public string id;
        public string speaker;
        public string text;
        public VoiceStyle style;

        public VoiceLine(string id, string speaker, string text, VoiceStyle style)
        {
            this.id = id;
            this.speaker = speaker;
            this.text = text;
            this.style = style;
        }

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        /// <summary>The audio clip for this line, or null if it hasn't been generated/recorded yet.</summary>
        public AudioClip Clip
        {
            get
            {
                if (cache.TryGetValue(id, out var clip)) return clip;
                clip = Resources.Load<AudioClip>("VO/" + id);
                cache[id] = clip;
                return clip;
            }
        }

        /// <summary>Builds numbered lines: prefix_1, prefix_2, ...</summary>
        public static VoiceLine[] Set(string prefix, string speaker, VoiceStyle style, params string[] texts)
        {
            var lines = new VoiceLine[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                lines[i] = new VoiceLine(prefix + "_" + (i + 1), speaker, texts[i], style);
            return lines;
        }

        public static VoiceLine Pick(VoiceLine[] lines) =>
            lines == null || lines.Length == 0 ? null : lines[Random.Range(0, lines.Length)];
    }
}
