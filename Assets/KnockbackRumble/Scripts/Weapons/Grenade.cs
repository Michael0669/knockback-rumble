using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Bouncer Grenade: a bouncy ball that blows up after a short fuse.</summary>
    public class Grenade : MonoBehaviour
    {
        GameObject owner;
        ushort id;
        bool cosmetic;
        float age;
        Renderer body;
        Material normal, flash;

        public static void Spawn(Contestant c, ushort id, Vector3 pos, Vector3 vel, bool cosmetic)
        {
            var go = Shapes.Make(PrimitiveType.Sphere, "Grenade", null, pos, Vector3.one * 0.45f,
                Mats.Get("Gadget_Grenade", new Color(0.55f, 0.9f, 0.3f)), true);
            var col = go.GetComponent<SphereCollider>();
            col.sharedMaterial = new PhysicsMaterial("Bouncy")
            {
                bounciness = 0.65f, dynamicFriction = 0.4f, staticFriction = 0.4f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
            };
            foreach (var oc in c.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(col, oc);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearVelocity = vel;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var g = go.AddComponent<Grenade>();
            g.owner = c.gameObject;
            g.id = id;
            g.cosmetic = cosmetic;
            g.body = go.GetComponent<Renderer>();
            g.normal = g.body.sharedMaterial;
            g.flash = Mats.Get("Hazard_Warn", Palette.HazardWarn);
            Gadgets.Register(id, go);
        }

        void Update()
        {
            age += Time.deltaTime;
            // Blink faster as the fuse runs out.
            float left = Gadgets.GrenadeFuse - age;
            bool on = left < 0.8f ? Mathf.Repeat(age * 10f, 1f) < 0.5f : Mathf.Repeat(age * 3f, 1f) < 0.5f;
            if (body != null) body.sharedMaterial = on ? flash : normal;

            if (cosmetic)
            {
                // The owner's machine announces the real explosion; this is just a safety net.
                if (age > Gadgets.GrenadeFuse + 2f) { Gadgets.Unregister(id); Destroy(gameObject); }
                return;
            }
            if (age >= Gadgets.GrenadeFuse)
            {
                Gadgets.Detonate(id, transform.position, Gadgets.GrenadeRadius, Gadgets.GrenadeKnock, Gadgets.GrenadeDent, owner, "Bouncer Grenade");
                Destroy(gameObject);
            }
        }
    }
}
