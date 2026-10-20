using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Makes a grandstand's fans bounce in their seats. Call CrowdBob.Cheer() on big moments
    /// (knock-offs, finishes, explosions) and every crowd jumps up for a couple of seconds.
    /// </summary>
    public class CrowdBob : MonoBehaviour
    {
        static float cheerUntil = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cheerUntil = -99f;

        public static void Cheer(float seconds = 2f) => cheerUntil = Mathf.Max(cheerUntil, Time.time + seconds);

        readonly List<Transform> fans = new List<Transform>();
        readonly List<float> baseY = new List<float>();
        readonly List<float> phase = new List<float>();
        float excitement;

        void Start()
        {
            foreach (Transform t in transform)
            {
                fans.Add(t);
                baseY.Add(t.localPosition.y);
                phase.Add(Random.value * 10f);
            }
        }

        void Update()
        {
            bool cheering = Time.time < cheerUntil;
            excitement = Mathf.MoveTowards(excitement, cheering ? 1f : 0f, Time.deltaTime * (cheering ? 4f : 1f));
            float speed = Mathf.Lerp(3f, 11f, excitement), height = Mathf.Lerp(0.05f, 0.45f, excitement);
            float time = Time.time;
            for (int i = 0; i < fans.Count; i++)
            {
                var t = fans[i];
                if (t == null) continue;
                var p = t.localPosition;
                p.y = baseY[i] + Mathf.Abs(Mathf.Sin(time * speed + phase[i])) * height;
                t.localPosition = p;
            }
        }
    }
}
