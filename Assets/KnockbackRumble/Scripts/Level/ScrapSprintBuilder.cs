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
            info.kind = EventKind.Gauntlet;
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

            // Ammo crates are deliberately scarce (4 on the whole course), so ammo is worth saving.
            Pickup(g, "ammo", new Vector3(0f, 0f, 8f));

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
            var roller = Mats.Get("Prop_DarkSteel", new Color(0.3f, 0.31f, 0.34f));
            foreach (float z in new[] { 12.5f, 40f })
                LowPoly.Make(LowPoly.Cylinder(12), "Roller", g, new Vector3(0f, -0.5f, z), new Vector3(1.1f, 12.4f, 1.1f), roller, new Vector3(0f, 0f, 90f));

            Vector3[] bumpers = { new Vector3(-3f, 0f, 18f), new Vector3(3f, 0f, 24f), new Vector3(-2.5f, 0f, 30f), new Vector3(3.5f, 0f, 35f) };
            foreach (var p in bumpers)
            {
                var b = Shapes.Make(PrimitiveType.Cylinder, "Bumper", g, p + Vector3.up, new Vector3(1.6f, 1f, 1.6f), bumper);
                b.AddComponent<Bumper>();
                Props.DressBumper(b.transform);
            }

            // CP1
            Floor(g, "CP1_Pad", 0f, 40f, 46f, 12f, 0f, floorDark);
            CheckpointGate(g, 0, new Vector3(0f, 0f, 43f), 12f);
            Pickup(g, "ammo", new Vector3(0f, 0f, 44.5f));
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
                // Chain of alternating links down to a spiked steel ball
                var chainMat = Mats.Get("Prop_DarkSteel", new Color(0.3f, 0.31f, 0.34f));
                for (int k = 0; k < 14; k++)
                    LowPoly.Make(LowPoly.Torus(10, 4, 0.3f), "Link", pivot, new Vector3(0f, -0.4f - k * 0.62f, 0f), new Vector3(0.45f, 0.45f, 0.8f),
                        chainMat, new Vector3(90f, k % 2 == 0 ? 0f : 90f, 0f));
                var ball = new GameObject("Ball").transform;
                ball.SetParent(pivot, false);
                ball.localPosition = new Vector3(0f, -10f, 0f);
                ball.localScale = Vector3.one * 3.2f;
                WeaponModels.SpikeBall(ball, Mats.Get("SpikeBall_Body", new Color(0.22f, 0.22f, 0.25f)), metal, 0.28f);

                var comp = wb.gameObject.AddComponent<WreckingBall>();
                comp.pivot = pivot;
                comp.ball = ball;
                comp.ballRadius = 1.9f;
                comp.phase = i * 2.1f;
            }
        }

        static void BuildGaps(Transform g)
        {
            Floor(g, "CP2_Pad", 0f, 78f, 84f, 10f, 0f, floorDark);
            CheckpointGate(g, 1, new Vector3(0f, 0f, 81f), 10f);
            Pickup(g, "ammo", new Vector3(0f, 0f, 82.5f));

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
            Pickup(g, "ammo", new Vector3(-5f, 0f, 120.5f)); // right next to the shortcut wall

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
                    DressCrusher(block.transform);
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
            for (int k = 0; k < 13; k += 2)
                Shapes.Make(PrimitiveType.Cube, "Stripe", bar.transform, new Vector3(-0.5f + (k + 0.5f) / 13f, 0f, 0f),
                    new Vector3(1f / 13f, 1.04f, 1.04f), white, false);
            var weight = Mats.Get("SpikeBall_Body", new Color(0.22f, 0.22f, 0.25f));
            LowPoly.Make(LowPoly.Ico(1), "EndL", spin, new Vector3(-6.6f, 0.8f, 0f), Vector3.one * 0.7f, weight);
            LowPoly.Make(LowPoly.Ico(1), "EndR", spin, new Vector3(6.6f, 0.8f, 0f), Vector3.one * 0.7f, weight);
            LowPoly.Make(LowPoly.Cylinder(10), "Hub", spin, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 0.6f, 1.2f), metal);
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
            // The junkyard pit under the course
            Block(g, "ScrapPit", new Vector3(0f, -14f, 90f), new Vector3(220f, 1f, 280f), pit, false);
            var rng = new System.Random(69);
            Color[] drumColors = { new Color(0.75f, 0.2f, 0.18f), new Color(0.2f, 0.45f, 0.75f), new Color(0.85f, 0.7f, 0.2f), new Color(0.3f, 0.55f, 0.3f) };
            for (int i = 0; i < 55; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * (13f + (float)rng.NextDouble() * 16f);
                float z = -20f + (float)rng.NextDouble() * 220f;
                var p = new Vector3(x, -13.5f, z);
                switch (rng.Next(5))
                {
                    case 0: Props.TireStack(g, p, 2 + rng.Next(4), rng); break;
                    case 1: Props.Drum(g, p, drumColors[rng.Next(drumColors.Length)], rng.NextDouble() < 0.4); break;
                    case 2: Props.CarWreck(g, p, (float)rng.NextDouble() * 360f, drumColors[rng.Next(drumColors.Length)], (float)rng.NextDouble() * 20f - 10f); break;
                    default: Props.ScrapPile(g, p, 3f + (float)rng.NextDouble() * 4f, rng); break;
                }
            }
            // Car stacks: the "towers" of the junkyard
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var p = new Vector3(side * (24f + (float)rng.NextDouble() * 4f), -13.5f, 10f + i * 22f);
                for (int k = 0; k < 3 + rng.Next(3); k++)
                    Props.CarWreck(g, p + Vector3.up * k * 1.9f, (float)rng.NextDouble() * 40f - 20f + (k % 2) * 90f,
                        drumColors[rng.Next(drumColors.Length)], 0f);
            }

            // The TV show: grandstands on both sides, light towers, a jumbotron past the finish
            for (int i = 0; i < 3; i++)
            {
                float z = 20f + i * 65f;
                Props.Grandstand(g, new Vector3(-36f, -1f, z), 90f, 44f, 5, rng, 13f);
                Props.Grandstand(g, new Vector3(36f, -1f, z), -90f, 44f, 5, rng, 13f);
            }
            for (int i = 0; i < 4; i++)
            {
                float z = -5f + i * 60f;
                Props.LightTower(g, new Vector3(-20f, -14f, z), 30f, new Vector3(0f, 0f, z + 10f));
                Props.LightTower(g, new Vector3(20f, -14f, z), 30f, new Vector3(0f, 0f, z + 10f));
            }
            Props.Jumbotron(g, new Vector3(-2.5f, 22f, 205f), 0f, "KNOCKBACK RUMBLE", "brought to you by SHART ENERGY");
            Props.Backdrop(g, new Vector3(0f, 0f, 90f), 120f, 7, "SHART ENERGY");
        }

        // ---------- Kit pieces ----------

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>A 1m-thick slab whose top surface is at "top".</summary>
        static GameObject Floor(Transform g, string name, float x, float z0, float z1, float width, float top, Material mat)
        {
            var go = Block(g, name, new Vector3(x, top - 0.5f, (z0 + z1) * 0.5f), new Vector3(width, 1f, z1 - z0), mat);
            // Yellow/black edge trim so you can read where the drop is
            float len = z1 - z0, zc = (z0 + z1) * 0.5f;
            foreach (float side in new[] { -1f, 1f })
            {
                Block(g, name + "_Edge", new Vector3(x + side * (width * 0.5f + 0.03f), top - 0.3f, zc), new Vector3(0.06f, 0.6f, len), stripe, false);
                int n = Mathf.Max(1, Mathf.FloorToInt(len / 1.2f));
                for (int i = 0; i < n; i += 2)
                    Block(g, name + "_EdgeBlack", new Vector3(x + side * (width * 0.5f + 0.05f), top - 0.3f, z0 + (i + 0.5f) * len / n),
                        new Vector3(0.06f, 0.6f, len / n), black, false);
            }
            return go;
        }

        static void DressCrusher(Transform block)
        {
            // Children inherit the block's scale (6.5 x 2 x 4), so sizes here are fractions of it.
            var yellow = stripe;
            for (int k = 0; k < 10; k++)
            {
                var m = k % 2 == 0 ? yellow : black;
                foreach (float z in new[] { -0.505f, 0.505f })
                    Shapes.Make(PrimitiveType.Cube, "Stripe", block, new Vector3(-0.45f + k * 0.1f, -0.38f, z), new Vector3(0.1f, 0.2f, 0.01f), m, false,
                        new Vector3(0f, 0f, 25f));
            }
            var steel = Mats.Get("Gun_Steel", new Color(0.55f, 0.57f, 0.62f));
            for (int k = 0; k < 6; k++)
                LowPoly.Make(LowPoly.Cone(4), "Tooth", block, new Vector3(-0.42f + k * 0.168f, -0.58f, 0f), new Vector3(0.06f, 0.18f, 0.5f), steel,
                    new Vector3(180f, 0f, 0f));
            LowPoly.Make(LowPoly.Cylinder(8), "Piston", block, new Vector3(0f, 1.5f, 0f), new Vector3(0.09f, 2f, 0.14f), steel);
        }


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
            var bulb = Mats.Unlit("CheckpointBulb", new Color(0.6f, 0.95f, 1f));
            for (float x = -width * 0.5f; x <= width * 0.5f + 0.01f; x += 1.5f)
                LowPoly.Make(LowPoly.Ico(0), "Bulb", g, pos + new Vector3(x, 4.45f, -0.22f), Vector3.one * 0.28f, bulb);
            Props.Sign(g, "CHECKPOINT " + (index + 1), pos + new Vector3(0f, 5.2f, -0.25f), 0.12f, new Color(0.6f, 0.95f, 1f));
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
