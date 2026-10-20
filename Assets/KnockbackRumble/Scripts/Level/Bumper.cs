using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A post that boings anyone who touches it.</summary>
    public class Bumper : MonoBehaviour
    {
        public float knockback = 11f;
        public float dent = 4f;
        readonly HitCooldown cooldown = new HitCooldown();
        Vector3 baseScale;
        float punch;

        void Awake() => baseScale = transform.localScale;

        void Update()
        {
            if (punch <= 0f) return;
            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 5f);
            float s = 1f + Mathf.Sin(punch * Mathf.PI) * 0.2f;
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y, baseScale.z * s);
        }

        void OnCollisionEnter(Collision collision)
        {
            var body = collision.collider.GetComponentInParent<DentBody>();
            if (body == null || !cooldown.TryHit(body, 0.3f)) return;
            Sfx.At("bumper", transform.position, 1f, 0.1f, 35f);
            punch = 1f;
            Puffs.Spark(transform.position + Vector3.up, Random.onUnitSphere * 5f, 0.15f, 0.3f, new Color(1f, 0.5f, 0.85f));
            Vector3 dir = Shapes.Flat(body.transform.position - transform.position);
            body.Hit(dir, knockback, dent, null, "Bumper", 0.3f);
        }
    }
}
