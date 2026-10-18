using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Cheap throwaway effects: explosion balls, puffs and tracers.</summary>
    public class Fx : MonoBehaviour
    {
        float age, life, startScale, endScale;
        LineRenderer line;

        public static void Explosion(Vector3 pos, float radius) =>
            Spawn(PrimitiveType.Sphere, pos, radius * 0.3f, radius * 1.4f, 0.25f, Mats.Get("Fx_Explosion", Palette.Explosion));

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
            if (age >= life) Destroy(gameObject);
        }
    }
}
