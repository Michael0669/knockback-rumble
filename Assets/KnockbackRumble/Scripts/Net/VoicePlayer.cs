using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Plays one person's incoming voice through a streaming AudioClip fed from a small buffer.</summary>
    public class VoicePlayer : MonoBehaviour
    {
        AudioSource source;
        float[] ring;
        int readPos, writePos, count, startThreshold;
        bool buffering = true;
        readonly object gate = new object();
        Transform follow;

        public void Init(int sampleRate)
        {
            ring = new float[sampleRate * 2];
            startThreshold = sampleRate / 12; // ~80 ms cushion against network hiccups

            source = gameObject.AddComponent<AudioSource>();
            source.clip = AudioClip.Create(name, sampleRate, 1, sampleRate, true, OnAudioRead);
            source.loop = true;
            source.spatialBlend = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = 35f;
            source.dopplerLevel = 0f;
            source.priority = 10;
            source.Play();
        }

        /// <summary>Attach the voice to a dummy (3D) or null for everywhere (2D).</summary>
        public void Follow(Transform target)
        {
            follow = target;
            if (source != null) source.spatialBlend = target != null ? 1f : 0f;
        }

        void LateUpdate()
        {
            if (follow != null) transform.position = follow.position + Vector3.up * 1.6f;
        }

        /// <summary>16-bit little-endian mono PCM from Steam's decoder.</summary>
        public void Push(byte[] pcm16, int bytes)
        {
            lock (gate)
            {
                for (int i = 0; i + 1 < bytes; i += 2)
                {
                    short s = (short)(pcm16[i] | (pcm16[i + 1] << 8));
                    ring[writePos] = s / 32768f;
                    writePos = (writePos + 1) % ring.Length;
                    if (count < ring.Length) count++;
                    else readPos = (readPos + 1) % ring.Length; // overflow: drop the oldest audio
                }
            }
        }

        // Called on Unity's audio thread.
        void OnAudioRead(float[] data)
        {
            lock (gate)
            {
                if (buffering && count >= startThreshold) buffering = false;
                for (int i = 0; i < data.Length; i++)
                {
                    if (buffering || count == 0)
                    {
                        data[i] = 0f;
                        if (count == 0) buffering = true;
                        continue;
                    }
                    data[i] = ring[readPos];
                    readPos = (readPos + 1) % ring.Length;
                    count--;
                }
            }
        }
    }
}
