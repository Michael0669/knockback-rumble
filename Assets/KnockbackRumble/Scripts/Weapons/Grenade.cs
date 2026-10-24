using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Bouncer Grenade (and Stink Bomb): a bouncy ball that blows up after a short fuse.</summary>
    public class Grenade : MonoBehaviour
    {
        /// <summary>Stink Bomb: shorter fuse, bigger, softer blast that piles on damage %.</summary>
        public bool Stink { get; private set; }
        float Fuse => Stink ? Gadgets.StinkFuse : Gadgets.GrenadeFuse;
        GameObject owner;
        ushort id;
        bool cosmetic;
        float age;
        Renderer body;
        Material normal, flash;
        bool lastOn;
        Transform spark;

        public static void Spawn(Contestant c, ushort id, Vector3 pos, Vector3 vel, bool cosmetic, bool stink = false)
        {
            var go = Shapes.Make(PrimitiveType.Sphere, stink ? "StinkBomb" : "Grenade", null, pos, Vector3.one * 0.45f,
                Mats.Get("Gadget_Grenade", new Color(0.55f, 0.9f, 0.3f)), true);
            var col = go.GetComponent<SphereCollider>();
            col.sharedMaterial = new PhysicsMaterial("Bouncy")
            {
                bounciness = stink ? 0.3f : 0.65f, dynamicFriction = 0.4f, staticFriction = 0.4f,
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
            g.Stink = stink;
            if (stink)
            {
                go.GetComponent<Renderer>().enabled = false;
                g.spark = WeaponModels.StinkBomb(go.transform);
                go.transform.Find("StinkModel").localScale = Vector3.one * 1.2f;
                Gadgets.Register(id, go);
                return;
            }
            // Looks like a cartoon bomb with a lit fuse; the band flashes faster as it's about to blow.
            go.GetComponent<Renderer>().enabled = false;
            g.spark = WeaponModels.CartoonBomb(go.transform, new Color(0.55f, 0.9f, 0.3f));
            var band = go.transform.Find("BombModel/Band");
            g.body = band != null ? band.GetComponent<Renderer>() : go.GetComponent<Renderer>();
            g.normal = g.body.sharedMaterial;
            g.flash = Mats.Get("Hazard_Warn", Palette.HazardWarn);
            Gadgets.Register(id, go);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 3f) Sfx.At("bounce", transform.position, 1f, 0.15f, 25f);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (Stink)
            {
                if (spark != null && Random.value < 0.5f)
                    Puffs.Smoke(spark.position, Random.insideUnitSphere * 0.6f + Vector3.up * 0.8f, Random.Range(0.12f, 0.22f), 0.6f);
            }
            else Blink();

            if (cosmetic)
            {
                // The owner's machine announces the real explosion; this is just a safety net.
                if (age > Fuse + 2f) { Gadgets.Unregister(id); Destroy(gameObject); }
                return;
            }
            if (age >= Fuse)
            {
                if (Stink)
                {
                    StinkFx(transform.position);
                    Gadgets.Detonate(id, transform.position, Gadgets.StinkRadius, Gadgets.StinkKnock, Gadgets.StinkDent, owner, "Stink Bomb", false);
                }
                else Gadgets.Detonate(id, transform.position, Gadgets.GrenadeRadius, Gadgets.GrenadeKnock, Gadgets.GrenadeDent, owner, "Bouncer Grenade");
                Destroy(gameObject);
            }
        }

        /// <summary>A big green fart cloud.</summary>
        public static void StinkFx(Vector3 pos)
        {
            Sfx.At("fart", pos, 1f, 0.1f, 60f);
            Fx.Burst(pos + Vector3.up * 0.4f, new Color(0.55f, 0.7f, 0.2f), 30, 7f, 0.5f);
            Fx.Ring(pos + Vector3.up * 0.1f, Gadgets.StinkRadius, new Color(0.55f, 0.75f, 0.2f));
            for (int i = 0; i < 14; i++)
                Puffs.Smoke(pos + Random.insideUnitSphere * 2f + Vector3.up, Random.insideUnitSphere * 1.5f + Vector3.up * 0.5f, Random.Range(0.6f, 1.1f), Random.Range(1.2f, 2.2f));
        }

        void Blink()
        {
            // Blink faster as the fuse runs out.
            float left = Gadgets.GrenadeFuse - age;
            bool on = left < 0.8f ? Mathf.Repeat(age * 10f, 1f) < 0.5f : Mathf.Repeat(age * 3f, 1f) < 0.5f;
            if (body != null) body.sharedMaterial = on ? flash : normal;
            if (on && !lastOn) Sfx.At("beep", transform.position, 1f, 0f, 25f);
            if (spark != null)
                Puffs.Spark(spark.position, Random.onUnitSphere * 1.5f + Vector3.up, 0.06f, 0.25f, new Color(1f, 0.8f, 0.3f));
            lastOn = on;
        }
    }
}
