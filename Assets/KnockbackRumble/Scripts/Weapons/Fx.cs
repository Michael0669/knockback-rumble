using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Throwaway effects: explosions, muzzle flashes, tracers, debris, shock rings.
    /// Smoke, fire and sparks come from Puffs (low-poly particles).
    /// </summary>
    public class Fx : MonoBehaviour
    {
        float age, life, startScale, endScale, startWidth;
        LineRenderer line;
        Vector3 velocity;
        bool debris, ring;
        Color lineColor;

        /// <summary>Every explosion in the game goes through here (sound, shake, fireball, smoke, sparks, debris).</summary>
        public static void Explosion(Vector3 pos, float radius)
        {
            // Bright core, then a fireball of tumbling low-poly flames
            Spawn(LowPoly.Ico(1), pos, radius * 0.4f, radius * 1.1f, 0.12f, Mats.Unlit("FxCore", new Color(1f, 0.95f, 0.75f)));
            for (int i = 0; i < 16; i++)
            {
                Vector3 v = Random.insideUnitSphere * radius * 2.6f + Vector3.up * radius * 0.6f;
                Puffs.Fire(pos + Random.insideUnitSphere * radius * 0.25f, v, radius * Random.Range(0.35f, 0.65f), Random.Range(0.35f, 0.65f));
            }
            Puffs.SmokeBurst(pos + Vector3.up * 0.3f, 8, radius * 1.1f, radius * 0.5f, 0.85f, true);
            Puffs.SmokeBurst(pos, 4, radius * 0.8f, radius * 0.4f, 0.55f);
            for (int i = 0; i < 14; i++)
            {
                Vector3 v = Random.onUnitSphere * radius * Random.Range(3f, 6f);
                v.y = Mathf.Abs(v.y) + 2f;
                Puffs.Spark(pos, v, Random.Range(0.08f, 0.16f), Random.Range(0.4f, 0.8f), new Color(1f, 0.8f, 0.3f));
            }
            Burst(pos, new Color(0.25f, 0.22f, 0.2f), 6, 7f + radius * 1.5f, 0.3f);
            Ring(pos + Vector3.up * 0.15f, radius * 2.2f, new Color(1f, 0.85f, 0.55f));
            FlashLight.Spawn(pos + Vector3.up, new Color(1f, 0.6f, 0.25f), radius * 4f, 6f, 0.25f);

            Sfx.At("explosion", pos, Mathf.Clamp(radius / 4f, 0.6f, 1.3f), 0.1f, 90f);
            CrowdBob.Cheer(1.2f);
            ThirdPersonCamera.ShakeAt(pos, 0.3f + radius * 0.06f, radius * 6f);
        }

        /// <summary>A flash at the end of a gun barrel.</summary>
        public static void Muzzle(Vector3 pos, Vector3 dir, Color color, float size)
        {
            for (int i = 0; i < 5; i++)
                Puffs.Fire(pos + dir * size * 0.3f * i, dir * Random.Range(2f, 6f) + Random.insideUnitSphere, size * Random.Range(0.5f, 0.9f), 0.08f);
            Puffs.Smoke(pos + dir * 0.2f, dir * 1.5f + Vector3.up * 0.5f, size * 0.6f, 0.25f);
            FlashLight.Spawn(pos, Color.Lerp(color, new Color(1f, 0.8f, 0.4f), 0.6f), 6f * size + 3f, 3f, 0.08f);
        }

        /// <summary>Chunks flying out of a point (big hits, explosions).</summary>
        public static void Burst(Vector3 pos, Color color, int count, float speed, float size)
        {
            var mat = Mats.Get("FxBurst_" + ColorUtility.ToHtmlStringRGB(color), color);
            for (int i = 0; i < count; i++)
            {
                var go = LowPoly.Make(LowPoly.Rock(i % 3), "Debris", null, pos, Vector3.one * size, mat,
                    new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f));
                var fx = go.AddComponent<Fx>();
                fx.life = Random.Range(0.4f, 0.7f);
                fx.startScale = size;
                fx.endScale = 0f;
                fx.debris = true;
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                fx.velocity = dir.normalized * speed * Random.Range(0.6f, 1.1f);
            }
        }

        /// <summary>A flat shock ring that races outward along the ground.</summary>
        public static void Ring(Vector3 pos, float radius, Color color)
        {
            var go = LowPoly.Make(LowPoly.Torus(20, 4, 0.12f), "ShockRing", null, pos, Vector3.one * 0.3f, Mats.Unlit("FxRing", color));
            var fx = go.AddComponent<Fx>();
            fx.life = 0.3f;
            fx.startScale = radius * 0.2f;
            fx.endScale = radius * 2f;
            fx.ring = true;
        }

        /// <summary>A small impact puff (bullet hits, shoves).</summary>
        public static void Puff(Vector3 pos, float size, Color color)
        {
            for (int i = 0; i < 4; i++)
                Puffs.Spark(pos, Random.onUnitSphere * 4f + Vector3.up * 2f, size * 0.25f, Random.Range(0.2f, 0.4f), color);
            Puffs.Smoke(pos, Random.insideUnitSphere * 0.5f + Vector3.up * 0.4f, size * 0.5f, 0.25f);
        }

        static void Spawn(Mesh mesh, Vector3 pos, float from, float to, float life, Material mat)
        {
            var go = LowPoly.Make(mesh, "Fx", null, pos, Vector3.one * from, mat);
            var fx = go.AddComponent<Fx>();
            fx.life = life;
            fx.startScale = from;
            fx.endScale = to;
        }

        public static void Tracer(Vector3 from, Vector3 to, Color color, float width = 0.09f, float life = 0.1f)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = width * 0.6f;
            lr.endWidth = width;
            lr.numCapVertices = 2;
            lr.sharedMaterial = Mats.Unlit("Tracer", Color.white);
            lr.startColor = color;
            lr.endColor = Color.Lerp(color, Color.white, 0.5f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fx = go.AddComponent<Fx>();
            fx.life = life;
            fx.line = lr;
            fx.startWidth = width;
            fx.lineColor = color;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            if (line != null)
            {
                // Tracers thin out and fade.
                float k = 1f - t;
                line.startWidth = startWidth * 0.6f * k;
                line.endWidth = startWidth * k;
                var c = lineColor;
                c.a = k;
                line.startColor = c;
                line.endColor = new Color(1f, 1f, 1f, k);
            }
            else if (ring)
            {
                float s = Mathf.Lerp(startScale, endScale, 1f - (1f - t) * (1f - t));
                transform.localScale = new Vector3(s, 0.6f * (1f - t) + 0.1f, s);
            }
            else
            {
                transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            }
            if (debris)
            {
                velocity += Vector3.down * 25f * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(400f * Time.deltaTime, 250f * Time.deltaTime, 0f);
            }
            if (age >= life) Destroy(gameObject);
        }
    }
}
