using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A point light that pops on and fades out (muzzle flashes, explosions).</summary>
    public class FlashLight : MonoBehaviour
    {
        Light lightComp;
        float life, age, peak;

        public static void Spawn(Vector3 pos, Color color, float range, float intensity, float life)
        {
            var go = new GameObject("Flash");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            var f = go.AddComponent<FlashLight>();
            f.lightComp = l;
            f.life = life;
            f.peak = intensity;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / life;
            if (t >= 1f) { Destroy(gameObject); return; }
            lightComp.intensity = peak * (1f - t) * (1f - t);
        }
    }
}
