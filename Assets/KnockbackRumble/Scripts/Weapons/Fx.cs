using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Cheap throwaway effects: explosion balls, debris bursts, puffs and tracers.</summary>
    public class Fx : MonoBehaviour
    {
        float age, life, startScale, endScale;
        LineRenderer line;
        Vector3 velocity;
        bool debris;

        /// <summary>Every explosion in the game goes through here (sound, shake, fireball, debris).</summary>
        public static void Explosion(Vector3 pos, float radius)
        {
            Spawn(PrimitiveType.Sphere, pos, radius * 0.3f, radius * 1.4f, 0.25f, Mats.Get("Fx_Explosion", Palette.Explosion));
            Spawn(PrimitiveType.Sphere, pos, radius * 0.2f, radius * 0.9f, 0.4f, Mats.Get("Fx_Smoke", new Color(0.25f, 0.23f, 0.22f)));
            Burst(pos, Palette.Explosion, 10, 7f + radius * 1.5f, 0.35f);
            Sfx.At("explosion", pos, Mathf.Clamp(radius / 4f, 0.6f, 1.3f), 0.1f, 90f);
            ThirdPersonCamera.ShakeAt(pos, 0.3f + radius * 0.06f, radius * 6f);
        }

        /// <summary>Chunks flying out of a point (big hits, explosions).</summary>
        public static void Burst(Vector3 pos, Color color, int count, float speed, float size)
        {
            var mat = Mats.Unlit("FxBurst_" + ColorUtility.ToHtmlStringRGB(color), color);
            for (int i = 0; i < count; i++)
            {
                var go = Shapes.Make(PrimitiveType.Cube, "Debris", null, pos, Vector3.one * size, mat, false,
                    new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f));
                var fx = go.AddComponent<Fx>();
                fx.life = Random.Range(0.35f, 0.6f);
                fx.startScale = size;
                fx.endScale = 0f;
                fx.debris = true;
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                fx.velocity = dir.normalized * speed * Random.Range(0.6f, 1.1f);
            }
        }

        public static void Puff(Vector3 pos, float size, Color color) =>
            Spawn(PrimitiveType.Sphere, pos, size * 0.4f, size, 0.12f, Mats.Get("Fx_" + ColorUtility.ToHtmlStringRGB(color), color));

        static void Spawn(PrimitiveType type, Vector3 pos, float from, float to, float life, Material mat)
        {
            var go = Shapes.Make(type, "Fx", null, pos, Vector3.one * from, mat, false);
            var fx = go.AddComponent<Fx>();
            fx.life = life;
            fx.startScale = from;
            fx.endScale = to;
        }

        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.07f;
            lr.endWidth = 0.03f;
            lr.sharedMaterial = Mats.Unlit("Tracer", Color.white);
            lr.startColor = color;
            lr.endColor = color;
            var fx = go.AddComponent<Fx>();
            fx.life = 0.07f;
            fx.line = lr;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            if (line == null) transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
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
