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
            FireFeel(def, owner, origin, dir);
            switch (def.kind)
            {
                case WeaponKind.Melee: Shove(def, owner, dir); break;
                case WeaponKind.Hitscan: Hitscan(def, owner, origin, dir); break;
                case WeaponKind.Rocket: Rocket.Spawn(def, owner, origin, dir); break;
                case WeaponKind.Shotgun: Shotgun(def, owner, origin, dir, true); break;
            }
        }

        /// <summary>
        /// Replays someone else's shot on this machine: same visuals, no hits
        /// (their machine works out who got hit and tells the victims).
        /// </summary>
        public static void FireCosmetic(WeaponDef def, GameObject owner, Vector3 origin, Vector3 direction)
        {
            Vector3 dir = direction.sqrMagnitude < 0.0001f ? owner.transform.forward : direction.normalized;
            FireFeel(def, owner, origin, dir);
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
                    Trail(def, end);
                    break;
                }
                case WeaponKind.Rocket:
                    Rocket.Spawn(def, owner, origin, dir, true);
                    break;
                case WeaponKind.Shotgun:
                    Shotgun(def, owner, origin, dir, false);
                    break;
            }
        }

        /// <summary>Where the last shot's barrel was (tracers start here so they come out of the gun).</summary>
        static Vector3 muzzle;

        /// <summary>The bang, the muzzle flash, and a little screen kick when it's your own gun.</summary>
        static void FireFeel(WeaponDef def, GameObject owner, Vector3 origin, Vector3 dir)
        {
            var visual = owner != null ? owner.GetComponent<DummyVisual>() : null;
            muzzle = visual != null && def.kind != WeaponKind.Melee ? visual.MuzzlePoint : origin;
            if (Vector3.Distance(muzzle, origin) > 2.5f) muzzle = origin; // tumbling or mid-animation: keep it sane
            switch (def.kind)
            {
                case WeaponKind.Shotgun:
                    if (def == WeaponDef.LeafBlower) break; // the gust puffs are drawn in Shotgun()
                    Fx.Muzzle(muzzle, dir, def.color, 0.55f);
                    Puffs.SmokeBurst(muzzle + dir * 0.4f, 3, 1.2f, 0.3f, 0.35f);
                    break;
                case WeaponKind.Rocket:
                    Fx.Muzzle(muzzle, dir, def.color, 0.4f);
                    Puffs.SmokeBurst(muzzle - dir * 1.1f, 3, 1.5f, 0.35f, 0.4f); // back-blast
                    break;
                case WeaponKind.Hitscan:
                    Fx.Muzzle(muzzle, dir, def.color, def == WeaponDef.LongShot || def == WeaponDef.HandCannon ? 0.45f
                        : def == WeaponDef.SprayNPray ? 0.16f : 0.22f);
                    break;
            }

            string sound = def.kind == WeaponKind.Melee ? "shove"
                         : def == WeaponDef.LeafBlower ? "blower"
                         : def.kind == WeaponKind.Shotgun ? "shotgun"
                         : def == WeaponDef.BigFlush ? "flush"
                         : def.kind == WeaponKind.Rocket ? "rocket"
                         : def == WeaponDef.LongShot ? "sniper"
                         : def == WeaponDef.HandCannon ? "cannon"
                         : def == WeaponDef.BonkRifle ? "rifle"
                         : def == WeaponDef.SprayNPray ? "smg" : "pistol";
            float range = def == WeaponDef.LongShot || def == WeaponDef.HandCannon ? 140f
                        : def.kind == WeaponKind.Shotgun && def != WeaponDef.LeafBlower ? 70f : 45f;
            Sfx.At(sound, origin, def == WeaponDef.LeafBlower ? 0.6f : 1f, 0.07f, range);
            if (def == WeaponDef.BigFlush) Sfx.At("rocket", origin, 0.7f, 0.07f, range);

            var c = owner != null ? owner.GetComponent<Contestant>() : null;
            if (c == null || !c.IsPlayer) return;
            if (def.kind == WeaponKind.Shotgun && def != WeaponDef.LeafBlower) { ThirdPersonCamera.Shake(0.35f); ThirdPersonCamera.Kick(4f); }
            else if (def == WeaponDef.LongShot || def == WeaponDef.HandCannon) { ThirdPersonCamera.Shake(0.3f); ThirdPersonCamera.Kick(3f); }
            else if (def == WeaponDef.BigFlush) { ThirdPersonCamera.Shake(0.4f); ThirdPersonCamera.Kick(5f); }
            else if (def.kind == WeaponKind.Rocket) ThirdPersonCamera.Shake(0.2f);
            else if (def == WeaponDef.SprayNPray || def == WeaponDef.LeafBlower) ThirdPersonCamera.Shake(0.03f);
            else if (def.kind != WeaponKind.Melee) ThirdPersonCamera.Shake(0.08f);
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

        /// <summary>
        /// Boom Stick: everyone inside a short cone gets one big launch (stronger up close).
        /// Pellet tracers are just for show.
        /// </summary>
        static void Shotgun(WeaponDef def, GameObject owner, Vector3 origin, Vector3 dir, bool applyHits)
        {
            if (applyHits)
            {
                var bodies = new List<DentBody>(DentBody.All);
                foreach (var body in bodies)
                {
                    if (body == null || body.gameObject == owner) continue;
                    Vector3 to = body.Center - origin;
                    float d = to.magnitude;
                    if (d > def.range || d < 0.01f) continue;
                    if (Vector3.Angle(dir, to) > def.spread * 1.8f) continue;
                    if (Blocked(origin, body, owner)) continue;
                    float falloff = 1f - 0.65f * (d / def.range);
                    body.Hit((to.normalized + dir) * 0.5f, def.knockback * falloff, def.dent * falloff, owner, def.name, def.minLift);
                }
            }

            if (def.pellets == 0)
            {
                // Leaf Blower: a cone of air puffs and the odd leaf.
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pd = Quaternion.LookRotation(dir) * (Quaternion.Euler(Random.Range(-def.spread, def.spread), Random.Range(-def.spread, def.spread), 0f) * Vector3.forward);
                    Puffs.Smoke(muzzle + pd * 0.3f, pd * Random.Range(10f, 16f), Random.Range(0.12f, 0.22f), 0.35f);
                }
                if (Random.value < 0.3f)
                    Puffs.Spark(muzzle, dir * 9f + Random.insideUnitSphere * 2f, 0.1f, 0.6f, new Color(0.55f, 0.75f, 0.2f));
            }
            for (int i = 0; i < def.pellets; i++)
            {
                Vector3 pd = Quaternion.Euler(Random.Range(-def.spread, def.spread), Random.Range(-def.spread, def.spread), 0f) * Vector3.forward;
                pd = Quaternion.LookRotation(dir) * pd;
                Vector3 end = origin + pd * def.range;
                foreach (var hit in Physics.RaycastAll(origin, pd, def.range, ~0, QueryTriggerInteraction.Ignore))
                {
                    var rb = hit.collider.attachedRigidbody;
                    if (rb != null && rb.gameObject == owner) continue;
                    if ((hit.point - origin).sqrMagnitude < (end - origin).sqrMagnitude) end = hit.point;
                }
                Fx.Tracer(muzzle, end, def.color, 0.05f, 0.08f);
            }
        }

        /// <summary>True if level geometry (not other dummies) is between the shooter and the body.</summary>
        static bool Blocked(Vector3 from, DentBody body, GameObject owner)
        {
            Vector3 to = body.Center - from;
            foreach (var hit in Physics.RaycastAll(from, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<DentBody>() != null) continue;
                return true;
            }
            return false;
        }

        /// <summary>The visible bullet line. The Long Shot leaves a fat glowing streak and a line of smoke.</summary>
        static void Trail(WeaponDef def, Vector3 end)
        {
            if (def == WeaponDef.HandCannon) Fx.Tracer(muzzle, end, def.color, 0.16f, 0.2f);
            else if (def == WeaponDef.SprayNPray) Fx.Tracer(muzzle, end, def.color, 0.05f, 0.06f);
            else if (def == WeaponDef.LongShot)
            {
                Fx.Tracer(muzzle, end, def.color, 0.22f, 0.45f);
                Vector3 d = end - muzzle;
                int n = Mathf.Min(40, Mathf.FloorToInt(d.magnitude / 1.5f));
                for (int i = 1; i <= n; i++)
                    Puffs.Smoke(muzzle + d * (i / (float)(n + 1)), Random.insideUnitSphere * 0.2f + Vector3.up * 0.2f, 0.16f, 0.35f);
            }
            else Fx.Tracer(muzzle, end, def.color);
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
            Trail(def, end);
        }

        /// <summary>Launches every dummy in the radius; stronger near the center. Hits the owner too (rocket jumps).</summary>
        public static void Explode(Vector3 pos, float radius, float knockback, float dent, GameObject owner, string weapon, float minLift = 0.35f,
            bool spareOwner = false, bool fx = true)
        {
            var bodies = new List<DentBody>(DentBody.All);
            foreach (var body in bodies)
            {
                if (body == null || (spareOwner && body.gameObject == owner)) continue;
                Vector3 c = body.Center;
                float d = Vector3.Distance(pos, c);
                if (d > radius) continue;
                float falloff = 1f - 0.6f * (d / radius);
                Vector3 dir = d < 0.05f ? Vector3.up : (c - pos) / d;
                body.Hit(dir, knockback * falloff, dent * falloff, owner, weapon, minLift);
            }
            if (fx) Fx.Explosion(pos, radius);
        }
    }
}
