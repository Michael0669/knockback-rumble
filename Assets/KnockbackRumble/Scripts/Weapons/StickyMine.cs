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
            Shapes.Make(PrimitiveType.Cylinder, "Base", go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.08f, 0.9f),
                Mats.Get("Dummy_Black", Palette.Black), false);
            var lampGo = Shapes.Make(PrimitiveType.Sphere, "Lamp", go.transform, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.25f,
                Mats.Get("Hazard_Red", Palette.Hazard), false);

            var m = go.AddComponent<StickyMine>();
            m.owner = c.gameObject;
            m.id = id;
            m.cosmetic = cosmetic;
            m.lamp = lampGo.GetComponent<Renderer>();
            m.off = Mats.Get("Dummy_Black", Palette.Black);
            m.on = Mats.Get("Hazard_Red", Palette.Hazard);
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
