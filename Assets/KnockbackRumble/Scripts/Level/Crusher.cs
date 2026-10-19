using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A block that waits up high, flashes orange, slams down, then rises.
    /// Anyone underneath on the slam gets squirted out sideways.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Crusher : MonoBehaviour
    {
        public float upY = 6f;
        public float downY = 1f;
        public Vector3 size = new Vector3(6.5f, 2f, 4f);
        public float phase;
        public float waitUp = 1.4f, slam = 0.18f, waitDown = 0.8f, rise = 1.0f, warnTime = 0.5f;
        public float knockback = 20f;
        public float dent = 22f;
        public Renderer blockRenderer;

        Rigidbody rb;
        Material normalMat, warnMat;
        readonly HitCooldown cooldown = new HitCooldown();

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            normalMat = Mats.Get("Hazard_Red", Palette.Hazard);
            warnMat = Mats.Get("Hazard_Warn", Palette.HazardWarn);
        }

        void FixedUpdate()
        {
            float cycle = waitUp + slam + waitDown + rise;
            float t = Mathf.Repeat(Net.Time + phase, cycle); // shared clock: same timing on every machine
            float y;
            bool slamming = false, warning = false;

            if (t < waitUp)
            {
                y = upY;
                warning = t > waitUp - warnTime;
            }
            else if (t < waitUp + slam)
            {
                y = Mathf.Lerp(upY, downY, (t - waitUp) / slam);
                slamming = true;
            }
            else if (t < waitUp + slam + waitDown)
            {
                y = downY;
            }
            else
            {
                y = Mathf.Lerp(downY, upY, (t - waitUp - slam - waitDown) / rise);
            }

            Vector3 p = transform.position;
            rb.MovePosition(new Vector3(p.x, y, p.z));

            if (blockRenderer != null) blockRenderer.sharedMaterial = warning ? warnMat : normalMat;
            if (slamming) Squish(y);
        }

        void Squish(float y)
        {
            float bottom = y - size.y * 0.5f;
            float floor = downY - size.y * 0.5f;
            Vector3 c = transform.position;
            foreach (var body in DentBody.All)
            {
                if (body == null) continue;
                Vector3 bp = body.transform.position;
                if (Mathf.Abs(bp.x - c.x) > size.x * 0.5f + 0.3f) continue;
                if (Mathf.Abs(bp.z - c.z) > size.z * 0.5f + 0.3f) continue;
                if (bp.y < floor - 0.5f || bp.y > bottom) continue;
                if (!cooldown.TryHit(body, 1f)) continue;
                float side = bp.x - c.x;
                if (Mathf.Abs(side) < 0.05f) side = Random.value < 0.5f ? -1f : 1f;
                body.Hit(new Vector3(Mathf.Sign(side), 0f, 0f), knockback, dent, null, "Crusher", 0.45f);
            }
        }
    }
}
