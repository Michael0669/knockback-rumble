using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A visible rocket that sweeps a small sphere each frame and explodes on contact.</summary>
    public class Rocket : MonoBehaviour
    {
        WeaponDef def;
        GameObject owner;
        Vector3 velocity;
        float life = 4f;

        public static void Spawn(WeaponDef def, GameObject owner, Vector3 position, Vector3 direction)
        {
            var go = Shapes.Make(PrimitiveType.Capsule, "Rocket", null, position, new Vector3(0.22f, 0.35f, 0.22f),
                Mats.Get("Weapon_rocket", def.color), false);
            go.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
            var r = go.AddComponent<Rocket>();
            r.def = def;
            r.owner = owner;
            r.velocity = direction * def.projectileSpeed;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Boom(transform.position); return; }

            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            Vector3 dir = velocity.normalized;

            bool hitSomething = false;
            float best = float.MaxValue;
            foreach (var hit in Physics.SphereCastAll(transform.position, 0.2f, dir, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                var rb = hit.collider.attachedRigidbody;
                if (rb != null && owner != null && rb.gameObject == owner) continue;
                float d = hit.distance <= 0f ? 0f : hit.distance;
                if (d < best) { best = d; hitSomething = true; }
            }

            if (hitSomething) { Boom(transform.position + dir * best); return; }
            transform.position += step;
        }

        void Boom(Vector3 at)
        {
            Weapons.Explode(at, def.radius, def.knockback, def.dent, owner, def.name, def.minLift);
            Destroy(gameObject);
        }
    }
}
