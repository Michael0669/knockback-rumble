using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>All weapon effects: shove, hitscan, rockets and explosions.</summary>
    public static class Weapons
    {
        public static void Fire(WeaponDef def, GameObject owner, Vector3 origin, Vector3 direction)
        {
            Vector3 dir = direction.sqrMagnitude < 0.0001f ? owner.transform.forward : direction.normalized;
            switch (def.kind)
            {
                case WeaponKind.Melee: Shove(def, owner, dir); break;
                case WeaponKind.Hitscan: Hitscan(def, owner, origin, dir); break;
                case WeaponKind.Rocket: Rocket.Spawn(def, owner, origin, dir); break;
            }
        }

        /// <summary>
        /// Replays someone else's shot on this machine: same visuals, no hits
        /// (their machine works out who got hit and tells the victims).
        /// </summary>
        public static void FireCosmetic(WeaponDef def, GameObject owner, Vector3 origin, Vector3 direction)
        {
            Vector3 dir = direction.sqrMagnitude < 0.0001f ? owner.transform.forward : direction.normalized;
            switch (def.kind)
            {
                case WeaponKind.Melee:
                {
                    Vector3 flat = Shapes.Flat(dir).normalized;
                    if (flat.sqrMagnitude < 0.01f) flat = owner.transform.forward;
                    Fx.Puff(owner.transform.position + Vector3.up * 1.0f + flat * def.range, 0.6f, Palette.White);
                    break;
                }
                case WeaponKind.Hitscan:
                {
                    Vector3 end = origin + dir * def.range;
                    float best = float.MaxValue;
                    foreach (var hit in Physics.RaycastAll(origin, dir, def.range, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var rb = hit.collider.attachedRigidbody;
                        if (rb != null && rb.gameObject == owner) continue;
                        if (hit.distance < best) { best = hit.distance; end = hit.point; }
                    }
                    if (best < float.MaxValue) Fx.Puff(end, 0.35f, def.color);
                    Fx.Tracer(origin, end, def.color);
                    break;
                }
                case WeaponKind.Rocket:
                    Rocket.Spawn(def, owner, origin, dir, true);
                    break;
            }
        }

        static void Shove(WeaponDef def, GameObject owner, Vector3 dir)
        {
            Vector3 flat = Shapes.Flat(dir).normalized;
            if (flat.sqrMagnitude < 0.01f) flat = owner.transform.forward;
            Vector3 center = owner.transform.position + Vector3.up * 1.0f + flat * def.range;
            var done = new HashSet<DentBody>();
            foreach (var col in Physics.OverlapSphere(center, def.radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var body = col.GetComponentInParent<DentBody>();
                if (body == null || body.gameObject == owner || !done.Add(body)) continue;
                Vector3 push = Shapes.Flat(body.transform.position - owner.transform.position);
                if (push.sqrMagnitude < 0.01f) push = flat;
                body.Hit(push.normalized, def.knockback, def.dent, owner, def.name, def.minLift);
            }
            Fx.Puff(center, 0.6f, Palette.White);
        }

        static void Hitscan(WeaponDef def, GameObject owner, Vector3 origin, Vector3 dir)
        {
            var hits = Physics.RaycastAll(origin, dir, def.range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Vector3 end = origin + dir * def.range;
            foreach (var hit in hits)
            {
                var rb = hit.collider.attachedRigidbody;
                if (rb != null && rb.gameObject == owner) continue;
                end = hit.point;
                var body = hit.collider.GetComponentInParent<DentBody>();
                if (body != null) body.Hit(dir, def.knockback, def.dent, owner, def.name, def.minLift);
                Fx.Puff(hit.point, 0.35f, def.color);
                break;
            }
            Fx.Tracer(origin, end, def.color);
        }

        /// <summary>Launches every dummy in the radius; stronger near the center. Hits the owner too (rocket jumps).</summary>
        public static void Explode(Vector3 pos, float radius, float knockback, float dent, GameObject owner, string weapon, float minLift = 0.35f)
        {
            var bodies = new List<DentBody>(DentBody.All);
            foreach (var body in bodies)
            {
                if (body == null) continue;
                Vector3 c = body.Center;
                float d = Vector3.Distance(pos, c);
                if (d > radius) continue;
                float falloff = 1f - 0.6f * (d / radius);
                Vector3 dir = d < 0.05f ? Vector3.up : (c - pos) / d;
                body.Hit(dir, knockback * falloff, dent * falloff, owner, weapon, minLift);
            }
            Fx.Explosion(pos, radius);
        }
    }
}
