using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A long sweeper bar spinning at shin height. Jump it or get swatted.</summary>
    public class SpinBar : MonoBehaviour
    {
        public float degreesPerSecond = 75f;
        public float length = 13f;
        public float barHeight = 0.8f;
        public float knockback = 15f;
        public float dent = 12f;
        public Transform bar;

        readonly HitCooldown cooldown = new HitCooldown();
        float baseYaw;

        void Awake() => baseYaw = transform.eulerAngles.y;

        void FixedUpdate()
        {
            // Angle comes from the shared clock so the bar is in the same place on every machine.
            transform.rotation = Quaternion.Euler(0f, baseYaw + degreesPerSecond * Net.Time, 0f);
            Vector3 axis = transform.rotation * Vector3.right;
            Vector3 center = transform.position;
            float half = length * 0.5f;

            foreach (var body in DentBody.All)
            {
                if (body == null) continue;
                Vector3 bp = body.transform.position;
                // Feet must be below the bar's top and not far underneath it
                float barY = center.y + barHeight;
                if (bp.y > barY + 0.25f || bp.y < barY - 2.2f) continue;
                Vector3 local = Shapes.Flat(bp - center);
                float t = Vector3.Dot(local, axis);
                if (Mathf.Abs(t) > half) continue;
                if ((local - axis * t).magnitude > 0.7f) continue;
                if (!cooldown.TryHit(body, 0.5f)) continue;
                Vector3 tangent = Vector3.Cross(Vector3.up, local).normalized * Mathf.Sign(degreesPerSecond);
                body.Hit(tangent, knockback, dent, null, "Spin Bar", 0.4f);
            }
        }
    }
}
