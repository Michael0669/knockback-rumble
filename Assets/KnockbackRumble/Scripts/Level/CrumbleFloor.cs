using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Crumble Plaza's floor: a grid of tiles that flash, then drop out one by one, from
    /// the outside in (with some randomness). Every machine builds the same schedule from
    /// the round's seed and runs it on the shared clock, so the floor falls identically
    /// for everyone without sending a single message.
    /// </summary>
    public class CrumbleFloor : MonoBehaviour
    {
        [Tooltip("Filled in by the level builder")]
        public Transform[] tiles;
        public float warnTime = 1.6f;
        [Tooltip("Seconds after GO before the first tile goes")]
        public float firstFall = 8f;
        [Tooltip("Share of the event's length by which the last tile has fallen")]
        [Range(0.5f, 1f)] public float lastFallShare = 0.92f;

        Vector3[] home;
        Renderer[] rends;
        Collider[] cols;
        float[] fallAt;
        byte[] state;   // 0 solid, 1 warning (flash on), 2 warning (flash off), 3 falling, 4 gone
        Material solid, warn;
        bool running, cached;
        float start, nextCrackSound, nextFallSound;

        static bool NearCamera(Vector3 p, float range)
        {
            var cam = Camera.main;
            return cam != null && (cam.transform.position - p).sqrMagnitude < range * range;
        }

        void Cache()
        {
            if (cached || tiles == null) return;
            cached = true;
            int n = tiles.Length;
            home = new Vector3[n];
            rends = new Renderer[n];
            cols = new Collider[n];
            fallAt = new float[n];
            state = new byte[n];
            for (int i = 0; i < n; i++)
            {
                if (tiles[i] == null) continue;
                home[i] = tiles[i].position;
                rends[i] = tiles[i].GetComponent<Renderer>();
                cols[i] = tiles[i].GetComponent<Collider>();
            }
            solid = rends.Length > 0 && rends[0] != null ? rends[0].sharedMaterial : Mats.Get("Floor", Palette.Floor);
            warn = Mats.Get("Hazard_Warn", Palette.HazardWarn);
        }

        /// <summary>Start the schedule. startTime is the shared clock time of GO.</summary>
        public void Begin(int seed, float startTime, float duration)
        {
            Cache();
            if (tiles == null || tiles.Length == 0) return;
            ResetTiles();
            start = startTime;
            running = true;

            // Score each tile: outer tiles and unlucky tiles go first.
            int n = tiles.Length;
            var rng = new System.Random(seed);
            Vector3 center = Vector3.zero;
            for (int i = 0; i < n; i++) center += home[i];
            center /= n;
            float maxDist = 0.01f;
            for (int i = 0; i < n; i++) maxDist = Mathf.Max(maxDist, Shapes.Flat(home[i] - center).magnitude);

            var score = new float[n];
            var order = new int[n];
            for (int i = 0; i < n; i++)
            {
                float edge = Shapes.Flat(home[i] - center).magnitude / maxDist; // 0 middle .. 1 corner
                score[i] = (1f - edge) * 0.65f + (float)rng.NextDouble() * 0.35f;
                order[i] = i;
            }
            System.Array.Sort(order, (a, b) => score[a] != score[b] ? score[a].CompareTo(score[b]) : a.CompareTo(b));

            float last = Mathf.Max(firstFall + 1f, duration * lastFallShare);
            for (int k = 0; k < n; k++)
            {
                float u = n > 1 ? (float)k / (n - 1) : 1f;
                // Slow at first, faster later.
                fallAt[order[k]] = Mathf.Lerp(firstFall, last, Mathf.Sqrt(u));
            }
        }

        /// <summary>Put every tile back.</summary>
        public void ResetTiles()
        {
            Cache();
            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                tiles[i].position = home[i];
                if (cols[i] != null) cols[i].enabled = true;
                if (rends[i] != null) { rends[i].enabled = true; rends[i].sharedMaterial = solid; }
                state[i] = 0;
            }
        }

        public void Stop()
        {
            running = false;
            ResetTiles();
        }

        void Update()
        {
            if (!running || tiles == null) return;
            float t = Net.Time - start;
            bool flashOn = Mathf.Repeat(t * 6f, 1f) < 0.5f;

            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null || state[i] == 4) continue;
                float f = fallAt[i];

                if (t < f - warnTime)
                {
                    continue; // still solid
                }
                if (t < f)
                {
                    if (state[i] == 0 && Time.time >= nextCrackSound && NearCamera(home[i], 16f))
                    {
                        nextCrackSound = Time.time + 0.15f;
                        Sfx.At("tile_crack", home[i], 1f, 0.15f, 25f);
                    }
                    byte s = flashOn ? (byte)1 : (byte)2;
                    if (state[i] != s && rends[i] != null) rends[i].sharedMaterial = flashOn ? warn : solid;
                    state[i] = s;
                    continue;
                }

                // Falling
                if (state[i] < 3)
                {
                    state[i] = 3;
                    if (cols[i] != null) cols[i].enabled = false;
                    if (Time.time >= nextFallSound && NearCamera(home[i], 22f))
                    {
                        nextFallSound = Time.time + 0.2f;
                        Sfx.At("tile_fall", home[i], 1f, 0.15f, 35f);
                    }
                    if (rends[i] != null) rends[i].sharedMaterial = warn;
                }
                float drop = t - f;
                tile.position = home[i] + Vector3.down * (4.9f * drop * drop);
                if (drop > 2.5f)
                {
                    state[i] = 4;
                    if (rends[i] != null) rends[i].enabled = false;
                }
            }
        }
    }
}
