using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Sticky Mine: sits on the floor, arms after a second, launches whoever walks near it.</summary>
    public class StickyMine : MonoBehaviour
    {
        const float Lifetime = 90f;

        GameObject owner;
        ushort id;
        bool cosmetic;
        float age;
        Renderer lamp;
        Material off, on;
        bool wasArmed;

        public static void Spawn(Contestant c, ushort id, Vector3 pos, bool cosmetic)
        {
            var go = new GameObject("StickyMine");
            go.transform.position = pos;
            // A squat landmine: dark disc, yellow/black rim, little spikes, and a blinking dome on top.
            var dark = Mats.Get("Gun_Dark", new Color(0.16f, 0.16f, 0.18f));
            LowPoly.Make(LowPoly.Cylinder(10), "Base", go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.95f, 0.16f, 0.95f), dark);
            LowPoly.Make(LowPoly.Torus(14, 4, 0.16f), "Rim", go.transform, new Vector3(0f, 0.14f, 0f), new Vector3(1f, 0.6f, 1f),
                Mats.Get("Hazard_Stripe", Palette.Stripe));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                LowPoly.Make(LowPoly.Cone(4), "Prong", go.transform, new Vector3(Mathf.Cos(a) * 0.28f, 0.22f, Mathf.Sin(a) * 0.28f),
                    new Vector3(0.06f, 0.14f, 0.06f), Mats.Get("Gun_Steel", new Color(0.55f, 0.57f, 0.62f)));
            }
            var lampGo = LowPoly.Make(LowPoly.Ico(1), "Lamp", go.transform, new Vector3(0f, 0.22f, 0f), Vector3.one * 0.22f,
                Mats.Get("Hazard_Red", Palette.Hazard));

            var m = go.AddComponent<StickyMine>();
            m.owner = c.gameObject;
            m.id = id;
            m.cosmetic = cosmetic;
            m.lamp = lampGo.GetComponent<Renderer>();
            m.off = Mats.Get("Dummy_Black", Palette.Black);
            m.on = Mats.Unlit("MineLamp", new Color(1f, 0.25f, 0.2f));
            Gadgets.Register(id, go);
        }

        void Update()
        {
            age += Time.deltaTime;
            bool armed = age >= Gadgets.MineArm;
            if (armed && !wasArmed) Sfx.At("mine_arm", transform.position, 1f, 0f, 25f);
            wasArmed = armed;
            if (lamp != null) lamp.sharedMaterial = armed && Mathf.Repeat(age * 2f, 1f) < 0.5f ? on : off;

            if (cosmetic)
            {
                if (age > Lifetime + 2f) { Gadgets.Unregister(id); Destroy(gameObject); }
                return;
            }
            if (age > Lifetime) { Gadgets.Unregister(id); Destroy(gameObject); return; }
            if (!armed) return;

            Vector3 p = transform.position + Vector3.up * 0.6f;
            foreach (var body in DentBody.All)
            {
                if (body == null || body.gameObject == owner) continue;
                if ((body.Center - p).sqrMagnitude > Gadgets.MineTrigger * Gadgets.MineTrigger) continue;
                Gadgets.Detonate(id, transform.position + Vector3.up * 0.3f, Gadgets.MineRadius, Gadgets.MineKnock, Gadgets.MineDent, owner, "Sticky Mine");
                Destroy(gameObject);
                return;
            }
        }
    }
}
