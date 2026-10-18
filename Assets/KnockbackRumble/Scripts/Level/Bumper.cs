using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A post that boings anyone who touches it.</summary>
    public class Bumper : MonoBehaviour
    {
        public float knockback = 11f;
        public float dent = 4f;
        readonly HitCooldown cooldown = new HitCooldown();

        void OnCollisionEnter(Collision collision)
        {
            var body = collision.collider.GetComponentInParent<DentBody>();
            if (body == null || !cooldown.TryHit(body, 0.3f)) return;
            Vector3 dir = Shapes.Flat(body.transform.position - transform.position);
            body.Hit(dir, knockback, dent, null, "Bumper", 0.3f);
        }
    }
}
