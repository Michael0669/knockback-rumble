using System;
using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    // =========================================================================
    // Outfits: hats, masks, shirts, pants, shoes and gloves your dummy wears.
    // Bought with wallet cash in CUSTOMIZE. To add an item: add a line to the
    // right list in Cosmetics.All (next free id for that slot, a name, a price),
    // and write its build function below. Ids are per slot; 0 means "nothing".
    // =========================================================================

    public enum CosmeticSlot : byte { Hat = 0, Mask = 1, Shirt = 2, Pants = 3, Shoes = 4, Gloves = 5 }

    /// <summary>Where outfit pieces attach on a dummy, and the body measurements they're built around.</summary>
    public class DummyRig
    {
        public Transform model, head, armL, armR, legL, legR;
        public Material body;

        // Model space
        public const float ChestY = 0.28f, BellyY = 0.05f, PelvisY = -0.1f, HeadY = 0.78f;
        // Limb space (from the shoulder / hip pivot)
        public const float UpperArmY = -0.17f, ForearmY = -0.42f, HandY = -0.6f;
        public const float ThighY = -0.2f, ShinY = -0.48f, FootY = -0.68f;
    }

    /// <summary>Handed to an item's build function: makes parts on the rig and remembers them so they can be removed.</summary>
    public class CosmeticBuilder
    {
        public readonly DummyRig rig;
        readonly List<GameObject> parts;

        public CosmeticBuilder(DummyRig rig, List<GameObject> parts)
        {
            this.rig = rig;
            this.parts = parts;
        }

        public Material Mat(string name, Color c) => Mats.Get("Cos_" + name, c);
        public Material Glow(string name, Color c) => Mats.Unlit("Cos_" + name, c);

        GameObject Track(GameObject go) { parts.Add(go); return go; }

        public GameObject Mesh(Transform bone, Mesh mesh, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) =>
            Track(LowPoly.Make(mesh, "Cosmetic", bone, pos, scale, mat, euler));

        public GameObject Box(Transform bone, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) =>
            Track(Shapes.Make(PrimitiveType.Cube, "Cosmetic", bone, pos, scale, mat, false, euler));

        public GameObject Capsule(Transform bone, Vector3 pos, Vector3 scale, Material mat) =>
            Track(Shapes.Make(PrimitiveType.Capsule, "Cosmetic", bone, pos, scale, mat, false));

        public GameObject Ball(Transform bone, Vector3 pos, Vector3 scale, Material mat) => Mesh(bone, LowPoly.Ico(1), pos, scale, mat);
        public GameObject Cyl(Transform bone, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) => Mesh(bone, LowPoly.Cylinder(12), pos, scale, mat, euler);
        public GameObject Cone(Transform bone, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) => Mesh(bone, LowPoly.Cone(10), pos, scale, mat, euler);
        public GameObject Ring(Transform bone, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) => Mesh(bone, LowPoly.Torus(12, 4, 0.25f), pos, scale, mat, euler);

        // Common shells
        public void TorsoShell(Material m, float puff = 1f)
        {
            Ball(rig.model, new Vector3(0f, DummyRig.ChestY, 0f), new Vector3(0.64f, 0.59f, 0.44f) * puff, m);
            Ball(rig.model, new Vector3(0f, DummyRig.BellyY, 0f), new Vector3(0.54f, 0.39f, 0.38f) * puff, m);
        }

        public void Sleeves(Material m, bool full)
        {
            foreach (var arm in new[] { rig.armL, rig.armR })
            {
                Capsule(arm, new Vector3(0f, DummyRig.UpperArmY + 0.04f, 0f), new Vector3(0.17f, full ? 0.15f : 0.11f, 0.17f), m);
                if (full) Capsule(arm, new Vector3(0f, DummyRig.ForearmY, 0f), new Vector3(0.15f, 0.13f, 0.15f), m);
            }
        }

        public void PantsShell(Material m, bool full)
        {
            Box(rig.model, new Vector3(0f, DummyRig.PelvisY, 0f), new Vector3(0.46f, 0.24f, 0.3f), m);
            foreach (var leg in new[] { rig.legL, rig.legR })
            {
                Capsule(leg, new Vector3(0f, DummyRig.ThighY - (full ? 0f : 0.03f), 0f), new Vector3(0.21f, full ? 0.18f : 0.13f, 0.21f), m);
                if (full) Capsule(leg, new Vector3(0f, DummyRig.ShinY, 0f), new Vector3(0.18f, 0.16f, 0.18f), m);
            }
        }

        public void BothFeet(Action<Transform> make) { make(rig.legL); make(rig.legR); }
        public void BothHands(Action<Transform> make) { make(rig.armL); make(rig.armR); }
    }

    public class CosmeticItem
    {
        public CosmeticSlot slot;
        public byte id;
        public string name;
        public int price;              // wallet cash; 0 = free
        public Color color;            // swatch in the shop
        public Action<CosmeticBuilder> build;
    }

    /// <summary>One item id per slot (0 = nothing).</summary>
    public class Outfit
    {
        public const int SlotCount = 6;
        public byte[] ids = new byte[SlotCount];

        public byte this[CosmeticSlot s] { get => ids[(int)s]; set => ids[(int)s] = value; }

        public Outfit Copy() => new Outfit { ids = (byte[])ids.Clone() };

        public void Write(NetWriter w) { for (int i = 0; i < SlotCount; i++) w.Byte(ids[i]); }

        public static Outfit Read(NetReader r)
        {
            var o = new Outfit();
            for (int i = 0; i < SlotCount; i++) o.ids[i] = r.Byte();
            return o;
        }
    }

    public static class Cosmetics
    {
        public static readonly string[] SlotNames = { "HAT", "MASK", "SHIRT", "PANTS", "SHOES", "GLOVES" };

        static CosmeticItem I(CosmeticSlot s, byte id, string name, int price, Color c, Action<CosmeticBuilder> b) =>
            new CosmeticItem { slot = s, id = id, name = name, price = price, color = c, build = b };

        static readonly Color Black = new Color(0.08f, 0.08f, 0.09f), White = new Color(0.95f, 0.95f, 0.93f);

        public static readonly CosmeticItem[] All =
        {
            // ---------------- Hats ----------------
            I(CosmeticSlot.Hat, 1, "Party Hat", 0, new Color(0.95f, 0.3f, 0.6f), PartyHat),
            I(CosmeticSlot.Hat, 2, "Hard Hat", 500, new Color(1f, 0.8f, 0.1f), HardHat),
            I(CosmeticSlot.Hat, 3, "Traffic Cone", 800, new Color(1f, 0.45f, 0.1f), ConeHat),
            I(CosmeticSlot.Hat, 4, "Propeller Beanie", 1000, new Color(0.3f, 0.6f, 1f), Beanie),
            I(CosmeticSlot.Hat, 5, "Cowboy Hat", 1200, new Color(0.55f, 0.35f, 0.18f), CowboyHat),
            I(CosmeticSlot.Hat, 6, "Top Hat", 1500, Black, TopHat),
            I(CosmeticSlot.Hat, 7, "Viking Helmet", 2000, new Color(0.6f, 0.62f, 0.66f), Viking),
            I(CosmeticSlot.Hat, 8, "Plunger", 2500, new Color(0.8f, 0.1f, 0.12f), PlungerHat),
            I(CosmeticSlot.Hat, 9, "Crown", 5000, new Color(1f, 0.82f, 0.2f), Crown),

            // ---------------- Masks ----------------
            I(CosmeticSlot.Mask, 1, "Shades", 0, Black, Shades),
            I(CosmeticSlot.Mask, 2, "Clown Nose", 250, new Color(0.95f, 0.1f, 0.1f), ClownNose),
            I(CosmeticSlot.Mask, 3, "Big Mustache", 600, new Color(0.35f, 0.2f, 0.1f), Mustache),
            I(CosmeticSlot.Mask, 4, "Gas Mask", 1200, new Color(0.3f, 0.35f, 0.3f), GasMask),
            I(CosmeticSlot.Mask, 5, "Luchador", 1500, new Color(0.2f, 0.4f, 0.95f), Luchador),
            I(CosmeticSlot.Mask, 6, "Hockey Mask", 1800, White, HockeyMask),

            // ---------------- Shirts ----------------
            I(CosmeticSlot.Shirt, 1, "White Tee", 0, White, b => { b.TorsoShell(b.Mat("Tee", White)); b.Sleeves(b.Mat("Tee", White), false); }),
            I(CosmeticSlot.Shirt, 2, "Tank Top", 300, new Color(0.85f, 0.2f, 0.2f), b => b.TorsoShell(b.Mat("Tank", new Color(0.85f, 0.2f, 0.2f)))),
            I(CosmeticSlot.Shirt, 3, "Hi-Vis Vest", 600, new Color(1f, 0.5f, 0.1f), HiVis),
            I(CosmeticSlot.Shirt, 4, "Hawaiian Shirt", 900, new Color(0.15f, 0.7f, 0.65f), Hawaiian),
            I(CosmeticSlot.Shirt, 5, "Tuxedo", 2500, Black, Tuxedo),
            I(CosmeticSlot.Shirt, 6, "Superhero Cape", 3000, new Color(0.85f, 0.1f, 0.15f), Cape),

            // ---------------- Pants ----------------
            I(CosmeticSlot.Pants, 1, "Gym Shorts", 0, new Color(0.25f, 0.3f, 0.75f), b => b.PantsShell(b.Mat("Shorts", new Color(0.25f, 0.3f, 0.75f)), false)),
            I(CosmeticSlot.Pants, 2, "Tighty Whities", 150, White, Briefs),
            I(CosmeticSlot.Pants, 3, "Jeans", 400, new Color(0.2f, 0.32f, 0.55f), Jeans),
            I(CosmeticSlot.Pants, 4, "Track Pants", 600, Black, TrackPants),
            I(CosmeticSlot.Pants, 5, "Kilt", 900, new Color(0.7f, 0.12f, 0.15f), Kilt),

            // ---------------- Shoes ----------------
            I(CosmeticSlot.Shoes, 1, "Sneakers", 0, White, Sneakers),
            I(CosmeticSlot.Shoes, 2, "Flip Flops", 100, new Color(0.2f, 0.7f, 0.9f), FlipFlops),
            I(CosmeticSlot.Shoes, 3, "Work Boots", 500, new Color(0.45f, 0.28f, 0.12f), Boots),
            I(CosmeticSlot.Shoes, 4, "Clown Shoes", 1200, new Color(0.95f, 0.15f, 0.15f), ClownShoes),
            I(CosmeticSlot.Shoes, 5, "Bunny Slippers", 1500, new Color(1f, 0.7f, 0.85f), BunnySlippers),

            // ---------------- Gloves ----------------
            I(CosmeticSlot.Gloves, 1, "Work Gloves", 0, new Color(0.8f, 0.65f, 0.35f), WorkGloves),
            I(CosmeticSlot.Gloves, 2, "Oven Mitts", 500, new Color(0.85f, 0.25f, 0.25f), OvenMitts),
            I(CosmeticSlot.Gloves, 3, "Boxing Gloves", 900, new Color(0.85f, 0.1f, 0.1f), BoxingGloves),
            I(CosmeticSlot.Gloves, 4, "Foam Finger", 1200, new Color(1f, 0.85f, 0.15f), FoamFinger),
        };

        public static CosmeticItem Find(CosmeticSlot slot, byte id)
        {
            if (id == 0) return null;
            foreach (var i in All) if (i.slot == slot && i.id == id) return i;
            return null;
        }

        public static List<CosmeticItem> ForSlot(CosmeticSlot slot)
        {
            var list = new List<CosmeticItem>();
            foreach (var i in All) if (i.slot == slot) list.Add(i);
            return list;
        }

        /// <summary>A random outfit for a bot (bots get to wear anything).</summary>
        public static Outfit RandomOutfit(System.Random rng)
        {
            var o = new Outfit();
            for (int s = 0; s < Outfit.SlotCount; s++)
            {
                if (rng.NextDouble() < 0.3) continue; // leave some slots bare
                var options = ForSlot((CosmeticSlot)s);
                if (options.Count > 0) o.ids[s] = options[rng.Next(options.Count)].id;
            }
            return o;
        }

        // =====================================================================
        // Item models (positions are on the bone: head center, model/torso, shoulder or hip pivots)
        // =====================================================================

        // ---------- Hats ----------

        static void PartyHat(CosmeticBuilder b)
        {
            var h = b.rig.head;
            b.Cone(h, new Vector3(0.02f, 0.42f, 0f), new Vector3(0.3f, 0.42f, 0.3f), b.Mat("PartyPink", new Color(0.95f, 0.3f, 0.6f)), new Vector3(0f, 0f, -10f));
            b.Ring(h, new Vector3(0.01f, 0.33f, 0f), new Vector3(0.24f, 0.1f, 0.24f), b.Mat("PartyYellow", new Color(1f, 0.85f, 0.2f)), new Vector3(0f, 0f, -10f));
            b.Ball(h, new Vector3(0.06f, 0.64f, 0f), Vector3.one * 0.1f, b.Mat("PartyYellow", new Color(1f, 0.85f, 0.2f)));
        }

        static void HardHat(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var y = b.Mat("HardHat", new Color(1f, 0.8f, 0.1f));
            b.Ball(h, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.34f, 0.52f), y);
            b.Cyl(h, new Vector3(0f, 0.1f, 0.04f), new Vector3(0.58f, 0.03f, 0.64f), y);
            b.Box(h, new Vector3(0f, 0.3f, 0f), new Vector3(0.06f, 0.06f, 0.44f), y);
        }

        static void ConeHat(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var o = b.Mat("ConeOrange", new Color(1f, 0.45f, 0.1f));
            b.Box(h, new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.04f, 0.5f), o);
            b.Cone(h, new Vector3(0f, 0.48f, 0f), new Vector3(0.4f, 0.56f, 0.4f), o);
            b.Ring(h, new Vector3(0f, 0.5f, 0f), new Vector3(0.28f, 0.12f, 0.28f), b.Mat("White", White));
        }

        static void Beanie(CosmeticBuilder b)
        {
            var h = b.rig.head;
            b.Ball(h, new Vector3(0f, 0.12f, 0f), new Vector3(0.49f, 0.36f, 0.49f), b.Mat("BeanieBlue", new Color(0.3f, 0.6f, 1f)));
            b.Ring(h, new Vector3(0f, 0.06f, 0f), new Vector3(0.48f, 0.1f, 0.48f), b.Mat("BeanieRed", new Color(0.9f, 0.2f, 0.2f)));
            b.Cyl(h, new Vector3(0f, 0.34f, 0f), new Vector3(0.03f, 0.08f, 0.03f), b.Mat("Steel", new Color(0.6f, 0.6f, 0.65f)));
            // Propeller (spins)
            var prop = b.Box(h, new Vector3(0f, 0.39f, 0f), new Vector3(0.36f, 0.015f, 0.06f), b.Mat("PropYellow", new Color(1f, 0.85f, 0.2f)));
            prop.AddComponent<Spinner>().degreesPerSecond = 900f;
        }

        static void CowboyHat(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var brown = b.Mat("Cowboy", new Color(0.55f, 0.35f, 0.18f));
            b.Cyl(h, new Vector3(0f, 0.17f, 0f), new Vector3(0.78f, 0.03f, 0.64f), brown);
            b.Cyl(h, new Vector3(0f, 0.3f, 0f), new Vector3(0.36f, 0.24f, 0.4f), brown);
            b.Cyl(h, new Vector3(0f, 0.21f, 0f), new Vector3(0.37f, 0.05f, 0.41f), b.Mat("CowboyBand", Black));
            b.Box(h, new Vector3(-0.36f, 0.21f, 0f), new Vector3(0.1f, 0.06f, 0.4f), brown, new Vector3(0f, 0f, -25f));
            b.Box(h, new Vector3(0.36f, 0.21f, 0f), new Vector3(0.1f, 0.06f, 0.4f), brown, new Vector3(0f, 0f, 25f));
        }

        static void TopHat(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var black = b.Mat("TopHat", Black);
            b.Cyl(h, new Vector3(0f, 0.2f, 0f), new Vector3(0.56f, 0.03f, 0.56f), black);
            b.Cyl(h, new Vector3(0f, 0.42f, 0f), new Vector3(0.36f, 0.42f, 0.36f), black);
            b.Cyl(h, new Vector3(0f, 0.27f, 0f), new Vector3(0.37f, 0.07f, 0.37f), b.Mat("TopHatBand", new Color(0.8f, 0.1f, 0.15f)));
        }

        static void Viking(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var steel = b.Mat("Steel", new Color(0.6f, 0.62f, 0.66f));
            var horn = b.Mat("Horn", new Color(0.95f, 0.9f, 0.75f));
            b.Ball(h, new Vector3(0f, 0.12f, 0f), new Vector3(0.5f, 0.38f, 0.5f), steel);
            b.Ring(h, new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.1f, 0.5f), b.Mat("Gold", new Color(1f, 0.8f, 0.2f)));
            b.Box(h, new Vector3(0f, 0.05f, 0.24f), new Vector3(0.05f, 0.18f, 0.04f), steel);
            b.Cone(h, new Vector3(-0.29f, 0.26f, 0f), new Vector3(0.11f, 0.32f, 0.11f), horn, new Vector3(0f, 0f, 50f));
            b.Cone(h, new Vector3(0.29f, 0.26f, 0f), new Vector3(0.11f, 0.32f, 0.11f), horn, new Vector3(0f, 0f, -50f));
        }

        static void PlungerHat(CosmeticBuilder b)
        {
            var h = b.rig.head;
            b.Cone(h, new Vector3(0f, 0.27f, 0f), new Vector3(0.38f, 0.2f, 0.38f), b.Mat("PlungerRed", new Color(0.8f, 0.1f, 0.12f)));
            b.Cyl(h, new Vector3(0f, 0.6f, 0f), new Vector3(0.06f, 0.5f, 0.06f), b.Mat("PlungerWood", new Color(0.6f, 0.4f, 0.2f)));
        }

        static void Crown(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var gold = b.Mat("Gold", new Color(1f, 0.8f, 0.2f));
            b.Cyl(h, new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.12f, 0.4f), gold);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                b.Cone(h, new Vector3(Mathf.Cos(a) * 0.17f, 0.37f, Mathf.Sin(a) * 0.17f), new Vector3(0.08f, 0.14f, 0.08f), gold);
                b.Mesh(h, LowPoly.Ico(0), new Vector3(Mathf.Cos(a) * 0.2f, 0.25f, Mathf.Sin(a) * 0.2f), Vector3.one * 0.05f,
                    b.Glow("Gem" + (i % 3), i % 3 == 0 ? new Color(1f, 0.2f, 0.3f) : i % 3 == 1 ? new Color(0.3f, 0.6f, 1f) : new Color(0.3f, 1f, 0.5f)));
            }
        }

        // ---------- Masks ----------

        static void Shades(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var black = b.Mat("Shades", Black);
            b.Box(h, new Vector3(-0.085f, 0.06f, 0.22f), new Vector3(0.14f, 0.08f, 0.03f), black);
            b.Box(h, new Vector3(0.085f, 0.06f, 0.22f), new Vector3(0.14f, 0.08f, 0.03f), black);
            b.Box(h, new Vector3(0f, 0.08f, 0.22f), new Vector3(0.06f, 0.02f, 0.02f), black);
            b.Box(h, new Vector3(-0.2f, 0.07f, 0.11f), new Vector3(0.02f, 0.02f, 0.22f), black);
            b.Box(h, new Vector3(0.2f, 0.07f, 0.11f), new Vector3(0.02f, 0.02f, 0.22f), black);
        }

        static void ClownNose(CosmeticBuilder b) =>
            b.Ball(b.rig.head, new Vector3(0f, -0.01f, 0.235f), Vector3.one * 0.11f, b.Glow("ClownNose", new Color(1f, 0.15f, 0.15f)));

        static void Mustache(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var brown = b.Mat("Mustache", new Color(0.35f, 0.2f, 0.1f));
            b.Ball(h, new Vector3(-0.06f, -0.04f, 0.21f), new Vector3(0.13f, 0.05f, 0.05f), brown);
            b.Ball(h, new Vector3(0.06f, -0.04f, 0.21f), new Vector3(0.13f, 0.05f, 0.05f), brown);
            b.Ball(h, new Vector3(-0.13f, -0.02f, 0.19f), new Vector3(0.06f, 0.05f, 0.04f), brown);
            b.Ball(h, new Vector3(0.13f, -0.02f, 0.19f), new Vector3(0.06f, 0.05f, 0.04f), brown);
        }

        static void GasMask(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var dark = b.Mat("GasMask", new Color(0.25f, 0.28f, 0.25f));
            b.Ball(h, new Vector3(0f, 0f, 0.07f), new Vector3(0.42f, 0.44f, 0.36f), dark);
            b.Cyl(h, new Vector3(0f, -0.08f, 0.22f), new Vector3(0.14f, 0.12f, 0.14f), dark, new Vector3(90f, 0f, 0f));
            b.Cyl(h, new Vector3(0f, -0.08f, 0.3f), new Vector3(0.12f, 0.06f, 0.12f), b.Mat("GasFilter", new Color(0.55f, 0.5f, 0.3f)), new Vector3(90f, 0f, 0f));
            b.Ring(h, new Vector3(-0.085f, 0.07f, 0.23f), new Vector3(0.12f, 0.1f, 0.12f), b.Mat("Steel", new Color(0.6f, 0.62f, 0.66f)), new Vector3(90f, 0f, 0f));
            b.Ring(h, new Vector3(0.085f, 0.07f, 0.23f), new Vector3(0.12f, 0.1f, 0.12f), b.Mat("Steel", new Color(0.6f, 0.62f, 0.66f)), new Vector3(90f, 0f, 0f));
            b.Mesh(h, LowPoly.Cylinder(10), new Vector3(-0.085f, 0.07f, 0.235f), new Vector3(0.09f, 0.01f, 0.09f), b.Glow("GasLens", new Color(0.5f, 0.9f, 0.7f)), new Vector3(90f, 0f, 0f));
            b.Mesh(h, LowPoly.Cylinder(10), new Vector3(0.085f, 0.07f, 0.235f), new Vector3(0.09f, 0.01f, 0.09f), b.Glow("GasLens", new Color(0.5f, 0.9f, 0.7f)), new Vector3(90f, 0f, 0f));
        }

        static void Luchador(CosmeticBuilder b)
        {
            var h = b.rig.head;
            var blue = b.Mat("Lucha", new Color(0.2f, 0.4f, 0.95f));
            var gold = b.Mat("Gold", new Color(1f, 0.8f, 0.2f));
            var white = b.Mat("White", White);
            b.Ball(h, Vector3.zero, new Vector3(0.465f, 0.5f, 0.465f), blue);
            // Eye and mouth openings, trimmed in gold, with the eyes peeking through
            foreach (float side in new[] { -1f, 1f })
            {
                b.Ball(h, new Vector3(side * 0.085f, 0.06f, 0.195f), new Vector3(0.14f, 0.12f, 0.08f), gold);
                b.Ball(h, new Vector3(side * 0.085f, 0.06f, 0.205f), new Vector3(0.11f, 0.1f, 0.08f), white);
                b.Ball(h, new Vector3(side * 0.08f, 0.055f, 0.24f), new Vector3(0.05f, 0.05f, 0.03f), b.Mat("Black", Black));
            }
            b.Box(h, new Vector3(0f, -0.1f, 0.2f), new Vector3(0.12f, 0.05f, 0.04f), gold);
            b.Box(h, new Vector3(0f, 0.2f, 0.12f), new Vector3(0.04f, 0.12f, 0.2f), gold, new Vector3(-30f, 0f, 0f));
        }

        static void HockeyMask(CosmeticBuilder b)
        {
            var h = b.rig.head;
            b.Ball(h, new Vector3(0f, 0.01f, 0.12f), new Vector3(0.38f, 0.46f, 0.24f), b.Mat("Hockey", White));
            var dark = b.Mat("Black", Black);
            foreach (float side in new[] { -1f, 1f })
                b.Ball(h, new Vector3(side * 0.08f, 0.06f, 0.235f), new Vector3(0.08f, 0.05f, 0.02f), dark);
            for (int i = 0; i < 6; i++)
                b.Mesh(h, LowPoly.Ico(0), new Vector3(-0.06f + (i % 3) * 0.06f, -0.06f - (i / 3) * 0.05f, 0.235f), Vector3.one * 0.02f, dark);
            b.Cone(h, new Vector3(-0.06f, 0.17f, 0.2f), new Vector3(0.04f, 0.06f, 0.02f), b.Mat("Red", new Color(0.85f, 0.1f, 0.1f)), new Vector3(180f, 0f, 0f));
            b.Cone(h, new Vector3(0.06f, 0.17f, 0.2f), new Vector3(0.04f, 0.06f, 0.02f), b.Mat("Red", new Color(0.85f, 0.1f, 0.1f)), new Vector3(180f, 0f, 0f));
        }

        // ---------- Shirts ----------

        static void HiVis(CosmeticBuilder b)
        {
            b.TorsoShell(b.Mat("HiVis", new Color(1f, 0.5f, 0.1f)));
            var silver = b.Glow("Reflective", new Color(0.85f, 0.88f, 0.9f));
            b.Box(b.rig.model, new Vector3(0f, 0.18f, 0f), new Vector3(0.6f, 0.04f, 0.44f), silver);
            b.Box(b.rig.model, new Vector3(0f, 0.02f, 0f), new Vector3(0.54f, 0.04f, 0.4f), silver);
        }

        static void Hawaiian(CosmeticBuilder b)
        {
            var teal = b.Mat("Hawaiian", new Color(0.15f, 0.7f, 0.65f));
            b.TorsoShell(teal);
            b.Sleeves(teal, false);
            Color[] flowers = { new Color(1f, 0.4f, 0.6f), new Color(1f, 0.85f, 0.2f), White };
            var rng = new System.Random(4);
            for (int i = 0; i < 12; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float y = 0.0f + (float)rng.NextDouble() * 0.45f;
                var p = new Vector3(Mathf.Sin(a) * 0.29f, y, Mathf.Cos(a) * 0.2f);
                b.Mesh(b.rig.model, LowPoly.Ico(0), p, Vector3.one * 0.07f, b.Mat("Flower" + (i % 3), flowers[i % 3]));
            }
            b.Box(b.rig.model, new Vector3(0f, 0.5f, 0.12f), new Vector3(0.3f, 0.04f, 0.12f), b.Mat("White", White), new Vector3(20f, 0f, 0f));
        }

        static void Tuxedo(CosmeticBuilder b)
        {
            var black = b.Mat("Tux", Black);
            b.TorsoShell(black);
            b.Sleeves(black, true);
            b.Box(b.rig.model, new Vector3(0f, 0.25f, 0.2f), new Vector3(0.16f, 0.4f, 0.04f), b.Mat("White", White));
            var red = b.Mat("BowTie", new Color(0.8f, 0.1f, 0.15f));
            b.Cone(b.rig.model, new Vector3(-0.05f, 0.47f, 0.22f), new Vector3(0.08f, 0.09f, 0.04f), red, new Vector3(0f, 0f, -90f));
            b.Cone(b.rig.model, new Vector3(0.05f, 0.47f, 0.22f), new Vector3(0.08f, 0.09f, 0.04f), red, new Vector3(0f, 0f, 90f));
            for (int i = 0; i < 3; i++)
                b.Mesh(b.rig.model, LowPoly.Ico(0), new Vector3(0f, 0.36f - i * 0.1f, 0.225f), Vector3.one * 0.03f, b.Mat("Black", Black));
        }

        static void Cape(CosmeticBuilder b)
        {
            var blue = b.Mat("HeroSuit", new Color(0.15f, 0.3f, 0.8f));
            var red = b.Mat("HeroCape", new Color(0.85f, 0.1f, 0.15f));
            b.TorsoShell(blue);
            b.Sleeves(blue, true);
            b.Box(b.rig.model, new Vector3(0f, 0.08f, -0.24f), new Vector3(0.62f, 0.8f, 0.03f), red, new Vector3(-12f, 0f, 0f));
            b.Box(b.rig.model, new Vector3(0f, 0.48f, -0.14f), new Vector3(0.5f, 0.06f, 0.1f), red);
            b.Mesh(b.rig.model, LowPoly.Cone(3), new Vector3(0f, 0.3f, 0.225f), new Vector3(0.18f, 0.16f, 0.03f), b.Mat("Gold", new Color(1f, 0.8f, 0.2f)),
                new Vector3(0f, 0f, 180f));
        }

        // ---------- Pants ----------

        static void Briefs(CosmeticBuilder b)
        {
            b.Box(b.rig.model, new Vector3(0f, DummyRig.PelvisY, 0f), new Vector3(0.46f, 0.23f, 0.3f), b.Mat("White", White));
            b.Box(b.rig.model, new Vector3(0f, -0.005f, 0f), new Vector3(0.47f, 0.04f, 0.31f), b.Mat("Waistband", new Color(0.3f, 0.45f, 0.85f)));
            foreach (var leg in new[] { b.rig.legL, b.rig.legR })
                b.Capsule(leg, new Vector3(0f, -0.06f, 0f), new Vector3(0.21f, 0.06f, 0.21f), b.Mat("White", White));
        }

        static void Jeans(CosmeticBuilder b)
        {
            b.PantsShell(b.Mat("Denim", new Color(0.2f, 0.32f, 0.55f)), true);
            b.Box(b.rig.model, new Vector3(0f, -0.005f, 0f), new Vector3(0.47f, 0.05f, 0.31f), b.Mat("Belt", new Color(0.3f, 0.18f, 0.08f)));
            b.Box(b.rig.model, new Vector3(0f, -0.005f, 0.16f), new Vector3(0.07f, 0.05f, 0.02f), b.Mat("Gold", new Color(1f, 0.8f, 0.2f)));
        }

        static void TrackPants(CosmeticBuilder b)
        {
            b.PantsShell(b.Mat("Track", Black), true);
            var white = b.Mat("White", White);
            foreach (var leg in new[] { b.rig.legL, b.rig.legR })
            {
                float side = leg == b.rig.legL ? -1f : 1f;
                b.Box(leg, new Vector3(side * 0.105f, -0.34f, 0f), new Vector3(0.02f, 0.62f, 0.04f), white);
            }
        }

        static void Kilt(CosmeticBuilder b)
        {
            var red = b.Mat("Kilt", new Color(0.7f, 0.12f, 0.15f));
            b.Box(b.rig.model, new Vector3(0f, DummyRig.PelvisY, 0f), new Vector3(0.46f, 0.24f, 0.3f), red);
            b.Mesh(b.rig.model, LowPoly.Cone(10), new Vector3(0f, -0.24f, 0f), new Vector3(0.62f, 0.34f, 0.46f), red, new Vector3(180f, 0f, 0f));
            var green = b.Mat("KiltStripe", new Color(0.1f, 0.35f, 0.2f));
            b.Ring(b.rig.model, new Vector3(0f, -0.2f, 0f), new Vector3(0.5f, 0.06f, 0.38f), green);
            b.Ring(b.rig.model, new Vector3(0f, -0.3f, 0f), new Vector3(0.58f, 0.06f, 0.44f), green);
            b.Box(b.rig.model, new Vector3(0f, -0.16f, 0.2f), new Vector3(0.1f, 0.12f, 0.04f), b.Mat("Sporran", new Color(0.45f, 0.3f, 0.15f)));
        }

        // ---------- Shoes ----------

        static void Sneakers(CosmeticBuilder b) => b.BothFeet(leg =>
        {
            b.Box(leg, new Vector3(0f, DummyRig.FootY + 0.01f, 0.06f), new Vector3(0.19f, 0.12f, 0.32f), b.Mat("White", White));
            b.Box(leg, new Vector3(0f, DummyRig.FootY - 0.05f, 0.06f), new Vector3(0.2f, 0.03f, 0.33f), b.Mat("Sole", new Color(0.2f, 0.2f, 0.22f)));
            b.Box(leg, new Vector3(0.096f, DummyRig.FootY + 0.02f, 0.04f), new Vector3(0.01f, 0.04f, 0.18f), b.Mat("SneakerRed", new Color(0.9f, 0.2f, 0.2f)), new Vector3(15f, 0f, 0f));
        });

        static void FlipFlops(CosmeticBuilder b) => b.BothFeet(leg =>
        {
            b.Box(leg, new Vector3(0f, DummyRig.FootY - 0.055f, 0.06f), new Vector3(0.19f, 0.03f, 0.32f), b.Mat("FlipFlop", new Color(0.2f, 0.7f, 0.9f)));
            b.Box(leg, new Vector3(0f, DummyRig.FootY + 0.03f, 0.12f), new Vector3(0.17f, 0.02f, 0.03f), b.Mat("FlipStrap", new Color(1f, 0.85f, 0.2f)), new Vector3(0f, 0f, 0f));
        });

        static void Boots(CosmeticBuilder b) => b.BothFeet(leg =>
        {
            var brown = b.Mat("Boot", new Color(0.45f, 0.28f, 0.12f));
            b.Box(leg, new Vector3(0f, DummyRig.FootY + 0.04f, 0.05f), new Vector3(0.2f, 0.18f, 0.33f), brown);
            b.Cyl(leg, new Vector3(0f, DummyRig.FootY + 0.15f, 0f), new Vector3(0.2f, 0.12f, 0.2f), brown);
            b.Box(leg, new Vector3(0f, DummyRig.FootY - 0.05f, 0.05f), new Vector3(0.21f, 0.04f, 0.34f), b.Mat("Sole", new Color(0.2f, 0.2f, 0.22f)));
        });

        static void ClownShoes(CosmeticBuilder b) => b.BothFeet(leg =>
        {
            b.Ball(leg, new Vector3(0f, DummyRig.FootY, 0.16f), new Vector3(0.28f, 0.16f, 0.55f), b.Mat("ClownShoe", new Color(0.95f, 0.15f, 0.15f)));
            b.Ring(leg, new Vector3(0f, DummyRig.FootY + 0.07f, 0f), new Vector3(0.18f, 0.08f, 0.18f), b.Mat("ClownCuff", new Color(1f, 0.85f, 0.2f)));
        });

        static void BunnySlippers(CosmeticBuilder b) => b.BothFeet(leg =>
        {
            var pink = b.Mat("Bunny", new Color(1f, 0.75f, 0.85f));
            b.Ball(leg, new Vector3(0f, DummyRig.FootY + 0.01f, 0.07f), new Vector3(0.24f, 0.17f, 0.36f), pink);
            b.Cone(leg, new Vector3(-0.05f, DummyRig.FootY + 0.15f, 0.12f), new Vector3(0.05f, 0.16f, 0.03f), pink, new Vector3(-15f, 0f, 10f));
            b.Cone(leg, new Vector3(0.05f, DummyRig.FootY + 0.15f, 0.12f), new Vector3(0.05f, 0.16f, 0.03f), pink, new Vector3(-15f, 0f, -10f));
            b.Mesh(leg, LowPoly.Ico(0), new Vector3(-0.04f, DummyRig.FootY + 0.06f, 0.24f), Vector3.one * 0.03f, b.Mat("Black", Black));
            b.Mesh(leg, LowPoly.Ico(0), new Vector3(0.04f, DummyRig.FootY + 0.06f, 0.24f), Vector3.one * 0.03f, b.Mat("Black", Black));
        });

        // ---------- Gloves ----------

        static void WorkGloves(CosmeticBuilder b) => b.BothHands(arm =>
        {
            var tan = b.Mat("WorkGlove", new Color(0.8f, 0.65f, 0.35f));
            b.Ball(arm, new Vector3(0f, DummyRig.HandY, 0f), new Vector3(0.18f, 0.19f, 0.16f), tan);
            b.Cyl(arm, new Vector3(0f, DummyRig.HandY + 0.09f, 0f), new Vector3(0.16f, 0.07f, 0.16f), b.Mat("WorkCuff", new Color(0.3f, 0.45f, 0.75f)));
        });

        static void OvenMitts(CosmeticBuilder b) => b.BothHands(arm =>
        {
            b.Ball(arm, new Vector3(0f, DummyRig.HandY - 0.02f, 0f), new Vector3(0.23f, 0.28f, 0.2f), b.Mat("OvenMitt", new Color(0.85f, 0.25f, 0.25f)));
            b.Ball(arm, new Vector3(0.07f, DummyRig.HandY + 0.01f, 0.04f), new Vector3(0.08f, 0.12f, 0.08f), b.Mat("OvenMitt", new Color(0.85f, 0.25f, 0.25f)));
            b.Cyl(arm, new Vector3(0f, DummyRig.HandY + 0.12f, 0f), new Vector3(0.19f, 0.06f, 0.19f), b.Mat("White", White));
        });

        static void BoxingGloves(CosmeticBuilder b) => b.BothHands(arm =>
        {
            b.Ball(arm, new Vector3(0f, DummyRig.HandY - 0.03f, 0.02f), new Vector3(0.27f, 0.3f, 0.29f), b.Mat("BoxGlove", new Color(0.85f, 0.1f, 0.1f)));
            b.Cyl(arm, new Vector3(0f, DummyRig.HandY + 0.12f, 0f), new Vector3(0.19f, 0.08f, 0.19f), b.Mat("White", White));
        });

        static void FoamFinger(CosmeticBuilder b)
        {
            var yellow = b.Mat("Foam", new Color(1f, 0.85f, 0.15f));
            var arm = b.rig.armL; // the off hand, so it's not under the gun
            b.Box(arm, new Vector3(0f, DummyRig.HandY - 0.06f, 0f), new Vector3(0.24f, 0.26f, 0.1f), yellow);
            b.Box(arm, new Vector3(0.03f, DummyRig.HandY - 0.32f, 0f), new Vector3(0.07f, 0.3f, 0.08f), yellow);
            b.Box(arm, new Vector3(-0.13f, DummyRig.HandY - 0.06f, 0f), new Vector3(0.06f, 0.12f, 0.08f), yellow, new Vector3(0f, 0f, 30f));
            b.Ball(b.rig.armR, new Vector3(0f, DummyRig.HandY, 0f), new Vector3(0.18f, 0.19f, 0.16f), yellow);
        }
    }
}
