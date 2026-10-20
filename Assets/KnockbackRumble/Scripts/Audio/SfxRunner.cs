using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Plays the sounds: a pool of 3D sources, a few 2D ones, and two music players for crossfades.</summary>
    public class SfxRunner : MonoBehaviour
    {
        const int Pool3D = 24, Pool2D = 8;
        readonly AudioSource[] world = new AudioSource[Pool3D];
        readonly AudioSource[] flat = new AudioSource[Pool2D];
        readonly AudioSource[] music = new AudioSource[2];
        int nextWorld, nextFlat, activeMusic;
        string currentTrack;

        struct Delayed
        {
            public AudioClip clip;
            public float volume, pitch, at;
        }

        readonly List<Delayed> delayed = new List<Delayed>();

        void Awake()
        {
            for (int i = 0; i < Pool3D; i++)
            {
                var s = new GameObject("World_" + i).AddComponent<AudioSource>();
                s.transform.SetParent(transform, false);
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 4f;
                s.dopplerLevel = 0f;
                world[i] = s;
            }
            for (int i = 0; i < Pool2D; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                flat[i] = s;
            }
            for (int i = 0; i < 2; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.loop = true;
                s.volume = 0f;
                s.priority = 0;
                music[i] = s;
            }
        }

        public void Play3D(AudioClip clip, Vector3 pos, float volume, float pitch, float range)
        {
            // Reuse the oldest source (or one that's free).
            AudioSource s = null;
            for (int i = 0; i < Pool3D; i++)
            {
                var c = world[(nextWorld + i) % Pool3D];
                if (!c.isPlaying) { s = c; break; }
            }
            if (s == null) s = world[nextWorld];
            nextWorld = (nextWorld + 1) % Pool3D;
            s.transform.position = pos;
            s.maxDistance = range;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }

        public void Play2D(AudioClip clip, float volume, float pitch, float delay)
        {
            if (delay > 0f)
            {
                delayed.Add(new Delayed { clip = clip, volume = volume, pitch = pitch, at = Time.unscaledTime + delay });
                return;
            }
            var s = flat[nextFlat];
            nextFlat = (nextFlat + 1) % Pool2D;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }

        void Update()
        {
            for (int i = delayed.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < delayed[i].at) continue;
                var d = delayed[i];
                delayed.RemoveAt(i);
                Play2D(d.clip, d.volume, d.pitch, 0f);
            }

            // The listener sits on the main camera; sounds follow it automatically.
            UpdateMusic();
        }

        void UpdateMusic()
        {
            var match = MatchController.Instance;
            var race = RaceManager.Instance;
            // Menus: lounge loop. In a show: the show loop on loading and results screens, the game loop while playing.
            bool inShow = match != null && match.InMatch && race != null;
            string want = !inShow ? "music_menu" : race.State == RaceManager.Phase.Racing ? "music_game" : "music_show";

            if (want != currentTrack)
            {
                var clip = Sfx.Clip(want);
                if (clip != null)
                {
                    currentTrack = want;
                    activeMusic = 1 - activeMusic;
                    var s = music[activeMusic];
                    s.clip = clip;
                    s.time = 0f;
                    s.Play();
                }
            }

            // Crossfade; duck while paused.
            float target = Sfx.MusicVolume * (MenuUI.Paused ? 0.45f : 1f) * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                var s = music[i];
                float goal = i == activeMusic ? target : 0f;
                s.volume = Mathf.MoveTowards(s.volume, goal, Time.unscaledDeltaTime * 0.4f); // ~1.5 s crossfade
                if (i != activeMusic && s.volume <= 0.001f && s.isPlaying) s.Stop();
            }
        }
    }
}
