using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A pendulum that swings across the bridge and launches anyone it touches.</summary>
    public class WreckingBall : MonoBehaviour
    {
        public Transform pivot;      // rotates around its Z axis
        public Transform ball;
        public float ballRadius = 1.6f;
        public float maxAngle = 55f;
        public float period = 2.6f;
        public float phase;
        public float knockback = 18f;
        public float dent = 14f;

        readonly HitCooldown cooldown = new HitCooldown();
        Vector3 lastPos;

        void Start() { if (ball != null) lastPos = ball.position; }

        void FixedUpdate()
        {
            if (pivot == null || ball == null) return;
            float angle = maxAngle * Mathf.Sin(Time.time / period * Mathf.PI * 2f + phase);
            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            Vector3 pos = ball.position;
            Vector3 vel = (pos - lastPos) / Time.fixedDeltaTime;
            lastPos = pos;

            foreach (var body in DentBody.All)
            {
                if (body == null) continue;
                if (Vector3.Distance(body.Center, pos) > ballRadius + 0.6f) continue;
                if (!cooldown.TryHit(body, 0.6f)) continue;
                Vector3 dir = vel.sqrMagnitude > 1f ? vel : body.Center - pos;
                float speedScale = Mathf.Clamp(vel.magnitude / 8f, 0.6f, 1.4f);
                body.Hit(dir, knockback * speedScale, dent, null, "Wrecking Ball", 0.35f);
            }
        }
    }
}
