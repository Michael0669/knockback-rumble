using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A rocket that sweeps a small sphere each frame and explodes on contact. Spins as it
    /// flies, with a flame at the nozzle and a thick trail of low-poly smoke.
    /// </summary>
    public class Rocket : MonoBehaviour
    {
        WeaponDef def;
        GameObject owner;
        bool cosmetic;   // someone else's rocket: explodes visually, hits are handled on their machine
        Vector3 velocity;
        float life = 4f;
        float trailTimer;
        Transform model;

        public static void Spawn(WeaponDef def, GameObject owner, Vector3 position, Vector3 direction, bool cosmetic = false)
        {
            var go = new GameObject("Rocket");
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            model.localScale = Vector3.one * 1.5f;
            WeaponModels.Rocket(model, def.color);
            var r = go.AddComponent<Rocket>();
            r.model = model;
            r.def = def;
            r.owner = owner;
            r.velocity = direction * def.projectileSpeed;
            r.cosmetic = cosmetic;
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

            // Spin, flame and smoke trail
            if (model != null) model.Rotate(0f, 0f, 540f * dt, Space.Self);
            Vector3 tail = transform.position - dir * 0.55f;
            Puffs.Fire(tail, -dir * 3f + Random.insideUnitSphere * 0.5f, Random.Range(0.18f, 0.3f), 0.07f);
            trailTimer -= dt;
            while (trailTimer <= 0f)
            {
                trailTimer += 0.025f;
                Puffs.Smoke(tail - dir * Random.Range(0f, 0.4f), Random.insideUnitSphere * 0.4f + Vector3.up * 0.3f,
                    Random.Range(0.25f, 0.4f), Random.Range(0.3f, 0.5f));
            }
        }

        void Boom(Vector3 at)
        {
            if (cosmetic) Fx.Explosion(at, def.radius);
            else Weapons.Explode(at, def.radius, def.knockback, def.dent, owner, def.name, def.minLift);
            Destroy(gameObject);
        }
    }
}
