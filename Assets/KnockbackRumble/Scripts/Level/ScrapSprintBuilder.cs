using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Builds The Gauntlet (the Scrap Sprint course). It runs along +Z:
    ///
    ///   Start gate -> backwards conveyor with bumpers -> CP1
    ///   -> narrow bridge with 3 spiked wrecking balls on crane gantries -> CP2
    ///   -> 4 hover platforms (jump gaps) -> CP3
    ///   -> crusher hall (3 rows of presses)   [rocket-jump shortcut along the shipping containers]
    ///   -> spin bar -> Punch Alley (boxing gloves out of the walls) -> CP4
    ///   -> Piston Pit (rising and sinking pillars) -> Wind Tunnel (fans + swinging plungers) -> CP5
    ///   -> ramp -> finish stage.
    ///
    /// All numbers live here, so changing the layout means editing this file and
    /// re-running "Knockback Rumble > Build Show Scene".
    /// </summary>
    public static class ScrapSprintBuilder
    {
        /// <summary>How much farther the finish is than in the original layout (room for the new sections).</summary>
        const float F = 108f;

        static Material floor, floorDark, belt, stripe, metal, rust, hazard, checkpointMat, gold, bumper, pit, black, white, potty;

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
            BuildSpinBar(Group(root, "6_SpinBar"));
            BuildPunchAlley(Group(root, "7_PunchAlley"));
            BuildPistonPit(Group(root, "8_PistonPit"));
            BuildWindTunnel(Group(root, "9_WindTunnel"));
            BuildFinish(Group(root, "10_Finish"));
            BuildScenery(Group(root, "11_Scenery"));

            info.waypoints = new[]
            {
                new Vector3(0f, 0f, 9f), new Vector3(0f, 0f, 14f), new Vector3(0f, 0f, 22f), new Vector3(0f, 0f, 32f),
                new Vector3(0f, 0f, 41f), new Vector3(0f, 0f, 47f), new Vector3(0f, 0f, 58f), new Vector3(0f, 0f, 68f),
                new Vector3(0f, 0f, 77f), new Vector3(0f, 0f, 81f),
                new Vector3(-1.5f, 0.5f, 89f), new Vector3(1.5f, 1f, 96.5f), new Vector3(-1f, 0.5f, 104f), new Vector3(1f, 0f, 111.5f),
                new Vector3(0f, 0f, 118f), new Vector3(0f, 0f, 124f), new Vector3(0f, 0f, 131.5f), new Vector3(0f, 0f, 138.5f),
                new Vector3(0f, 0f, 146f), new Vector3(3.8f, 0f, 150f), new Vector3(0f, 0f, 155f),
                // Punch Alley, CP4
                new Vector3(0f, 0f, 162f), new Vector3(0f, 0f, 172f), new Vector3(0f, 0f, 182f), new Vector3(0f, 0f, 189f),
                // Piston Pit
                new Vector3(-1.2f, 0f, 196f), new Vector3(1.2f, 0f, 200.5f), new Vector3(-1.2f, 0f, 205f), new Vector3(1.2f, 0f, 209.5f),
                new Vector3(-1.2f, 0f, 214f), new Vector3(1.2f, 0f, 218.5f), new Vector3(0f, 0f, 223.5f),
                // Wind Tunnel, CP5
                new Vector3(-0.8f, 0f, 232f), new Vector3(-0.8f, 0f, 242f), new Vector3(-0.5f, 0f, 252f), new Vector3(0f, 0f, 259f),
                // Ramp and finish
                new Vector3(0f, 0f, 263f), new Vector3(0f, 1.5f, 268f), new Vector3(0f, 3f, 166f + F + 2f), new Vector3(0f, 3f, 174f + F),
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
            Kit.Railing(g, -7.85f, -8f, 10f, 0f);
            Kit.Railing(g, 7.85f, -8f, 10f, 0f);

            // Starting gate: a back wall with a sponsor screen
            Block(g, "BackWall", new Vector3(0f, 2f, -8.5f), new Vector3(16f, 4f, 1f), Kit.Girder);
            var panel = Mats.Unlit("Start_Panel", new Color(0.18f, 0.12f, 0.35f));
            Kit.Box(g, "Screen", new Vector3(0f, 2.2f, -7.98f), new Vector3(14.5f, 3.2f, 0.04f), panel);
            Props.Sign(g, "SHART ENERGY", new Vector3(0f, 2.8f, -7.94f), 0.2f, new Color(0.5f, 1f, 0.4f));
            Props.Sign(g, "now with 40% more shart", new Vector3(0f, 1.6f, -7.94f), 0.09f, Color.white);

            // Painted start line
            for (int i = 0; i < 16; i++)
                Kit.Box(g, "StartLine", new Vector3(-7.5f + i, 0.012f, 2.2f), new Vector3(1f, 0.01f, 0.6f), i % 2 == 0 ? black : white);

            var spawns = new List<Vector3>();
            float[] rows = { 1f, -1.5f, -4f, -6.5f };
            float[] cols = { -6f, -2f, 2f, 6f };
            foreach (float z in rows)
                foreach (float x in cols)
                    spawns.Add(new Vector3(x, 0.05f, z));
            info.spawnPoints = spawns.ToArray();

            // Ammo crates are deliberately scarce (5 on the whole course), so ammo is worth saving.
            Pickup(g, "ammo", new Vector3(0f, 0f, 8f));

            Kit.Arch(g, new Vector3(0f, 0f, 9.3f), 15f, 6f, gold, new Color(1f, 0.85f, 0.4f));
            Props.Sign(g, "THE GAUNTLET", new Vector3(0f, 6.6f, 8.85f), 0.22f, Palette.Gold);
        }

        static void BuildConveyor(Transform g)
        {
            // Backwards belt: you run at 8, it drags you at 3. No side rails.
            var beltGo = Floor(g, "Belt", 0f, 12.5f, 40f, 12f, 0f, belt, false);
            var conveyor = beltGo.AddComponent<Conveyor>();
            conveyor.velocity = new Vector3(0f, 0f, -3f);
            conveyor.stripeLength = 27.5f;

            var stripes = new GameObject("BeltStripes").transform;
            stripes.SetParent(g, false);
            stripes.localPosition = new Vector3(0f, 0.02f, 26.25f);
            for (int i = 0; i < 11; i++)
                Shapes.Make(PrimitiveType.Cube, "Stripe", stripes, new Vector3(0f, 0f, -13.75f + i * 2.5f), new Vector3(11.6f, 0.04f, 0.6f), stripe, false);
            conveyor.stripes = stripes;
            foreach (float z in new[] { 12.5f, 40f })
                LowPoly.Make(LowPoly.Cylinder(12), "Roller", g, new Vector3(0f, -0.5f, z), new Vector3(1.1f, 12.4f, 1.1f), Kit.Girder, new Vector3(0f, 0f, 90f));
            // Belt side frames
            foreach (float side in new[] { -1f, 1f })
                Kit.IBeam(g, new Vector3(side * 6.15f, -0.45f, 12.5f), new Vector3(side * 6.15f, -0.45f, 40f), 0.9f, Kit.SafetyYellow);

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

                // A yellow crane gantry holds the chain
                Vector3 hang = Kit.Gantry(wb, Vector3.zero, 7f, 12f);
                var pivot = new GameObject("Pivot").transform;
                pivot.SetParent(wb, false);
                pivot.localPosition = hang;
                for (int k = 0; k < 14; k++)
                    LowPoly.Make(LowPoly.Torus(10, 4, 0.3f), "Link", pivot, new Vector3(0f, -0.2f - k * 0.62f, 0f), new Vector3(0.45f, 0.45f, 0.8f),
                        Kit.Girder, new Vector3(90f, k % 2 == 0 ? 0f : 90f, 0f));
                var ball = new GameObject("Ball").transform;
                ball.SetParent(pivot, false);
                ball.localPosition = new Vector3(0f, -9.8f, 0f);
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

            // 5x5 hover platforms with 2.5m gaps and small height changes
            Vector3[] plats = { new Vector3(-1.5f, 0.5f, 89f), new Vector3(1.5f, 1f, 96.5f), new Vector3(-1f, 0.5f, 104f), new Vector3(1f, 0f, 111.5f) };
            var thruster = Mats.Unlit("Thruster", new Color(1f, 0.55f, 0.15f));
            for (int i = 0; i < plats.Length; i++)
            {
                var p = plats[i];
                Floor(g, "Platform_" + (i + 1), p.x, p.z - 2.5f, p.z + 2.5f, 5f, p.y, i % 2 == 0 ? rust : metal, true, false);
                // Hover engines underneath
                for (int k = 0; k < 4; k++)
                {
                    var c = new Vector3(p.x + (k % 2 == 0 ? -1.5f : 1.5f), p.y - 1.4f, p.z + (k < 2 ? -1.5f : 1.5f));
                    LowPoly.Make(LowPoly.Cylinder(8), "Engine", g, c, new Vector3(0.9f, 0.8f, 0.9f), Kit.Girder);
                    LowPoly.Make(LowPoly.Cone(8), "Flame", g, c + Vector3.down * 0.75f, new Vector3(0.6f, 0.7f, 0.6f), thruster, new Vector3(180f, 0f, 0f));
                }
            }
        }

        static void BuildCrusherHall(Transform g)
        {
            Floor(g, "CP3_Pad", 0f, 116.5f, 122f, 14f, 0f, floorDark);
            CheckpointGate(g, 2, new Vector3(0f, 0f, 119.5f), 14f);
            Pickup(g, "ammo", new Vector3(-5f, 0f, 120.5f)); // right next to the shortcut

            Floor(g, "HallFloor", 0f, 122f, 148f, 14f, 0f, floor);

            float[] rowZ = { 128f, 135f, 142f };
            float[,] phases = { { 0f, 1.7f }, { 0.85f, 2.55f }, { 1.7f, 0f } };
            for (int r = 0; r < rowZ.Length; r++)
            {
                float z = rowZ[r];
                // Press frame: two I-beam columns and a header beam with stripes
                Kit.IBeam(g, new Vector3(-7.4f, -14f, z), new Vector3(-7.4f, 8.6f, z), 0.8f, Kit.SafetyYellow);
                Kit.IBeam(g, new Vector3(7.4f, -14f, z), new Vector3(7.4f, 8.6f, z), 0.8f, Kit.SafetyYellow);
                Kit.IBeam(g, new Vector3(-7.9f, 8.6f, z), new Vector3(7.9f, 8.6f, z), 0.9f, Kit.SafetyYellow);
                for (int k = -4; k <= 4; k++)
                    Kit.Box(g, "HeaderStripe", new Vector3(k * 1.6f, 8.6f, z), new Vector3(0.8f, 0.92f, 0.84f), black);

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

            // Rocket-jump shortcut: a 5.5m ledge along the left side, made of stacked shipping containers.
            // It skips the presses and drops you off after the hall.
            var ledge = Block(g, "ShortcutLedge", new Vector3(-9.25f, 2.25f, 138f), new Vector3(4.5f, 6.5f, 32f), Kit.Girder);
            ledge.GetComponent<Renderer>().enabled = false;
            Color[] colors = { new Color(0.6f, 0.35f, 0.85f), new Color(0.2f, 0.5f, 0.75f), new Color(0.8f, 0.3f, 0.2f), new Color(0.3f, 0.6f, 0.35f) };
            for (int i = 0; i < 5; i++)
            {
                float z = 125.2f + i * 6.4f;
                // Two containers high, tops flush with the ledge (5.5 m)
                Kit.Container(g, new Vector3(-9.25f, 0.3f, z), 0f, colors[i % colors.Length], 6.2f, i == 0 ? "EXPRESS LANE" : null);
                Kit.Container(g, new Vector3(-9.25f, 2.9f, z), 0f, colors[(i + 2) % colors.Length], 6.2f);
            }
            Kit.IBeam(g, new Vector3(-9.25f, -14f, 128f), new Vector3(-9.25f, 0.3f, 128f), 0.9f, Kit.Girder);
            Kit.IBeam(g, new Vector3(-9.25f, -14f, 148f), new Vector3(-9.25f, 0.3f, 148f), 0.9f, Kit.Girder);
            Props.Sign(g, "EXPRESS LANE", new Vector3(-9.25f, 8f, 121.8f), 0.25f, Palette.Shortcut);
            Props.Sign(g, "(rocket jump. probably fine. - Dr. Lee Ability)", new Vector3(-9.25f, 7f, 121.8f), 0.1f, Color.white);
        }

        static void BuildSpinBar(Transform g)
        {
            Floor(g, "AfterHall", 0f, 148f, 154f, 14f, 0f, floorDark);
            Pickup(g, "ammo", new Vector3(5.5f, 0f, 152.5f));

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
        }

        /// <summary>Punch Alley: a 5 m walkway with giant boxing gloves firing out of the walls on both sides.</summary>
        static void BuildPunchAlley(Transform g)
        {
            Floor(g, "Alley", 0f, 154f, 186f, 5f, 0f, floor);
            var red = Mats.Get("Glove_Red", new Color(0.85f, 0.12f, 0.1f));
            float[] zs = { 160f, 164.5f, 169f, 173.5f, 178f, 182.5f };
            for (int i = 0; i < zs.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;   // alternate walls
                var root = new GameObject("PunchWall_" + i).transform;
                root.SetParent(g, false);
                root.localPosition = new Vector3(side * 3.6f, 1.1f, zs[i]);
                root.localRotation = Quaternion.Euler(0f, side < 0f ? 0f : 180f, 0f); // local +X points at the walkway

                // Housing on a post from the pit
                Kit.Box(root, "Housing", new Vector3(-0.6f, 0f, 0f), new Vector3(1.2f, 2.2f, 2.2f), Kit.SafetyYellow);
                Kit.Box(root, "Mouth", new Vector3(0.01f, 0f, 0f), new Vector3(0.05f, 1.6f, 1.6f), black);
                for (int k = 0; k < 4; k++)
                    Kit.Box(root, "Stripe", new Vector3(-0.6f, 0.9f - k * 0.6f, 1.105f), new Vector3(1.2f, 0.3f, 0.01f), k % 2 == 0 ? black : Kit.SafetyYellow);
                Kit.IBeam(root, new Vector3(-0.6f, -15f, 0f), new Vector3(-0.6f, -1.1f, 0f), 0.6f, Kit.Girder);

                // Spring arm and glove
                var arm = LowPoly.Make(LowPoly.Cylinder(8), "Arm", root, new Vector3(0.2f, 0f, 0f), new Vector3(0.35f, 0.4f, 0.35f), Kit.Steel,
                    new Vector3(0f, 0f, 90f)).transform;
                var glove = new GameObject("Glove").transform;
                glove.SetParent(root, false);
                glove.localPosition = new Vector3(0.4f, 0f, 0f);
                LowPoly.Make(LowPoly.Ico(1), "Fist", glove, new Vector3(0.55f, 0f, 0f), new Vector3(1.2f, 1.1f, 1.25f), red);
                LowPoly.Make(LowPoly.Ico(1), "Thumb", glove, new Vector3(0.45f, 0.15f, 0.6f), new Vector3(0.45f, 0.4f, 0.4f), red);
                LowPoly.Make(LowPoly.Cylinder(10), "Cuff", glove, new Vector3(-0.05f, 0f, 0f), new Vector3(0.95f, 0.35f, 0.95f), white, new Vector3(0f, 0f, 90f));

                var pg = root.gameObject.AddComponent<PunchGlove>();
                pg.glove = glove;
                pg.arm = arm;
                pg.reach = 3.6f;
                pg.period = 2.4f + (i % 3) * 0.3f;
                pg.phase = i * 0.7f;
            }

            // CP4
            Floor(g, "CP4_Pad", 0f, 186f, 192f, 10f, 0f, floorDark);
            CheckpointGate(g, 3, new Vector3(0f, 0f, 189f), 10f);
        }

        /// <summary>Piston Pit: pillars that rise and sink in a wave. Time your jumps.</summary>
        static void BuildPistonPit(Transform g)
        {
            float[] zs = { 196f, 200.5f, 205f, 209.5f, 214f, 218.5f };
            for (int i = 0; i < zs.Length; i++)
            {
                float x = i % 2 == 0 ? -1.2f : 1.2f;
                var pillar = Block(g, "Pillar_" + i, new Vector3(x, -0.6f, zs[i]), new Vector3(3.4f, 1f, 3.4f), i % 2 == 0 ? rust : metal);
                // Painted rim and a piston under it (children move with it; sizes are fractions of the 3.4 x 1 x 3.4 top)
                Kit.Box(pillar.transform, "RimA", new Vector3(0f, 0.505f, 0.44f), new Vector3(0.98f, 0.01f, 0.08f), stripe);
                Kit.Box(pillar.transform, "RimB", new Vector3(0f, 0.505f, -0.44f), new Vector3(0.98f, 0.01f, 0.08f), stripe);
                Kit.Box(pillar.transform, "RimC", new Vector3(0.44f, 0.505f, 0f), new Vector3(0.08f, 0.01f, 0.98f), stripe);
                Kit.Box(pillar.transform, "RimD", new Vector3(-0.44f, 0.505f, 0f), new Vector3(0.08f, 0.01f, 0.98f), stripe);
                LowPoly.Make(LowPoly.Cylinder(10), "Piston", pillar.transform, new Vector3(0f, -4f, 0f), new Vector3(0.35f, 7f, 0.35f), Kit.Steel);
                LowPoly.Make(LowPoly.Cylinder(10), "Sleeve", g, new Vector3(x, -10.5f, zs[i]), new Vector3(1.8f, 7f, 1.8f), Kit.SafetyYellow);
                pillar.AddComponent<Rigidbody>();
                var pp = pillar.AddComponent<PistonPillar>();
                pp.baseY = -0.6f;
                pp.amplitude = 1.3f;
                pp.period = 3.4f;
                pp.phase = i * 0.95f;
            }
            Floor(g, "Landing", 0f, 221.5f, 226f, 8f, 0f, floorDark);
        }

        /// <summary>Wind Tunnel: a narrow bridge, giant fans blowing you toward the edge in gusts, and two swinging plungers.</summary>
        static void BuildWindTunnel(Transform g)
        {
            Floor(g, "WindBridge", 0f, 226f, 256f, 4.5f, 0f, floor);

            var zone = new GameObject("Wind").transform;
            zone.SetParent(g, false);
            zone.localPosition = new Vector3(0f, 1.5f, 241f);
            var wz = zone.gameObject.AddComponent<WindBlower>();
            wz.size = new Vector3(5f, 4f, 30f);
            wz.wind = new Vector3(4.5f, 0f, 0f);
            wz.gustPeriod = 3.5f;

            // Three giant fans on the left, blowing right
            var fans = new List<Transform>();
            var blade = Mats.Get("Fan_Blade", new Color(0.8f, 0.82f, 0.85f));
            foreach (float z in new[] { 231f, 241f, 251f })
            {
                var housing = new GameObject("Fan").transform;
                housing.SetParent(g, false);
                housing.localPosition = new Vector3(-8f, 2.5f, z);
                LowPoly.Make(LowPoly.Torus(20, 5, 0.12f), "Ring", housing, Vector3.zero, new Vector3(6f, 1.6f, 6f), Kit.SafetyYellow,
                    new Vector3(0f, 0f, 90f));
                Kit.IBeam(g, new Vector3(-8f, -14f, z), new Vector3(-8f, -0.5f, z), 0.7f, Kit.Girder);
                var rotor = new GameObject("Rotor").transform;
                rotor.SetParent(housing, false);
                rotor.localRotation = Quaternion.Euler(0f, 0f, -90f); // local Y = wind direction (+X)
                LowPoly.Make(LowPoly.Cylinder(10), "Hub", rotor, Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), Kit.Girder);
                for (int b = 0; b < 4; b++)
                    Kit.Box(rotor, "Blade", Quaternion.Euler(0f, b * 90f, 0f) * new Vector3(1.4f, 0f, 0f), new Vector3(2.4f, 0.06f, 0.8f), blade, false,
                        new Vector3(20f, b * 90f, 0f));
                fans.Add(rotor);
            }
            wz.fans = fans.ToArray();

            // Swinging plungers (Lord Dookington's finest) on gantries
            var wood = Mats.Get("Plunger_Wood", new Color(0.6f, 0.4f, 0.2f));
            var rubber = Mats.Get("Plunger_Rubber", new Color(0.8f, 0.1f, 0.12f));
            float[] pz = { 236f, 247f };
            for (int i = 0; i < pz.Length; i++)
            {
                var wb = new GameObject("Plunger_" + (i + 1)).transform;
                wb.SetParent(g, false);
                wb.localPosition = new Vector3(0f, 0f, pz[i]);
                Vector3 hang = Kit.Gantry(wb, Vector3.zero, 6f, 11f);
                var pivot = new GameObject("Pivot").transform;
                pivot.SetParent(wb, false);
                pivot.localPosition = hang;
                LowPoly.Make(LowPoly.Cylinder(8), "Handle", pivot, new Vector3(0f, -4.5f, 0f), new Vector3(0.5f, 9f, 0.5f), wood);
                var head = new GameObject("Ball").transform;
                head.SetParent(pivot, false);
                head.localPosition = new Vector3(0f, -9.6f, 0f);
                LowPoly.Make(LowPoly.Cone(12), "Cup", head, Vector3.zero, new Vector3(2.6f, 1.6f, 2.6f), rubber);
                LowPoly.Make(LowPoly.Cylinder(12), "Rim", head, new Vector3(0f, -0.75f, 0f), new Vector3(2.7f, 0.15f, 2.7f), rubber);

                var comp = wb.gameObject.AddComponent<WreckingBall>();
                comp.pivot = pivot;
                comp.ball = head;
                comp.ballRadius = 1.4f;
                comp.period = 2.3f;
                comp.phase = 1f + i * 1.9f;
                comp.knockback = 16f;
            }

            // CP5
            Floor(g, "CP5_Pad", 0f, 256f, 262f, 10f, 0f, floorDark);
            CheckpointGate(g, 4, new Vector3(0f, 0f, 259f), 10f);
        }

        static void BuildFinish(Transform g)
        {
            // Ramp from (z 154+F, y 0) up to (z 166+F, y 3)
            float rise = 3f, run = 12f;
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(rise * rise + run * run);
            float a = angle * Mathf.Deg2Rad;
            var ramp = Block(g, "Ramp", new Vector3(0f, 1.5f - 0.5f * Mathf.Cos(a), 160f + F + 0.5f * Mathf.Sin(a)), new Vector3(10f, 1f, len), floor, true,
                new Vector3(-angle, 0f, 0f));
            for (int k = 0; k < 8; k++) // grip strips
                Kit.Box(ramp.transform, "Grip", new Vector3(0f, 0.505f, -0.45f + k * 0.13f), new Vector3(0.96f, 0.01f, 0.03f), Kit.Seam);

            // Finish stage with a red carpet
            Floor(g, "FinishPad", -2.5f, 166f + F, 182f + F, 19f, 3f, floorDark);
            Kit.Box(g, "Carpet", new Vector3(0f, 3.012f, 176f + F), new Vector3(6f, 0.01f, 12f), Mats.Get("Carpet_Red", new Color(0.7f, 0.08f, 0.12f)));
            Block(g, "FinishBackWall", new Vector3(-2.5f, 5f, 182.5f + F), new Vector3(19f, 4f, 1f), Kit.Girder);
            Kit.Box(g, "FinishScreen", new Vector3(-2.5f, 5.2f, 181.98f + F), new Vector3(17.5f, 3.2f, 0.04f), Mats.Unlit("Start_Panel", new Color(0.18f, 0.12f, 0.35f)));
            Props.Sign(g, "WINNERS GET PAID", new Vector3(-2.5f, 5.4f, 181.94f + F), 0.2f, Palette.Gold);
            Kit.Railing(g, 7.15f, 166f + F, 182f + F, 3f);

            var line = new GameObject("FinishLine").transform;
            line.SetParent(g, false);
            line.localPosition = new Vector3(-2.5f, 3.05f, 171f + F);
            var trigger = line.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(19f, 6f, 1.5f);
            trigger.center = new Vector3(0f, 3f, 0f);
            line.gameObject.AddComponent<FinishLine>();

            for (int i = 0; i < 19; i++)
                Kit.Box(g, "Checker", new Vector3(-11.5f + i, 3.015f, 171f + F), new Vector3(1f, 0.01f, 1.2f), i % 2 == 0 ? black : white);
            Kit.Arch(g, new Vector3(-2.5f, 3f, 171f + F), 19.6f, 6f, gold, new Color(1f, 0.85f, 0.4f));
            Props.Sign(g, "FINISH", new Vector3(-2.5f, 9.6f, 170.55f + F), 0.3f, Palette.Gold);

            // Podium
            float[] podium = { 1.2f, 0.8f, 0.5f };
            float[] px = { -2.5f, -4.6f, -0.4f };
            for (int i = 0; i < 3; i++)
            {
                Block(g, "Podium_" + (i + 1), new Vector3(px[i], 3f + podium[i] * 0.5f, 179.5f + F), new Vector3(2f, podium[i], 2f), white);
                Props.Sign(g, (i + 1).ToString(), new Vector3(px[i], 3f + podium[i] * 0.5f, 178.48f + F), 0.12f, Palette.Gold);
            }

            BuildLordDookington(g, new Vector3(4.5f, 3f, 179f + F));
        }

        /// <summary>Lord Dookington III, the show's porta-potty mascot: top hat, monocle, a moon on the door.</summary>
        static void BuildLordDookington(Transform g, Vector3 p)
        {
            var dark = Mats.Get("Potty_Dark", new Color(0.18f, 0.32f, 0.65f));
            Block(g, "LordDookington", p + new Vector3(0f, 1.4f, 0f), new Vector3(1.6f, 2.8f, 1.6f), potty);
            Kit.Box(g, "Roof", p + new Vector3(0f, 2.9f, 0f), new Vector3(1.75f, 0.2f, 1.75f), dark);
            Kit.Box(g, "Door", p + new Vector3(0f, 1.3f, -0.81f), new Vector3(1.2f, 2.3f, 0.03f), dark);
            Kit.Box(g, "Moon", p + new Vector3(0f, 2.15f, -0.83f), new Vector3(0.28f, 0.28f, 0.02f), gold, false, new Vector3(0f, 0f, 45f));
            Kit.Box(g, "Handle", p + new Vector3(0.45f, 1.3f, -0.84f), new Vector3(0.06f, 0.25f, 0.04f), Kit.Steel);
            LowPoly.Make(LowPoly.Cylinder(10), "HatBrim", g, p + new Vector3(0f, 3.05f, 0f), new Vector3(1.7f, 0.1f, 1.7f), black);
            LowPoly.Make(LowPoly.Cylinder(10), "Hat", g, p + new Vector3(0f, 3.55f, 0f), new Vector3(1.05f, 1f, 1.05f), black);
            LowPoly.Make(LowPoly.Cylinder(10), "HatBand", g, p + new Vector3(0f, 3.2f, 0f), new Vector3(1.08f, 0.2f, 1.08f), hazard);
            LowPoly.Make(LowPoly.Torus(12, 4, 0.2f), "Monocle", g, p + new Vector3(-0.35f, 2.15f, -0.86f), new Vector3(0.4f, 0.4f, 0.4f),
                gold, new Vector3(90f, 0f, 0f));
            Props.Sign(g, "LORD DOOKINGTON III", p + new Vector3(0f, 4.4f, -0.1f), 0.08f, Color.white);
        }

        static void BuildScenery(Transform g)
        {
            // The junkyard pit under the course
            Block(g, "ScrapPit", new Vector3(0f, -14f, 150f), new Vector3(240f, 1f, 420f), pit, false);
            var rng = new System.Random(69);
            Color[] drumColors = { new Color(0.75f, 0.2f, 0.18f), new Color(0.2f, 0.45f, 0.75f), new Color(0.85f, 0.7f, 0.2f), new Color(0.3f, 0.55f, 0.3f) };
            for (int i = 0; i < 85; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * (13f + (float)rng.NextDouble() * 16f);
                float z = -20f + (float)rng.NextDouble() * 320f;
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
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var p = new Vector3(side * (24f + (float)rng.NextDouble() * 4f), -13.5f, 10f + i * 24f);
                int count = 3 + rng.Next(3);
                for (int k = 0; k < count; k++)
                    Props.CarWreck(g, p + Vector3.up * k * 1.9f, (float)rng.NextDouble() * 40f - 20f + (k % 2) * 90f,
                        drumColors[rng.Next(drumColors.Length)], 0f);
            }

            // The TV show: grandstands on both sides, light towers, a jumbotron past the finish
            for (int i = 0; i < 5; i++)
            {
                float z = 20f + i * 62f;
                Props.Grandstand(g, new Vector3(-36f, -1f, z), 90f, 44f, 5, rng, 13f);
                Props.Grandstand(g, new Vector3(36f, -1f, z), -90f, 44f, 5, rng, 13f);
            }
            for (int i = 0; i < 6; i++)
            {
                float z = -5f + i * 58f;
                Props.LightTower(g, new Vector3(-20f, -14f, z), 30f, new Vector3(0f, 0f, z + 10f));
                Props.LightTower(g, new Vector3(20f, -14f, z), 30f, new Vector3(0f, 0f, z + 10f));
            }
            Props.Jumbotron(g, new Vector3(-2.5f, 22f, 205f + F), 0f, "KNOCKBACK RUMBLE", "brought to you by SHART ENERGY");
            Props.Backdrop(g, new Vector3(0f, 0f, 145f), 170f, 7, "SHART ENERGY");
        }

        // ---------- Kit pieces ----------

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>A steel deck whose top surface is at "top" (see Kit.Deck).</summary>
        static GameObject Floor(Transform g, string name, float x, float z0, float z1, float width, float top, Material mat,
            bool trim = true, bool girders = true) =>
            Kit.Deck(g, name, x, z0, z1, width, top, mat, trim, girders);

        static void DressCrusher(Transform block)
        {
            // Children inherit the block's scale (6.5 x 2 x 4), so sizes here are fractions of it.
            // Stripes are painted ON the front/back faces (flush), not sticking out.
            for (int k = 0; k < 10; k++)
            {
                var m = k % 2 == 0 ? stripe : black;
                foreach (float z in new[] { -0.501f, 0.501f })
                    Shapes.Make(PrimitiveType.Cube, "Stripe", block, new Vector3(-0.45f + k * 0.1f, -0.36f, z), new Vector3(0.1f, 0.22f, 0.002f), m, false);
            }
            // Press plate and teeth on the underside (inside the block's footprint)
            Shapes.Make(PrimitiveType.Cube, "Plate", block, new Vector3(0f, -0.52f, 0f), new Vector3(0.96f, 0.04f, 0.94f), Kit.Girder, false);
            for (int k = 0; k < 6; k++)
                LowPoly.Make(LowPoly.Cone(4), "Tooth", block, new Vector3(-0.4f + k * 0.16f, -0.6f, 0f), new Vector3(0.05f, 0.12f, 0.4f), Kit.Steel,
                    new Vector3(180f, 0f, 0f));
            LowPoly.Make(LowPoly.Cylinder(8), "Piston", block, new Vector3(0f, 1.5f, 0f), new Vector3(0.09f, 2f, 0.14f), Kit.Steel);
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

            Kit.Arch(g, pos, width + 0.6f, 4.2f, checkpointMat, new Color(0.6f, 0.95f, 1f));
            Props.Sign(g, "CHECKPOINT " + (index + 1), pos + new Vector3(0f, 4.8f, -0.45f), 0.12f, new Color(0.6f, 0.95f, 1f));
        }

        static void Pickup(Transform g, string id, Vector3 pos)
        {
            var go = new GameObject("Pickup_" + id);
            go.transform.SetParent(g, false);
            go.transform.localPosition = pos;
            go.AddComponent<WeaponPickup>().Setup(id);
        }
    }
}
