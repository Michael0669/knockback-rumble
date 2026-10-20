using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Traffic cop for every voice in the game. Only one line plays at a time, with a
    /// short breath between lines, and bots get far less airtime than the announcers:
    ///   - Important announcer lines (intro, GO, your falls, results) interrupt anyone.
    ///   - Other announcer lines wait until it's quiet and at least AnnouncerGap since the last one.
    ///   - Bot barks wait until it's quiet, at least BotGap since any bot spoke,
    ///     and only bots near you are allowed to talk.
    /// </summary>
    public static class VoiceDirector
    {
        /// <summary>
        /// Master switch for every spoken line (announcers and bot barks, audio and subtitles).
        /// Off for now: the placeholder robot voices got annoying. Flip to true to bring them back.
        /// </summary>
        public static bool Enabled = false;

        public static float Breath = 0.6f;          // silence after any line
        public static float AnnouncerGap = 4f;      // between unforced announcer lines
        public static float BotGap = 7f;            // between any two bot barks
        public static float BotHearingRange = 25f;  // bots farther than this stay quiet

        static AudioSource current;
        static float busyUntil = -99f, lastAnnouncer = -99f, lastBot = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            busyUntil = lastAnnouncer = lastBot = -99f;
        }

        public static bool Quiet => Time.time >= busyUntil + Breath;

        public static bool MayAnnounce(bool force) =>
            Enabled && (force || (Quiet && Time.time - lastAnnouncer >= AnnouncerGap));

        public static bool MayBark(Vector3 speakerPosition)
        {
            if (!Enabled || !Quiet || Time.time - lastBot < BotGap) return false;
            var cam = Camera.main;
            return cam == null || Vector3.Distance(cam.transform.position, speakerPosition) <= BotHearingRange;
        }

        /// <summary>Plays the clip on the source, cutting off whoever was talking.</summary>
        public static void Play(AudioSource source, AudioClip clip, bool announcer)
        {
            if (current != null && current != source && current.isPlaying) current.Stop();
            current = source;
            float length = clip != null ? clip.length : 2f;
            busyUntil = Time.time + length;
            if (announcer) lastAnnouncer = Time.time + length;
            else lastBot = Time.time;
            if (source == null || clip == null) return;
            source.Stop();
            source.clip = clip;
            source.Play();
        }
    }
}
