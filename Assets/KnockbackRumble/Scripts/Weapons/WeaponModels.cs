using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Low-poly models for the guns your dummy holds. Each model points along +Z (barrel
    /// forward), +Y up, origin at the grip, sized in meters. Built from primitives and
    /// LowPoly meshes, no colliders.
    /// </summary>
    public static class WeaponModels
    {
        static Material M(string name, Color c) => Mats.Get(name, c);
        static Material Dark => M("Gun_Dark", new Color(0.16f, 0.16f, 0.18f));
        static Material Steel => M("Gun_Steel", new Color(0.55f, 0.57f, 0.62f));
        static Material Wood => M("Gun_Wood", new Color(0.5f, 0.3f, 0.15f));
        static Material Olive => M("Gun_Olive", new Color(0.36f, 0.42f, 0.24f));
        static Material Orange => M("Gun_Orange", new Color(1f, 0.5f, 0.1f));

        public static GameObject Build(WeaponDef def, Transform parent)
        {
            var root = new GameObject("Weapon_" + def.id).transform;
            root.SetParent(parent, false);
            if (def == WeaponDef.PopPistol) Pistol(root, def);
            else if (def == WeaponDef.BoomStick) Shotgun(root, def);
            else if (def == WeaponDef.RocketLauncher) Launcher(root, def);
            else if (def == WeaponDef.LongShot) Sniper(root, def);
            else if (def == WeaponDef.LeafBlower) Blower(root, def);
            else if (def == WeaponDef.BigFlush) FlushCannon(root, def);
            else if (def == WeaponDef.BonkRifle || def == WeaponDef.SprayNPray) Rifle(root, def, def == WeaponDef.SprayNPray);
            else if (def == WeaponDef.HandCannon) Pistol(root, def);
            else Box(root, "Body", new Vector3(0f, 0f, 0.2f), new Vector3(0.14f, 0.14f, 0.5f), M("Weapon_" + def.id, def.color));
            return root.gameObject;
        }

        // ---------- Guns ----------

        /// <summary>Pop Pistol: a chunky toy blaster with an orange safety tip.</summary>
        static void Pistol(Transform r, WeaponDef def)
        {
            var body = M("Weapon_" + def.id, def.color);
            Box(r, "Grip", new Vector3(0f, -0.1f, -0.02f), new Vector3(0.09f, 0.2f, 0.11f), Dark, new Vector3(15f, 0f, 0f));
            Box(r, "Body", new Vector3(0f, 0.04f, 0.1f), new Vector3(0.12f, 0.14f, 0.34f), body);
            Box(r, "Rail", new Vector3(0f, 0.12f, 0.08f), new Vector3(0.05f, 0.03f, 0.26f), Dark);
            Tube(r, "Barrel", new Vector3(0f, 0.05f, 0.33f), 0.07f, 0.16f, Dark);
            Tube(r, "Tip", new Vector3(0f, 0.05f, 0.42f), 0.09f, 0.04f, Orange);
            Box(r, "Trigger", new Vector3(0f, -0.04f, 0.05f), new Vector3(0.02f, 0.06f, 0.03f), Dark);
            Box(r, "Fin", new Vector3(0f, 0.04f, -0.06f), new Vector3(0.14f, 0.05f, 0.05f), Orange);
        }

        /// <summary>Boom Stick: sawed-off double barrel with a wooden stock.</summary>
        static void Shotgun(Transform r, WeaponDef def)
        {
            var accent = M("Weapon_" + def.id, def.color);
            Box(r, "Stock", new Vector3(0f, -0.06f, -0.26f), new Vector3(0.09f, 0.14f, 0.34f), Wood, new Vector3(-8f, 0f, 0f));
            Box(r, "Grip", new Vector3(0f, -0.08f, -0.04f), new Vector3(0.08f, 0.14f, 0.1f), Wood, new Vector3(20f, 0f, 0f));
            Box(r, "Receiver", new Vector3(0f, 0.02f, 0.06f), new Vector3(0.14f, 0.12f, 0.2f), Steel);
            Tube(r, "BarrelL", new Vector3(-0.037f, 0.05f, 0.42f), 0.075f, 0.62f, Dark);
            Tube(r, "BarrelR", new Vector3(0.037f, 0.05f, 0.42f), 0.075f, 0.62f, Dark);
            Box(r, "Pump", new Vector3(0f, -0.02f, 0.32f), new Vector3(0.12f, 0.07f, 0.2f), Wood);
            Box(r, "Band", new Vector3(0f, 0.05f, 0.7f), new Vector3(0.17f, 0.1f, 0.04f), accent);
            Box(r, "Hammer", new Vector3(0f, 0.1f, -0.02f), new Vector3(0.04f, 0.05f, 0.05f), Steel);
        }

        /// <summary>Rocket Launcher: shoulder tube with a warhead poking out the front.</summary>
        static void Launcher(Transform r, WeaponDef def)
        {
            var red = M("Weapon_" + def.id, def.color);
            Tube(r, "Tube", new Vector3(0f, 0.12f, 0.15f), 0.2f, 1.05f, Olive);
            Tube(r, "FrontRing", new Vector3(0f, 0.12f, 0.66f), 0.25f, 0.08f, Dark);
            var bell = LowPoly.Make(LowPoly.Cone(10), "RearBell", r, new Vector3(0f, 0.12f, -0.44f), new Vector3(0.3f, 0.16f, 0.3f), Dark,
                new Vector3(90f, 0f, 0f));
            bell.transform.localEulerAngles = new Vector3(90f, 0f, 0f); // wide end faces backward
            Tube(r, "StripeA", new Vector3(0f, 0.12f, 0.35f), 0.21f, 0.05f, red);
            Tube(r, "StripeB", new Vector3(0f, 0.12f, 0.45f), 0.21f, 0.05f, red);
            LowPoly.Make(LowPoly.Cone(8), "Warhead", r, new Vector3(0f, 0.12f, 0.76f), new Vector3(0.15f, 0.18f, 0.15f), red,
                new Vector3(90f, 0f, 0f));
            Box(r, "Grip", new Vector3(0f, -0.04f, 0.05f), new Vector3(0.07f, 0.16f, 0.09f), Dark, new Vector3(10f, 0f, 0f));
            Box(r, "FrontGrip", new Vector3(0f, -0.02f, 0.38f), new Vector3(0.06f, 0.12f, 0.07f), Dark);
            Box(r, "Sight", new Vector3(0.11f, 0.22f, 0.25f), new Vector3(0.05f, 0.08f, 0.12f), Dark);
        }

        /// <summary>Long Shot: long barrel, fat scope, folded bipod.</summary>
        static void Sniper(Transform r, WeaponDef def)
        {
            var body = M("Weapon_" + def.id, def.color);
            Box(r, "Stock", new Vector3(0f, -0.03f, -0.32f), new Vector3(0.08f, 0.15f, 0.36f), Dark, new Vector3(-5f, 0f, 0f));
            Box(r, "Grip", new Vector3(0f, -0.09f, -0.06f), new Vector3(0.07f, 0.14f, 0.08f), Dark, new Vector3(18f, 0f, 0f));
            Box(r, "Body", new Vector3(0f, 0.02f, 0.08f), new Vector3(0.1f, 0.12f, 0.5f), body);
            Tube(r, "Barrel", new Vector3(0f, 0.04f, 0.72f), 0.045f, 0.8f, Dark);
            Box(r, "Brake", new Vector3(0f, 0.04f, 1.12f), new Vector3(0.08f, 0.06f, 0.1f), Steel);
            Tube(r, "Scope", new Vector3(0f, 0.15f, 0.08f), 0.08f, 0.34f, Dark);
            Tube(r, "ScopeFront", new Vector3(0f, 0.15f, 0.26f), 0.1f, 0.05f, Dark);
            Tube(r, "Lens", new Vector3(0f, 0.15f, 0.29f), 0.08f, 0.01f, Mats.Unlit("Gun_Lens", new Color(0.4f, 0.9f, 1f)));
            Box(r, "Mount", new Vector3(0f, 0.09f, 0.08f), new Vector3(0.04f, 0.05f, 0.12f), Steel);
            Box(r, "BipodL", new Vector3(-0.03f, -0.02f, 0.62f), new Vector3(0.02f, 0.02f, 0.28f), Steel);
            Box(r, "BipodR", new Vector3(0.03f, -0.02f, 0.62f), new Vector3(0.02f, 0.02f, 0.28f), Steel);
        }

        /// <summary>Leaf Blower: a fat orange motor box with a long nozzle.</summary>
        static void Blower(Transform r, WeaponDef def)
        {
            var body = M("Weapon_" + def.id, def.color);
            Box(r, "Motor", new Vector3(0f, 0.02f, -0.1f), new Vector3(0.24f, 0.26f, 0.3f), body);
            Box(r, "Handle", new Vector3(0f, 0.2f, -0.1f), new Vector3(0.05f, 0.05f, 0.26f), Dark);
            Tube(r, "Fan", new Vector3(0.13f, 0.02f, -0.1f), 0.22f, 0.04f, Dark);
            Tube(r, "Nozzle", new Vector3(0f, -0.02f, 0.35f), 0.12f, 0.62f, Dark);
            Tube(r, "Mouth", new Vector3(0f, -0.02f, 0.68f), 0.17f, 0.06f, Orange);
            Box(r, "Grip", new Vector3(0f, -0.1f, -0.06f), new Vector3(0.07f, 0.14f, 0.08f), Dark, new Vector3(15f, 0f, 0f));
        }

        /// <summary>The Big Flush: a porcelain launcher with a plunger stuck on top.</summary>
        static void FlushCannon(Transform r, WeaponDef def)
        {
            var white = M("Porcelain", new Color(0.93f, 0.94f, 0.95f));
            Tube(r, "Tube", new Vector3(0f, 0.12f, 0.15f), 0.3f, 1.0f, white);
            Tube(r, "Rim", new Vector3(0f, 0.12f, 0.66f), 0.36f, 0.08f, white);
            Tube(r, "Hole", new Vector3(0f, 0.12f, 0.69f), 0.24f, 0.02f, M("Toilet_Water", new Color(0.35f, 0.6f, 0.9f)));
            Box(r, "Tank", new Vector3(0f, 0.12f, -0.42f), new Vector3(0.34f, 0.3f, 0.18f), white);
            Box(r, "Lever", new Vector3(0.18f, 0.22f, -0.38f), new Vector3(0.06f, 0.02f, 0.02f), Steel);
            LowPoly.Make(LowPoly.Cylinder(8), "PlungerStick", r, new Vector3(0f, 0.38f, 0.1f), new Vector3(0.04f, 0.32f, 0.04f), Wood);
            LowPoly.Make(LowPoly.Cone(8), "PlungerCup", r, new Vector3(0f, 0.3f, 0.1f), new Vector3(0.14f, 0.1f, 0.14f),
                M("Plunger_Red", new Color(0.8f, 0.15f, 0.15f)), new Vector3(180f, 0f, 0f));
            Box(r, "Grip", new Vector3(0f, -0.06f, 0.0f), new Vector3(0.07f, 0.16f, 0.09f), Dark, new Vector3(10f, 0f, 0f));
        }

        /// <summary>Bonk Rifle / Spray-n-Pray fallback: a short carbine (the SMG gets a drum magazine).</summary>
        static void Rifle(Transform r, WeaponDef def, bool smg)
        {
            var body = M("Weapon_" + def.id, def.color);
            Box(r, "Stock", new Vector3(0f, -0.03f, -0.24f), new Vector3(0.08f, 0.13f, 0.26f), Dark);
            Box(r, "Grip", new Vector3(0f, -0.09f, -0.04f), new Vector3(0.07f, 0.14f, 0.08f), Dark, new Vector3(18f, 0f, 0f));
            Box(r, "Body", new Vector3(0f, 0.02f, 0.1f), new Vector3(0.11f, 0.13f, 0.42f), body);
            Tube(r, "Barrel", new Vector3(0f, 0.04f, 0.42f), 0.05f, 0.3f, Dark);
            if (smg) Tube(r, "Drum", new Vector3(0f, -0.1f, 0.12f), 0.2f, 0.08f, Dark);
            else Box(r, "Mag", new Vector3(0f, -0.12f, 0.12f), new Vector3(0.06f, 0.16f, 0.08f), Dark, new Vector3(-10f, 0f, 0f));
        }

        // ---------- Gadgets and projectiles ----------

        /// <summary>A cartoon bomb: round black body, a cap, and a fuse (the grenade).</summary>
        public static Transform CartoonBomb(Transform parent, Color band)
        {
            var root = new GameObject("BombModel").transform;
            root.SetParent(parent, false);
            LowPoly.Make(LowPoly.Ico(1), "Body", root, Vector3.zero, Vector3.one, M("Bomb_Black", new Color(0.12f, 0.12f, 0.14f)));
            LowPoly.Make(LowPoly.Torus(12, 4, 0.18f), "Band", root, Vector3.zero, new Vector3(1.04f, 0.9f, 1.04f), M("Bomb_Band", band),
                new Vector3(90f, 0f, 0f));
            LowPoly.Make(LowPoly.Cylinder(8), "Cap", root, new Vector3(0f, 0.5f, 0f), new Vector3(0.32f, 0.16f, 0.32f), Steel);
            LowPoly.Make(LowPoly.Cylinder(6), "Fuse", root, new Vector3(0.05f, 0.66f, 0f), new Vector3(0.07f, 0.22f, 0.07f),
                M("Bomb_Fuse", new Color(0.8f, 0.7f, 0.5f)), new Vector3(0f, 0f, -20f));
            var spark = new GameObject("Spark").transform;
            spark.SetParent(root, false);
            spark.localPosition = new Vector3(0.1f, 0.8f, 0f);
            return spark;
        }

        /// <summary>A rocket: white body, red nose, dark fins. Points along +Z.</summary>
        public static void Rocket(Transform parent, Color nose)
        {
            var white = M("Rocket_Body", new Color(0.92f, 0.92f, 0.9f));
            var red = M("Rocket_Nose", nose);
            LowPoly.Make(LowPoly.Cylinder(8), "Body", parent, new Vector3(0f, 0f, -0.05f), new Vector3(0.18f, 0.5f, 0.18f), white, new Vector3(90f, 0f, 0f));
            LowPoly.Make(LowPoly.Cone(8), "Nose", parent, new Vector3(0f, 0f, 0.3f), new Vector3(0.18f, 0.22f, 0.18f), red, new Vector3(90f, 0f, 0f));
            LowPoly.Make(LowPoly.Cylinder(8), "Nozzle", parent, new Vector3(0f, 0f, -0.34f), new Vector3(0.14f, 0.08f, 0.14f), Dark, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 4; i++)
            {
                var fin = Shapes.Make(PrimitiveType.Cube, "Fin", parent, Vector3.zero, new Vector3(0.02f, 0.14f, 0.16f), red, false);
                fin.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                fin.transform.localPosition = fin.transform.localRotation * new Vector3(0f, 0.12f, -0.22f);
            }
        }

        /// <summary>Gary's "bomb": a big white seagull dropping with a little swirl on top.</summary>
        public static void GaryBomb(Transform parent)
        {
            var white = M("Gary_Dropping", new Color(0.95f, 0.95f, 0.88f));
            LowPoly.Make(LowPoly.Ico(1), "Blob", parent, Vector3.zero, new Vector3(1f, 0.75f, 1f), white);
            LowPoly.Make(LowPoly.Ico(1), "Blob2", parent, new Vector3(0f, 0.38f, 0f), new Vector3(0.7f, 0.5f, 0.7f), white);
            LowPoly.Make(LowPoly.Cone(6), "Swirl", parent, new Vector3(0f, 0.72f, 0f), new Vector3(0.35f, 0.4f, 0.35f), white,
                new Vector3(0f, 0f, 12f));
        }

        /// <summary>A spiked steel ball (wrecking balls, the Wrecking Ball streak). Unit size: radius 0.5 + spikes.</summary>
        public static void SpikeBall(Transform parent, Material body, Material spikes, float spikeLength = 0.35f)
        {
            LowPoly.Make(LowPoly.Ico(1), "Ball", parent, Vector3.zero, Vector3.one, body);
            // Spikes on the 12 icosahedron corners
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] dirs =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            foreach (var d0 in dirs)
            {
                Vector3 d = d0.normalized;
                var s = LowPoly.Make(LowPoly.Cone(5), "Spike", parent, d * (0.45f + spikeLength * 0.5f),
                    new Vector3(0.22f, spikeLength, 0.22f), spikes);
                s.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d);
            }
            LowPoly.Make(LowPoly.Torus(16, 4, 0.12f), "Band", parent, Vector3.zero, new Vector3(1.04f, 1.3f, 1.04f),
                M("Hazard_Stripe", Palette.Stripe));
        }

        /// <summary>A whole toilet (The Big Flush shot, the Tactical Toilet streak). About 1 m tall, front along +Z.</summary>
        public static void Toilet(Transform parent)
        {
            var white = M("Porcelain", new Color(0.93f, 0.94f, 0.95f));
            LowPoly.Make(LowPoly.Cylinder(10), "Base", parent, new Vector3(0f, 0.2f, 0.05f), new Vector3(0.38f, 0.4f, 0.45f), white);
            LowPoly.Make(LowPoly.Cylinder(12), "Bowl", parent, new Vector3(0f, 0.48f, 0.12f), new Vector3(0.62f, 0.2f, 0.75f), white);
            LowPoly.Make(LowPoly.Cylinder(12), "Water", parent, new Vector3(0f, 0.585f, 0.12f), new Vector3(0.46f, 0.02f, 0.56f),
                M("Toilet_Water", new Color(0.35f, 0.6f, 0.9f)));
            LowPoly.Make(LowPoly.Torus(14, 4, 0.18f), "Seat", parent, new Vector3(0f, 0.6f, 0.12f), new Vector3(0.64f, 0.5f, 0.78f), white);
            Shapes.Make(PrimitiveType.Cube, "Tank", parent, new Vector3(0f, 0.85f, -0.3f), new Vector3(0.62f, 0.55f, 0.24f), white, false);
            Shapes.Make(PrimitiveType.Cube, "Lid", parent, new Vector3(0f, 1.14f, -0.3f), new Vector3(0.66f, 0.05f, 0.28f), white, false);
            Shapes.Make(PrimitiveType.Cube, "Handle", parent, new Vector3(0.26f, 1.0f, -0.17f), new Vector3(0.1f, 0.03f, 0.03f), Steel, false);
        }

        /// <summary>A cartoon anvil (Anvil Drop). About 1 m long, 0.7 m tall.</summary>
        public static void Anvil(Transform parent)
        {
            var iron = M("Anvil_Iron", new Color(0.2f, 0.21f, 0.24f));
            Shapes.Make(PrimitiveType.Cube, "Foot", parent, new Vector3(0f, 0.1f, 0f), new Vector3(0.6f, 0.2f, 0.5f), iron, false);
            Shapes.Make(PrimitiveType.Cube, "Waist", parent, new Vector3(0f, 0.33f, 0f), new Vector3(0.32f, 0.3f, 0.3f), iron, false);
            Shapes.Make(PrimitiveType.Cube, "Face", parent, new Vector3(-0.05f, 0.58f, 0f), new Vector3(0.82f, 0.22f, 0.42f), iron, false);
            var horn = LowPoly.Make(LowPoly.Cone(8), "Horn", parent, new Vector3(0.55f, 0.6f, 0f), new Vector3(0.26f, 0.38f, 0.26f), iron);
            horn.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        /// <summary>A banana peel lying flat (Banana Peel gadget).</summary>
        public static void BananaPeel(Transform parent)
        {
            var yellow = M("Banana", new Color(1f, 0.86f, 0.2f));
            var inner = M("Banana_In", new Color(1f, 0.95f, 0.7f));
            LowPoly.Make(LowPoly.Ico(1), "Middle", parent, new Vector3(0f, 0.08f, 0f), new Vector3(0.25f, 0.16f, 0.25f), inner);
            for (int i = 0; i < 4; i++)
            {
                var flap = LowPoly.Make(LowPoly.Ico(1), "Flap", parent, Vector3.zero, new Vector3(0.14f, 0.05f, 0.5f), yellow);
                flap.transform.localRotation = Quaternion.Euler(0f, i * 90f + 20f, 0f);
                flap.transform.localPosition = flap.transform.localRotation * new Vector3(0f, 0.03f, 0.26f);
            }
            Shapes.Make(PrimitiveType.Cube, "Stem", parent, new Vector3(0f, 0.2f, 0f), new Vector3(0.05f, 0.1f, 0.05f),
                M("Banana_Stem", new Color(0.35f, 0.25f, 0.1f)), false);
        }

        /// <summary>A bounce pad: a bright trampoline disc with a coil (Bounce Pad gadget). 1.6 m across.</summary>
        public static void BouncePad(Transform parent)
        {
            LowPoly.Make(LowPoly.Cylinder(14), "Frame", parent, new Vector3(0f, 0.1f, 0f), new Vector3(1.6f, 0.2f, 1.6f), Dark);
            LowPoly.Make(LowPoly.Cylinder(14), "Mat", parent, new Vector3(0f, 0.21f, 0f), new Vector3(1.35f, 0.03f, 1.35f),
                M("Pad_Pink", new Color(1f, 0.35f, 0.7f)));
            LowPoly.Make(LowPoly.Torus(16, 4, 0.12f), "Rim", parent, new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.8f, 1.6f),
                M("Hazard_Stripe", Palette.Stripe));
        }

        /// <summary>A stink bomb: a brown paper bag with green fumes (Stink Bomb gadget). Unit size.</summary>
        public static Transform StinkBomb(Transform parent)
        {
            var root = new GameObject("StinkModel").transform;
            root.SetParent(parent, false);
            Shapes.Make(PrimitiveType.Cube, "Bag", root, new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.8f, 0.5f),
                M("Paper_Bag", new Color(0.62f, 0.45f, 0.28f)), false);
            Shapes.Make(PrimitiveType.Cube, "Fold", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.72f, 0.12f, 0.52f),
                M("Paper_Bag_Dark", new Color(0.5f, 0.36f, 0.22f)), false);
            var spark = new GameObject("Fumes").transform;
            spark.SetParent(root, false);
            spark.localPosition = new Vector3(0f, 0.6f, 0f);
            return spark;
        }

        // ---------- Helpers ----------

        static GameObject Box(Transform p, string name, Vector3 pos, Vector3 size, Material mat, Vector3? euler = null) =>
            Shapes.Make(PrimitiveType.Cube, name, p, pos, size, mat, false, euler);

        /// <summary>A cylinder lying along Z.</summary>
        static GameObject Tube(Transform p, string name, Vector3 pos, float diameter, float length, Material mat) =>
            LowPoly.Make(LowPoly.Cylinder(10), name, p, pos, new Vector3(diameter, length, diameter), mat, new Vector3(90f, 0f, 0f));
    }
}
