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
            if (!punching) return;

            Vector3 c = glove.position;
            Vector3 dir = transform.right + Vector3.up * 0.25f;
            foreach (var body in DentBody.All)
            {
                if (body == null) continue;
                if ((body.Center - c).sqrMagnitude > hitRadius * hitRadius * 2.5f) continue;
                if (!cooldown.TryHit(body, 0.8f)) continue;
                body.Hit(dir, knockback, dent, null, "Punch Wall", 0.3f);
                Sfx.At("hit_heavy", c, 1f, 0.1f, 40f);
            }
        }
    }
}
