using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Builds the Scrap Sprint greybox. The course runs along +Z:
    ///
    ///   Start pad -> gap -> backwards conveyor with bumpers -> CP1
    ///   -> narrow bridge with 3 wrecking balls -> CP2
    ///   -> 4 floating platforms (jump gaps) -> CP3
    ///   -> crusher hall (3 rows of slamming blocks)   [rocket-jump shortcut ledge on the left]
    ///   -> spin bar -> ramp -> finish line.
    ///
    /// All numbers live here, so changing the layout means editing this file and
    /// re-running "Knockback Rumble > Build Scrap Sprint Scene".
    /// </summary>
    public static class ScrapSprintBuilder
    {
        static Material floor, floorDark, belt, stripe, metal, rust, hazard, checkpointMat, gold, shortcut, bumper, pit, black, white, potty;

        public static CourseInfo Build(Transform parent)
        {
            LoadMaterials();

            var root = new GameObject("ScrapSprint").transform;
            if (parent != null) root.SetParent(parent, false);
            var info = root.gameObject.AddComponent<CourseInfo>();
            info.courseName = "Scrap Sprint";
            info.killY = -12f;

            BuildStart(Group(root, "1_Start"), info);
            BuildConveyor(Group(root, "2_Conveyor"));
            BuildWreckingBridge(Group(root, "3_WreckingBalls"));
            BuildGaps(Group(root, "4_Gaps"));
            BuildCrusherHall(Group(root, "5_Crushers"));
            BuildFinish(Group(root, "6_Finish"));
            BuildScenery(Group(root, "7_Scenery"));

            info.waypoints = new[]
            {
                new Vector3(0f, 0f, 9f), new Vector3(0f, 0f, 14f), new Vector3(0f, 0f, 22f), new Vector3(0f, 0f, 32f),
                new Vector3(0f, 0f, 41f), new Vector3(0f, 0f, 47f), new Vector3(0f, 0f, 58f), new Vector3(0f, 0f, 68f),
                new Vector3(0f, 0f, 77f), new Vector3(0f, 0f, 81f),
                new Vector3(-1.5f, 0.5f, 89f), new Vector3(1.5f, 1f, 96.5f), new Vector3(-1f, 0.5f, 104f), new Vector3(1f, 0f, 111.5f),
                new Vector3(0f, 0f, 118f), new Vector3(0f, 0f, 124f), new Vector3(0f, 0f, 131.5f), new Vector3(0f, 0f, 138.5f),
                new Vector3(0f, 0f, 146f), new Vector3(3.8f, 0f, 150f), new Vector3(0f, 0f, 155f), new Vector3(0f, 1.5f, 160f),
                new Vector3(0f, 3f, 168f), new Vector3(0f, 3f, 174f),
            };

            return info;
        }

        static void LoadMaterials()
        {
            floor = Mats.Get("Floor", Palette.Floor);
            floorDark = Mats.Get("Floor_Dark", Palette.FloorDark);
            belt = Mats.Get("Belt", Palette.Belt);
            stripe = Mats.Get("Stripe", Palette.Stripe);
            metal = Mats.Get("Metal", Palette.Metal);
            rust = Mats.Get("Rust", Palette.Rust);
            hazard = Mats.Get("Hazard_Red", Palette.Hazard);
            checkpointMat = Mats.Get("Checkpoint", Palette.Checkpoint);
            gold = Mats.Get("Gold", Palette.Gold);
            shortcut = Mats.Get("Shortcut", Palette.Shortcut);
            bumper = Mats.Get("Bumper", Palette.Bumper);
            pit = Mats.Get("Pit", Palette.Pit);
            black = Mats.Get("Dummy_Black", Palette.Black);
            white = Mats.Get("White", Palette.White);
            potty = Mats.Get("Potty", new Color(0.25f, 0.45f, 0.85f));
        }

        // ---------- Sections ----------

        static void BuildStart(Transform g, CourseInfo info)
        {
            Floor(g, "StartPad", 0f, -8f, 10f, 16f, 0f, floorDark);
            Block(g, "BackWall", new Vector3(0f, 1.5f, -8.5f), new Vector3(16f, 3f, 1f), metal);
            Block(g, "RailL", new Vector3(-8.25f, 0.5f, 1f), new Vector3(0.5f, 1f, 18f), metal);
            Block(g, "RailR", new Vector3(8.25f, 0.5f, 1f), new Vector3(0.5f, 1f, 18f), metal);

            var spawns = new List<Vector3>();
            float[] rows = { 1f, -1.5f, -4f, -6.5f };
            float[] cols = { -6f, -2f, 2f, 6f };
            foreach (float z in rows)
                foreach (float x in cols)
                    spawns.Add(new Vector3(x, 0.05f, z));
            info.spawnPoints = spawns.ToArray();

            Pickup(g, "pistol", new Vector3(-4f, 0f, 7f));
            Pickup(g, "pistol", new Vector3(4f, 0f, 7f));
            Pickup(g, "random", new Vector3(0f, 0f, 8f));

            Sign(g, "SCRAP SPRINT", new Vector3(0f, 7f, 12f), 0.5f, Palette.Gold);
            Sign(g, "brought to you by SHART ENERGY", new Vector3(0f, 5.2f, 12f), 0.18f, Color.white);
        }

        static void BuildConveyor(Transform g)
        {
            // Backwards belt: you run at 8, it drags you at 3. No side rails.
            var beltGo = Floor(g, "Belt", 0f, 12.5f, 40f, 12f, 0f, belt);
            var conveyor = beltGo.AddComponent<Conveyor>();
            conveyor.velocity = new Vector3(0f, 0f, -3f);
            conveyor.stripeLength = 27.5f;

            var stripes = new GameObject("BeltStripes").transform;
            stripes.SetParent(g, false);
            stripes.localPosition = new Vector3(0f, 0.02f, 26.25f);
            for (int i = 0; i < 11; i++)
                Shapes.Make(PrimitiveType.Cube, "Stripe", stripes, new Vector3(0f, 0f, -13.75f + i * 2.5f), new Vector3(11.6f, 0.04f, 0.6f), stripe, false);
            conveyor.stripes = stripes;

            Vector3[] bumpers = { new Vector3(-3f, 0f, 18f), new Vector3(3f, 0f, 24f), new Vector3(-2.5f, 0f, 30f), new Vector3(3.5f, 0f, 35f) };
            foreach (var p in bumpers)
            {
                var b = Shapes.Make(PrimitiveType.Cylinder, "Bumper", g, p + Vector3.up, new Vector3(1.6f, 1f, 1.6f), bumper);
                b.AddComponent<Bumper>();
            }

            // CP1
            Floor(g, "CP1_Pad", 0f, 40f, 46f, 12f, 0f, floorDark);
            CheckpointGate(g, 0, new Vector3(0f, 0f, 43f), 12f);
            Pickup(g, "rocket", new Vector3(-3f, 0f, 44.5f));
            Pickup(g, "random", new Vector3(3f, 0f, 44.5f));
        }

        static void BuildWreckingBridge(Transform g)
        {
            Floor(g, "Bridge", 0f, 46f, 78f, 6f, 0f, floor);
            float[] zs = { 52f, 61f, 70f };
            for (int i = 0; i < zs.Length; i++)
            {
                float z = zs[i];
                var wb = new GameObject("WreckingBall_" + (i + 1)).transform;
                wb.SetParent(g, false);
                wb.localPosition = new Vector3(0f, 0f, z);

                Block(wb, "PostL", new Vector3(-7f, 6f, 0f), new Vector3(0.6f, 12f, 0.6f), metal, false);
                Block(wb, "PostR", new Vector3(7f, 6f, 0f), new Vector3(0.6f, 12f, 0.6f), metal, false);
                Block(wb, "Beam", new Vector3(0f, 12f, 0f), new Vector3(14.6f, 0.6f, 0.6f), metal, false);

                var pivot = new GameObject("Pivot").transform;
                pivot.SetParent(wb, false);
                pivot.localPosition = new Vector3(0f, 11.8f, 0f);
                Shapes.Make(PrimitiveType.Cylinder, "Chain", pivot, new Vector3(0f, -5f, 0f), new Vector3(0.15f, 5f, 0.15f), black, false);
                var ball = Shapes.Make(PrimitiveType.Sphere, "Ball", pivot, new Vector3(0f, -10f, 0f), Vector3.one * 3.2f, hazard, false);

                var comp = wb.gameObject.AddComponent<WreckingBall>();
                comp.pivot = pivot;
                comp.ball = ball.transform;
                comp.ballRadius = 1.6f;
                comp.phase = i * 2.1f;
            }
        }

        static void BuildGaps(Transform g)
        {
            Floor(g, "CP2_Pad", 0f, 78f, 84f, 10f, 0f, floorDark);
            CheckpointGate(g, 1, new Vector3(0f, 0f, 81f), 10f);
            Pickup(g, "rocket", new Vector3(-2.5f, 0f, 82.5f));
            Pickup(g, "pistol", new Vector3(2.5f, 0f, 82.5f));

            // 5x5 platforms with 2.5m gaps and small height changes
            Vector3[] plats = { new Vector3(-1.5f, 0.5f, 89f), new Vector3(1.5f, 1f, 96.5f), new Vector3(-1f, 0.5f, 104f), new Vector3(1f, 0f, 111.5f) };
            for (int i = 0; i < plats.Length; i++)
            {
                var p = plats[i];
                Floor(g, "Platform_" + (i + 1), p.x, p.z - 2.5f, p.z + 2.5f, 5f, p.y, i % 2 == 0 ? rust : metal);
            }
        }

        static void BuildCrusherHall(Transform g)
        {
            Floor(g, "CP3_Pad", 0f, 116.5f, 122f, 14f, 0f, floorDark);
            CheckpointGate(g, 2, new Vector3(0f, 0f, 119.5f), 14f);
            Pickup(g, "rocket", new Vector3(-5f, 0f, 120.5f)); // right next to the shortcut wall
            Pickup(g, "random", new Vector3(3f, 0f, 120.5f));

            Floor(g, "HallFloor", 0f, 122f, 148f, 14f, 0f, floor);

            float[] rowZ = { 128f, 135f, 142f };
            float[,] phases = { { 0f, 1.7f }, { 0.85f, 2.55f }, { 1.7f, 0f } };
            for (int r = 0; r < rowZ.Length; r++)
            {
                float z = rowZ[r];
                Block(g, "HousingL_" + r, new Vector3(-7.3f, 4f, z), new Vector3(0.6f, 8f, 0.6f), metal, false);
                Block(g, "HousingR_" + r, new Vector3(7.3f, 4f, z), new Vector3(0.6f, 8f, 0.6f), metal, false);
                Block(g, "HousingBeam_" + r, new Vector3(0f, 8.2f, z), new Vector3(15.2f, 0.5f, 0.5f), metal, false);

                for (int lane = 0; lane < 2; lane++)
                {
                    float x = lane == 0 ? -3.5f : 3.5f;
                    var block = Block(g, "Crusher_" + r + "_" + lane, new Vector3(x, 6f, z), new Vector3(6.5f, 2f, 4f), hazard);
                    var rb = block.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    var crusher = block.AddComponent<Crusher>();
                    crusher.upY = 6f;
                    crusher.downY = 1f;
                    crusher.size = new Vector3(6.5f, 2f, 4f);
                    crusher.phase = phases[r, lane];
                    crusher.blockRenderer = block.GetComponent<Renderer>();
                }
            }

            // Rocket-jump shortcut: a 5.5m wall/ledge along the left side, all the way to the finish.
            Block(g, "ShortcutLedge", new Vector3(-9f, 2.25f, 143f), new Vector3(4f, 6.5f, 46f), shortcut);
            Sign(g, "EXPRESS LANE", new Vector3(-9f, 8f, 119.8f), 0.25f, Palette.Shortcut);
            Sign(g, "(rocket jump. probably fine. - Dr. Lee Ability)", new Vector3(-9f, 6.9f, 119.8f), 0.1f, Color.white);
        }

        static void BuildFinish(Transform g)
        {
            Floor(g, "AfterHall", 0f, 148f, 154f, 14f, 0f, floorDark);

            var spin = new GameObject("SpinBar").transform;
            spin.SetParent(g, false);
            spin.localPosition = new Vector3(0f, 0f, 151f);
            Shapes.Make(PrimitiveType.Cylinder, "Post", spin, new Vector3(0f, 0.75f, 0f), new Vector3(1f, 0.75f, 1f), metal);
            var bar = Shapes.Make(PrimitiveType.Cube, "Bar", spin, new Vector3(0f, 0.8f, 0f), new Vector3(13f, 0.35f, 0.35f), hazard, false);
            var sb = spin.gameObject.AddComponent<SpinBar>();
            sb.length = 13f;
            sb.barHeight = 0.8f;
            sb.bar = bar.transform;

            // Ramp from (z 154, y 0) up to (z 166, y 3)
            float rise = 3f, run = 12f;
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(rise * rise + run * run);
            float a = angle * Mathf.Deg2Rad;
            Block(g, "Ramp", new Vector3(0f, 1.5f - 0.5f * Mathf.Cos(a), 160f + 0.5f * Mathf.Sin(a)), new Vector3(10f, 1f, len), floor, true, new Vector3(-angle, 0f, 0f));

            // Finish platform (extends left so the shortcut ledge drops onto it)
            Floor(g, "FinishPad", -2.5f, 166f, 182f, 19f, 3f, floorDark);
            Block(g, "FinishBackWall", new Vector3(-2.5f, 5f, 182.5f), new Vector3(19f, 4f, 1f), metal);
            Block(g, "FinishRailR", new Vector3(7.25f, 3.5f, 174f), new Vector3(0.5f, 1f, 16f), metal);

            var line = new GameObject("FinishLine").transform;
            line.SetParent(g, false);
            line.localPosition = new Vector3(-2.5f, 3.05f, 171f);
            var trigger = line.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(19f, 6f, 1.5f);
            trigger.center = new Vector3(0f, 3f, 0f);
            line.gameObject.AddComponent<FinishLine>();

            Block(g, "ArchL", new Vector3(-12.2f, 6f, 171f), new Vector3(0.6f, 6f, 0.6f), gold);
            Block(g, "ArchR", new Vector3(7.4f, 6f, 171f), new Vector3(0.6f, 6f, 0.6f), gold);
            Block(g, "ArchTop", new Vector3(-2.4f, 9.2f, 171f), new Vector3(20.2f, 0.8f, 0.6f), gold, false);
            for (int i = 0; i < 19; i++)
                Block(g, "Checker", new Vector3(-11.5f + i, 3.02f, 171f), new Vector3(1f, 0.04f, 1.2f), i % 2 == 0 ? black : white, false);
            Sign(g, "FINISH", new Vector3(-2.5f, 11f, 170.6f), 0.45f, Palette.Gold);

            // Lord Dookington III, the show's porta-potty mascot, watching from the finish.
            Block(g, "LordDookington", new Vector3(4.5f, 4.4f, 179f), new Vector3(1.6f, 2.8f, 1.6f), potty);
            Block(g, "LordDookington_Hat", new Vector3(4.5f, 6.2f, 179f), new Vector3(1f, 0.8f, 1f), black, false);
            Block(g, "LordDookington_HatBrim", new Vector3(4.5f, 5.85f, 179f), new Vector3(1.6f, 0.1f, 1.6f), black, false);
            Sign(g, "LORD DOOKINGTON III", new Vector3(4.5f, 7.2f, 178.1f), 0.08f, Color.white);
        }

        static void BuildScenery(Transform g)
        {
            Block(g, "ScrapPit", new Vector3(0f, -14f, 90f), new Vector3(220f, 1f, 280f), pit, false);
            var rng = new System.Random(69);
            Material[] mats = { rust, metal, floorDark, rust };
            for (int i = 0; i < 70; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * (15f + (float)rng.NextDouble() * 30f);
                float z = -20f + (float)rng.NextDouble() * 220f;
                float h = 3f + (float)rng.NextDouble() * 12f;
                float w = 2f + (float)rng.NextDouble() * 4f;
                Block(g, "Scrap", new Vector3(x, -13.5f + h * 0.5f, z), new Vector3(w, h, w * (0.6f + (float)rng.NextDouble())),
                    mats[rng.Next(mats.Length)], false, new Vector3(0f, (float)rng.NextDouble() * 90f, 0f));
            }
        }

        // ---------- Kit pieces ----------

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>A 1m-thick slab whose top surface is at "top".</summary>
        static GameObject Floor(Transform g, string name, float x, float z0, float z1, float width, float top, Material mat) =>
            Block(g, name, new Vector3(x, top - 0.5f, (z0 + z1) * 0.5f), new Vector3(width, 1f, z1 - z0), mat);

        /// <summary>A box positioned relative to its group (section groups sit at the course origin).</summary>
        static GameObject Block(Transform g, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true,
            Vector3? euler = null) =>
            Shapes.Make(PrimitiveType.Cube, name, g, pos, size, mat, collider, euler);

        static void CheckpointGate(Transform g, int index, Vector3 pos, float width)
        {
            var cp = new GameObject("Checkpoint_" + (index + 1)).transform;
            cp.SetParent(g, false);
            cp.localPosition = pos + Vector3.up * 0.1f;
            var box = cp.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, 6f, 2f);
            box.center = new Vector3(0f, 3f, 0f);
            cp.gameObject.AddComponent<Checkpoint>().index = index;

            float half = width * 0.5f + 0.2f;
            Block(g, "CP" + (index + 1) + "_PostL", pos + new Vector3(-half, 2f, 0f), new Vector3(0.4f, 4f, 0.4f), checkpointMat);
            Block(g, "CP" + (index + 1) + "_PostR", pos + new Vector3(half, 2f, 0f), new Vector3(0.4f, 4f, 0.4f), checkpointMat);
            Block(g, "CP" + (index + 1) + "_Top", pos + new Vector3(0f, 4.2f, 0f), new Vector3(width + 0.8f, 0.4f, 0.4f), checkpointMat, false);
        }

        static void Pickup(Transform g, string id, Vector3 pos)
        {
            var go = new GameObject("Pickup_" + id);
            go.transform.SetParent(g, false);
            go.transform.localPosition = pos;
            go.AddComponent<WeaponPickup>().Setup(id);
        }

        static void Sign(Transform g, string text, Vector3 pos, float size, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(g, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }
}
