using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A giant boxing glove on a spring arm that punches out of a wall across the walkway.
    /// It shakes for a moment before it fires. Runs on the shared clock so every machine
    /// sees the same punches. The root's local +X points at the walkway.
    /// </summary>
    public class PunchGlove : MonoBehaviour
    {
        public Transform glove;
        public Transform arm;
        public float reach = 4f;
        public float period = 2.6f;
        public float phase;
        public float knockback = 17f;
        public float dent = 14f;
        public float hitRadius = 1.2f;

        readonly HitCooldown cooldown = new HitCooldown();
        readonly HitCooldown bumpCooldown = new HitCooldown();
        Vector3 home;

        const float WindUp = 0.45f, Punch = 0.12f, Hold = 0.35f, Retract = 0.7f;

        void Awake() { if (glove != null) home = glove.localPosition; }

        void FixedUpdate()
        {
            if (glove == null) return;
            float cycle = Mathf.Max(period, WindUp + Punch + Hold + Retract + 0.1f);
            float t = Mathf.Repeat(Net.Time + phase, cycle);
            float rest = cycle - (WindUp + Punch + Hold + Retract);
            float ext;
            bool punching = false;
            Vector3 shake = Vector3.zero;

            if (t < rest) ext = 0f;
            else if (t < rest + WindUp)
            {
                ext = -0.3f * ((t - rest) / WindUp);  // pulls back...
                shake = Random.insideUnitSphere * 0.06f; // ...and trembles: get ready
            }
            else if (t < rest + WindUp + Punch) { ext = Mathf.Lerp(-0.3f, reach, (t - rest - WindUp) / Punch); punching = true; }
            else if (t < rest + WindUp + Punch + Hold) ext = reach;
            else ext = Mathf.Lerp(reach, 0f, (t - rest - WindUp - Punch - Hold) / Retract);

            glove.localPosition = home + new Vector3(ext, 0f, 0f) + shake;
            if (arm != null)
            {
                float len = Mathf.Max(0.1f, ext + 0.4f);
                arm.localPosition = home + new Vector3(len * 0.5f - 0.4f, 0f, 0f);
                arm.localScale = new Vector3(arm.localScale.x, len, arm.localScale.z);
            }
            // The fist is in front of the cuff. While punching, anything near it gets launched;
            // the rest of the time, walking into the glove or the arm just bumps you back a little.
            Vector3 fist = glove.TransformPoint(new Vector3(0.55f, 0f, 0f));
            Vector3 dir = transform.right + Vector3.up * 0.25f;
            Vector3 armStart = transform.position, armEnd = fist;
            foreach (var body in DentBody.All)
            {
                if (body == null) continue;
                Vector3 bc = body.Center;
                float fistDist = (bc - fist).magnitude;
                if (punching)
                {
                    if (fistDist > hitRadius + 0.6f || !cooldown.TryHit(body, 0.8f)) continue;
                    body.Hit(dir, knockback, dent, null, "Punch Wall", 0.3f);
                    Sfx.At("hit_heavy", fist, 1f, 0.1f, 40f);
                }
                else if (ext > 0.5f)
                {
                    float armDist = DistanceToSegment(bc, armStart, armEnd);
                    if (Mathf.Min(fistDist - 0.4f, armDist) > 0.75f || !bumpCooldown.TryHit(body, 0.6f)) continue;
                    Vector3 away = Shapes.Flat(bc - ClosestOnSegment(bc, armStart, armEnd));
                    if (away.sqrMagnitude < 0.01f) away = transform.forward;
                    body.Hit(away.normalized, knockback * 0.35f, dent * 0.3f, null, "Punch Wall", 0.25f);
                }
            }
        }

        static Vector3 ClosestOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return a + ab * t;
        }

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b) => (p - ClosestOnSegment(p, a, b)).magnitude;
    }
}