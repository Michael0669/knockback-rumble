using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Builds the two arena events, far away from the Gauntlet so they can all live in one scene:
    ///
    ///   Cash Grab ("The Vault", x = +400): open square arena with no rails, a raised
    ///   money dais in the middle, cover walls and pillars, bumpers, and ammo crates.
    ///
    ///   Crumble Plaza (x = -400): a 10x10 grid of tiles over a sewage pit that falls
    ///   apart during the event, plus the Losers' Lounge where knocked-out dummies watch.
    ///
    /// Re-run "Knockback Rumble > Build Show Scene" after changing numbers here.
    /// If an older scene doesn't have these arenas, PrototypeBootstrap builds them at Play.
    /// </summary>
    public static class ArenaBuilder
    {
        public static readonly Vector3 CashGrabOrigin = new Vector3(400f, 0f, 0f);
        public static readonly Vector3 CrumbleOrigin = new Vector3(-400f, 0f, 0f);

        // =====================================================================
        // Cash Grab
        // =====================================================================

        public static CourseInfo BuildCashGrab(Transform parent)
        {
            var floor = Mats.Get("Vault_Floor", new Color(0.32f, 0.36f, 0.34f));
            var floorDark = Mats.Get("Floor_Dark", Palette.FloorDark);
            var metal = Mats.Get("Metal", Palette.Metal);
            var gold = Mats.Get("Gold", Palette.Gold);
            var cashGreen = Mats.Get("Cash_Green", new Color(0.35f, 0.7f, 0.35f));
            var stripe = Mats.Get("Stripe", Palette.Stripe);
            var bumper = Mats.Get("Bumper", Palette.Bumper);
            var pit = Mats.Get("Pit", Palette.Pit);

            var root = new GameObject("CashGrab").transform;
            if (parent != null) root.SetParent(parent, false);
            root.position = CashGrabOrigin;
            var info = root.gameObject.AddComponent<CourseInfo>();
            info.courseName = "Cash Grab";
            info.kind = EventKind.CashGrab;
            info.killY = -12f;
            info.center = CashGrabOrigin;
            info.radius = 20f;

            // Main floor: 40 x 40 of checkered marble, no rails. The edge is the point.
            Block(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), floor);
            var marbleA = Mats.Get("Marble_White", new Color(0.86f, 0.84f, 0.8f));
            var marbleB = Mats.Get("Marble_Green", new Color(0.18f, 0.38f, 0.3f));
            for (int tx = 0; tx < 10; tx++)
                for (int tz = 0; tz < 10; tz++)
                    Block(root, "Tile", new Vector3(-18f + tx * 4f, 0.004f, -18f + tz * 4f), new Vector3(3.96f, 0.01f, 3.96f),
                        (tx + tz) % 2 == 0 ? marbleA : marbleB, false);
            Block(root, "FloorLip", new Vector3(0f, -0.12f, 0f), new Vector3(40.04f, 0.2f, 40.04f), gold, false);
            for (int i = 0; i < 4; i++)
                Kit.IBeam(root, Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(-19f, -1.5f, -19f),
                    Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(19f, -1.5f, -19f), 1f, Kit.Girder);
            // Hazard striping around the rim
            Block(root, "EdgeN", new Vector3(0f, 0.012f, 19.6f), new Vector3(40f, 0.01f, 0.8f), stripe, false);
            Block(root, "EdgeS", new Vector3(0f, 0.012f, -19.6f), new Vector3(40f, 0.01f, 0.8f), stripe, false);
            Block(root, "EdgeE", new Vector3(19.6f, 0.012f, 0f), new Vector3(0.8f, 0.01f, 40f), stripe, false);
            Block(root, "EdgeW", new Vector3(-19.6f, 0.012f, 0f), new Vector3(0.8f, 0.01f, 40f), stripe, false);

            // Money dais in the middle with a ramp on each side
            Block(root, "Dais", new Vector3(0f, 0.75f, 0f), new Vector3(10f, 1.5f, 10f), Mats.Get("Vault_Steel", new Color(0.45f, 0.48f, 0.52f)));
            Block(root, "DaisTop", new Vector3(0f, 1.505f, 0f), new Vector3(9.2f, 0.01f, 9.2f), Mats.Get("Carpet_Red", new Color(0.7f, 0.08f, 0.12f)), false);
            Block(root, "DaisTrim", new Vector3(0f, 1.42f, 0f), new Vector3(10.1f, 0.12f, 10.1f), gold, false); // top sits just under the carpet (no flicker)
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, i * 90f, 0f);
                // Rivets along each side
                for (int k = -4; k <= 4; k++)
                    LowPoly.Make(LowPoly.Ico(0), "Rivet", root, rot * new Vector3(k * 1.05f, 0.75f, 5.02f), Vector3.one * 0.18f, gold);
            }
            for (int i = 0; i < 4; i++)
            {
                float yaw = i * 90f;
                var rot = Quaternion.Euler(0f, yaw, 0f);
                // Ramp from the floor (5 m out) up to the dais edge.
                float rise = 1.5f, run = 4f;
                float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
                float len = Mathf.Sqrt(rise * rise + run * run);
                float a = angle * Mathf.Deg2Rad;
                Vector3 mid = rot * new Vector3(0f, 0.75f - 0.5f * Mathf.Cos(a), 7f - 0.5f * Mathf.Sin(a));
                Block(root, "Ramp_" + i, mid, new Vector3(4f, 1f, len), floorDark, true, new Vector3(angle, yaw, 0f));
            }

            // Cover: four low walls and four tall pillars
            Vector3[] walls = { new Vector3(10f, 0f, 0f), new Vector3(-10f, 0f, 0f), new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, -11f) };
            for (int i = 0; i < walls.Length; i++)
            {
                bool alongX = i >= 2;
                // Cover is a giant stack of cash: green bricks with paper bands
                var wall = Block(root, "CoverWall_" + i, walls[i] + Vector3.up, alongX ? new Vector3(6f, 2f, 1f) : new Vector3(1f, 2f, 6f), cashGreen);
                for (int k = -1; k <= 1; k++)
                    Shapes.Make(PrimitiveType.Cube, "Band", wall.transform, alongX ? new Vector3(k * 0.33f, 0f, 0f) : new Vector3(0f, 0f, k * 0.33f),
                        alongX ? new Vector3(0.06f, 1.01f, 1.04f) : new Vector3(1.04f, 1.01f, 0.06f), Mats.Get("Cash_Band", new Color(0.95f, 0.92f, 0.8f)), false);
                for (int k = 0; k < 3; k++)
                    Shapes.Make(PrimitiveType.Cube, "Layer", wall.transform, new Vector3(0f, -0.33f + k * 0.33f, 0f), new Vector3(1.01f, 0.015f, 1.01f),
                        Mats.Get("Cash_Dark", new Color(0.22f, 0.45f, 0.25f)), false);
                Props.Sign(wall.transform, "$", alongX ? new Vector3(0.5f, 0f, -0.52f) : new Vector3(-0.52f, 0f, 0.5f), 0.12f, new Color(0.9f, 0.95f, 0.8f));
            }
            Vector3[] pillars = { new Vector3(13f, 0f, 13f), new Vector3(-13f, 0f, 13f), new Vector3(13f, 0f, -13f), new Vector3(-13f, 0f, -13f) };
            foreach (var p in pillars)
            {
                // Marble columns with gold caps (an invisible box keeps the collision simple)
                var col = Block(root, "Pillar", p + Vector3.up * 3f, new Vector3(2.2f, 6f, 2.2f), metal);
                col.GetComponent<Renderer>().enabled = false;
                LowPoly.Make(LowPoly.Cylinder(12), "Column", root, p + Vector3.up * 3f, new Vector3(2.1f, 5.4f, 2.1f), marbleA);
                LowPoly.Make(LowPoly.Cylinder(12), "Base", root, p + Vector3.up * 0.25f, new Vector3(2.7f, 0.5f, 2.7f), gold);
                LowPoly.Make(LowPoly.Cylinder(12), "Capital", root, p + Vector3.up * 5.9f, new Vector3(2.7f, 0.5f, 2.7f), gold);
                for (int k = 0; k < 12; k++)
                {
                    float fa = k * 30f * Mathf.Deg2Rad;
                    Block(root, "Flute", p + new Vector3(Mathf.Cos(fa) * 1.02f, 3f, Mathf.Sin(fa) * 1.02f), new Vector3(0.12f, 5f, 0.12f), marbleB, false,
                        new Vector3(0f, -k * 30f, 0f));
                }
            }

            // Bumpers between the pillars and the edge
            Vector3[] bumpers = { new Vector3(16f, 0f, 4f), new Vector3(-16f, 0f, -4f), new Vector3(4f, 0f, -16f), new Vector3(-4f, 0f, 16f) };
            foreach (var p in bumpers)
            {
                var b = Shapes.Make(PrimitiveType.Cylinder, "Bumper", root, p + Vector3.up, new Vector3(1.6f, 1f, 1.6f), bumper);
                b.AddComponent<Bumper>();
                Props.DressBumper(b.transform);
            }

            // Ammo crates
            Pickup(root, new Vector3(16f, 0f, -9f));
            Pickup(root, new Vector3(-16f, 0f, 9f));
            Pickup(root, new Vector3(9f, 0f, 16f));
            Pickup(root, new Vector3(-9f, 0f, -16f));

            // Where bags appear (world positions)
            var bagSpots = new List<Vector3>
            {
                new Vector3(0f, 1.5f, 0f), new Vector3(14f, 0f, 0f), new Vector3(-14f, 0f, 0f), new Vector3(0f, 0f, 15f),
                new Vector3(0f, 0f, -15f), new Vector3(8f, 0f, 8f), new Vector3(-8f, 0f, -8f), new Vector3(8f, 0f, -8f), new Vector3(-8f, 0f, 8f),
            };
            for (int i = 0; i < bagSpots.Count; i++) bagSpots[i] += CashGrabOrigin;
            info.bagSpawns = bagSpots.ToArray();

            // Spawns: two rings around the middle
            var spawns = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                spawns.Add(CashGrabOrigin + new Vector3(Mathf.Sin(ang) * 15.5f, 0.05f, Mathf.Cos(ang) * 15.5f));
            }
            for (int i = 0; i < 8; i++)
            {
                float ang = ((i + 0.5f) / 8f) * Mathf.PI * 2f;
                spawns.Add(CashGrabOrigin + new Vector3(Mathf.Sin(ang) * 17.5f, 0.05f, Mathf.Cos(ang) * 17.5f));
            }
            info.spawnPoints = spawns.ToArray();

            // Scenery: the pit, a giant vault door, and piles of cash
            Block(root, "Pit", new Vector3(0f, -14f, 0f), new Vector3(160f, 1f, 160f), pit, false);
            var door = Shapes.Make(PrimitiveType.Cylinder, "VaultDoor", root, new Vector3(0f, 9f, 34f), new Vector3(20f, 0.8f, 20f), gold, false,
                new Vector3(90f, 0f, 0f));
            Shapes.Make(PrimitiveType.Cylinder, "VaultHub", door.transform, new Vector3(0f, -0.6f, 0f), new Vector3(0.3f, 0.6f, 0.3f), metal, false);
            for (int i = 0; i < 3; i++)
                Block(root, "VaultSpoke_" + i, new Vector3(0f, 9f, 33f), new Vector3(12f, 0.6f, 0.6f), metal, false, new Vector3(0f, 0f, i * 60f));
            var rng = new System.Random(420);
            for (int i = 0; i < 26; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = 30f + (float)rng.NextDouble() * 22f;
                float h = 2f + (float)rng.NextDouble() * 8f;
                Vector3 p = new Vector3(Mathf.Sin(ang) * dist, -13.5f + h * 0.5f, Mathf.Cos(ang) * dist);
                if (p.z > 24f && Mathf.Abs(p.x) < 14f) continue; // keep the vault door clear
                Block(root, "CashStack", p, new Vector3(3f, h, 1.6f), cashGreen, false, new Vector3(0f, (float)rng.NextDouble() * 90f, 0f));
            }

            // Gold bars stacked around the dais, giant coins, and the crowd
            var goldBar = Mats.Get("Gold", Palette.Gold);
            for (int i = 0; i < 4; i++)
            {
                var corner = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 1.5f, 6.4f);
                for (int k = 0; k < 3; k++)
                    Block(root, "GoldBar", corner + new Vector3(0f, 0.15f + k * 0.3f, 0f), new Vector3(1.2f - k * 0.3f, 0.3f, 0.6f), goldBar, false,
                        new Vector3(0f, 45f + i * 90f, 0f));
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var pivot = new GameObject("GiantCoin").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(Mathf.Sin(a) * 30f, -2f + (i % 2) * 3f, Mathf.Cos(a) * 30f);
                pivot.localEulerAngles = new Vector3(0f, i * 60f, 0f);
                pivot.gameObject.AddComponent<Spinner>().degreesPerSecond = 20f + i * 3f;
                LowPoly.Make(LowPoly.Cylinder(14), "Coin", pivot, Vector3.zero, new Vector3(5f, 0.6f, 5f), goldBar, new Vector3(90f, 0f, 0f));
                Props.Sign(pivot, "$", new Vector3(0f, 0f, -0.35f), 0.4f, new Color(0.6f, 0.45f, 0.1f));
            }
            var srng = new System.Random(33);
            Props.Grandstand(root, new Vector3(0f, -1f, -32f), 0f, 40f, 5, srng, 13f);
            Props.Grandstand(root, new Vector3(-32f, -1f, 0f), 90f, 40f, 5, srng, 13f);
            Props.Grandstand(root, new Vector3(32f, -1f, 0f), -90f, 40f, 5, srng, 13f);
            foreach (var c in new[] { new Vector3(-24f, -14f, -24f), new Vector3(24f, -14f, -24f), new Vector3(-24f, -14f, 24f), new Vector3(24f, -14f, 24f) })
                Props.LightTower(root, c, 30f, Vector3.zero);
            Props.Backdrop(root, Vector3.zero, 60f, 11, "PAYDAY LOANZ");

            Sign(root, "CASH GRAB", new Vector3(0f, 15f, 31f), 0.6f, Palette.Gold);
            Sign(root, "sponsored by PAYDAY LOANZ 4 U  (only 690% APR!)", new Vector3(0f, 12.6f, 31f), 0.16f, Color.white);
            return info;
        }

        // =====================================================================
        // Crumble Plaza
        // =====================================================================

        public static CourseInfo BuildCrumble(Transform parent)
        {
            const int grid = 10;
            const float tile = 3f;
            var tileA = Mats.Get("Crumble_TileA", new Color(0.62f, 0.56f, 0.48f));
            var tileB = Mats.Get("Crumble_TileB", new Color(0.5f, 0.45f, 0.4f));
            var metal = Mats.Get("Metal", Palette.Metal);
            var rust = Mats.Get("Rust", Palette.Rust);
            var sewage = Mats.Get("Sewage", new Color(0.35f, 0.42f, 0.18f));
            var couch = Mats.Get("Couch", new Color(0.55f, 0.25f, 0.45f));
            var gold = Mats.Get("Gold", Palette.Gold);
            var white = Mats.Get("White", Palette.White);
            var crackMat = Mats.Get("Crumble_Crack", new Color(0.2f, 0.17f, 0.15f));

            var root = new GameObject("CrumblePlaza").transform;
            if (parent != null) root.SetParent(parent, false);
            root.position = CrumbleOrigin;
            var info = root.gameObject.AddComponent<CourseInfo>();
            info.courseName = "Crumble Plaza";
            info.kind = EventKind.Crumble;
            info.killY = -12f;
            info.center = CrumbleOrigin;
            info.radius = grid * tile * 0.5f;

            // The tiles
            var tilesRoot = new GameObject("Tiles").transform;
            tilesRoot.SetParent(root, false);
            var tiles = new List<Transform>();
            float half = grid * tile * 0.5f;
            for (int x = 0; x < grid; x++)
                for (int z = 0; z < grid; z++)
                {
                    Vector3 p = new Vector3(-half + tile * (x + 0.5f), -0.5f, -half + tile * (z + 0.5f));
                    var t = Block(tilesRoot, "Tile_" + x + "_" + z, p, new Vector3(tile - 0.1f, 1f, tile - 0.1f), (x + z) % 2 == 0 ? tileA : tileB);
                    tiles.Add(t.transform);
                    // Cracks (children, so they fall with the tile)
                    int cracks = 1 + (x * 7 + z * 3) % 3;
                    for (int c = 0; c < cracks; c++)
                        Block(t.transform, "Crack", new Vector3(((x * 13 + c * 7) % 5 - 2) * 0.12f, 0.505f, ((z * 11 + c * 5) % 5 - 2) * 0.12f),
                            new Vector3(0.45f, 0.01f, 0.03f), crackMat, false, new Vector3(0f, (x * 37 + z * 53 + c * 71) % 180, 0f));
                }
            var floor = root.gameObject.AddComponent<CrumbleFloor>();
            floor.tiles = tiles.ToArray();

            // Spawns on the middle tiles
            var spawns = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                spawns.Add(CrumbleOrigin + new Vector3(Mathf.Sin(ang) * 6f, 0.05f, Mathf.Cos(ang) * 6f));
            }
            for (int i = 0; i < 8; i++)
            {
                float ang = ((i + 0.5f) / 8f) * Mathf.PI * 2f;
                spawns.Add(CrumbleOrigin + new Vector3(Mathf.Sin(ang) * 10f, 0.05f, Mathf.Cos(ang) * 10f));
            }
            info.spawnPoints = spawns.ToArray();

            // A few ammo crates (they float in place when their tile drops: free bait)
            Pickup(root, new Vector3(9f, 0f, 9f));
            Pickup(root, new Vector3(-9f, 0f, -9f));
            Pickup(root, new Vector3(9f, 0f, -9f));
            Pickup(root, new Vector3(-9f, 0f, 9f));

            // Sewage pit below
            Block(root, "Sewage", new Vector3(0f, -14f, 0f), new Vector3(160f, 1f, 160f), sewage, false);
            var rng = new System.Random(1337);
            for (int i = 0; i < 30; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = 26f + (float)rng.NextDouble() * 26f;
                float h = 3f + (float)rng.NextDouble() * 10f;
                var pp = new Vector3(Mathf.Sin(ang) * dist, -13.5f + h * 0.5f, Mathf.Cos(ang) * dist);
                LowPoly.Make(LowPoly.Cylinder(10), "Pipe", root, pp, new Vector3(2f, h, 2f), i % 2 == 0 ? rust : metal);
                LowPoly.Make(LowPoly.Cylinder(10), "PipeCap", root, pp + Vector3.up * h * 0.5f, new Vector3(2.4f, 0.4f, 2.4f), Kit.Girder);
                if (i % 3 == 0) Props.Drum(root, pp + Vector3.up * (h * 0.5f + 0.2f), new Color(0.35f, 0.42f, 0.18f));
            }

            // The Losers' Lounge: a fenced balcony overlooking the plaza.
            Vector3 lounge = new Vector3(0f, 8f, half + 9f);
            Block(root, "Lounge_Floor", lounge + Vector3.down * 0.5f, new Vector3(18f, 1f, 8f), metal);
            Block(root, "Lounge_Carpet", lounge + new Vector3(0f, 0.006f, 0f), new Vector3(17f, 0.01f, 7f), Mats.Get("Lounge_Carpet", new Color(0.35f, 0.15f, 0.4f)), false);
            Kit.RailingAlong(root, lounge + new Vector3(-9f, 0f, -4f), lounge + new Vector3(9f, 0f, -4f));
            Kit.RailingAlong(root, lounge + new Vector3(-9f, 0f, -4f), lounge + new Vector3(-9f, 0f, 4f));
            Kit.RailingAlong(root, lounge + new Vector3(9f, 0f, -4f), lounge + new Vector3(9f, 0f, 4f));
            Block(root, "Lounge_RailBack", lounge + new Vector3(0f, 1.5f, 4.2f), new Vector3(18f, 3f, 0.4f), Mats.Get("Lounge_Wall", new Color(0.25f, 0.2f, 0.3f)));
            Block(root, "Lounge_Neon", lounge + new Vector3(0f, 2.4f, 3.98f), new Vector3(9f, 1.2f, 0.04f), Mats.Unlit("Lounge_Neon", new Color(0.12f, 0.08f, 0.2f)), false);
            for (int i = 0; i < 3; i++)
            {
                Vector3 c = lounge + new Vector3(-6f + i * 6f, 0f, 2.6f);
                Block(root, "Couch_Seat", c + Vector3.up * 0.3f, new Vector3(3.4f, 0.6f, 1.4f), couch);
                Block(root, "Couch_Back", c + new Vector3(0f, 0.9f, 0.6f), new Vector3(3.4f, 1.2f, 0.4f), couch);
                Block(root, "Couch_ArmL", c + new Vector3(-1.6f, 0.65f, 0f), new Vector3(0.3f, 0.7f, 1.4f), couch, false);
                Block(root, "Couch_ArmR", c + new Vector3(1.6f, 0.65f, 0f), new Vector3(0.3f, 0.7f, 1.4f), couch, false);
                for (int k = 0; k < 2; k++)
                    Block(root, "Cushion", c + new Vector3(-0.75f + k * 1.5f, 0.66f, -0.05f), new Vector3(1.4f, 0.14f, 1.2f),
                        Mats.Get("Couch_Cushion", new Color(0.65f, 0.35f, 0.55f)), false);
            }
            Block(root, "SnackTable", lounge + new Vector3(0f, 0.4f, -1f), new Vector3(2f, 0.8f, 1f), white);
            LowPoly.Make(LowPoly.Cylinder(10), "SadSnacks", root, lounge + new Vector3(0f, 0.9f, -1f), new Vector3(0.8f, 0.2f, 0.8f),
                Mats.Get("Snack_Bowl", new Color(0.9f, 0.5f, 0.2f)));
            Kit.IBeam(root, new Vector3(-6f, -14f, half + 9f), new Vector3(-6f, 7f, half + 9f), 1f, Kit.Girder);
            Kit.IBeam(root, new Vector3(6f, -14f, half + 9f), new Vector3(6f, 7f, half + 9f), 1f, Kit.Girder);
            info.loungePoint = CrumbleOrigin + lounge + new Vector3(0f, 0.1f, -0.5f);
            Sign(root, "LOSERS' LOUNGE", lounge + new Vector3(0f, 2.4f, 3.9f), 0.25f, Palette.Gold);
            Sign(root, "complimentary sad snacks", lounge + new Vector3(0f, 1.6f, 3.9f), 0.1f, Color.white);

            var crng = new System.Random(77);
            Props.Grandstand(root, new Vector3(0f, -1f, -half - 12f), 0f, 36f, 5, crng, 13f);
            Props.Grandstand(root, new Vector3(-half - 12f, -1f, 0f), 90f, 36f, 5, crng, 13f);
            Props.Grandstand(root, new Vector3(half + 12f, -1f, 0f), -90f, 36f, 5, crng, 13f);
            foreach (var c in new[] { new Vector3(-22f, -14f, -22f), new Vector3(22f, -14f, -22f), new Vector3(-22f, -14f, 22f), new Vector3(22f, -14f, 22f) })
                Props.LightTower(root, c, 28f, Vector3.zero);
            Props.Backdrop(root, Vector3.zero, 55f, 23, "LOWEST BIDDER CONSTRUCTION");

            Sign(root, "CRUMBLE PLAZA", new Vector3(0f, 18f, half + 14f), 0.6f, Palette.Gold);
            Sign(root, "built by the lowest bidder", new Vector3(0f, 15.6f, half + 14f), 0.16f, Color.white);
            return info;
        }

        // =====================================================================
        // Kit
        // =====================================================================

        static GameObject Block(Transform g, string name, Vector3 localPos, Vector3 size, Material mat, bool collider = true,
            Vector3? euler = null) =>
            Shapes.Make(PrimitiveType.Cube, name, g, localPos, size, mat, collider, euler);

        static void Pickup(Transform g, Vector3 localPos)
        {
            var go = new GameObject("Pickup_ammo");
            go.transform.SetParent(g, false);
            go.transform.localPosition = localPos;
            go.AddComponent<WeaponPickup>().Setup("ammo");
        }

        static void Sign(Transform g, string text, Vector3 localPos, float size, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(g, false);
            go.transform.localPosition = localPos;
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
