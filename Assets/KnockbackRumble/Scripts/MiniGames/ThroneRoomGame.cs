using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// THRONE ROOM: king of the hill. A golden toilet sits in a glowing zone; stand in it to earn,
    /// more if you're alone. Every 20 seconds it moves somewhere else. The schedule comes from the
    /// round's seed, so every machine moves it at the same moment with no messages.
    /// </summary>
    public class ThroneRoomGame : MiniGame
    {
        const int SoloCash = 20, SharedCash = 6;
        const float ZoneRadius = 3.6f, Segment = 20f;

        public override EventKind Kind => EventKind.ThroneRoom;
        public override string Title => "THRONE ROOM";
        public override string Goal => "Sit on the golden throne. Alone.";
        public override string Objective => "Stand in the glowing zone around the golden toilet. $" + SoloCash + " a second if you're alone, $" + SharedCash +
                                            " if you're sharing. Every 20 seconds it moves!";
        public override string Blurb => "King of the hill, but the hill is a golden toilet. It moves.";
        public override string[,] Cards => new[,]
        {
            { "$" + SoloCash + " / SEC", "alone on the throne" },
            { "$" + SharedCash + " / SEC", "if you're sharing" },
            { "IT MOVES", "every 20 seconds" },
        };
        public override float Duration => 100f;
        public override string PrizeLabel => "THRONE TIME";

        static readonly Vector3[] Spots =
        {
            new Vector3(0f, 1.5f, 0f), new Vector3(-12f, 0f, -12f), new Vector3(12f, 0f, -12f), new Vector3(12f, 0f, 12f), new Vector3(-12f, 0f, 12f),
        };

        Transform throne, zone;
        Renderer zoneRend;
        Material zoneOn, zoneWarn;
        Vector3 shownSpot;
        int shownSegment = -1;
        float moveT = 1f;
        Vector3 moveFrom;

        // ------------------------------------------------------------------ arena

        public override void Build(Transform root, CourseInfo info)
        {
            var marbleA = MiniKit.M("Marble_White", new Color(0.86f, 0.84f, 0.8f));
            var marbleB = MiniKit.M("Throne_Purple", new Color(0.42f, 0.2f, 0.5f));
            var gold = MiniKit.M("Gold", Palette.Gold);
            var carpet = MiniKit.M("Carpet_Red", new Color(0.7f, 0.08f, 0.12f));
            var steel = MiniKit.M("Vault_Steel", new Color(0.45f, 0.48f, 0.52f));

            MiniKit.Block(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(34f, 1f, 34f), steel);
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 8; z++)
                    MiniKit.Block(root, "Tile", new Vector3(-14.875f + x * 4.25f, 0.004f, -14.875f + z * 4.25f), new Vector3(4.2f, 0.01f, 4.2f),
                        (x + z) % 2 == 0 ? marbleA : marbleB, false);
            MiniKit.Block(root, "Lip", new Vector3(0f, -0.12f, 0f), new Vector3(34.05f, 0.2f, 34.05f), gold, false);
            for (int i = 0; i < 4; i++)
                Kit.IBeam(root, Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(-16f, -1.5f, -16f), Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(16f, -1.5f, -16f),
                    1f, Kit.Girder);

            // The royal dais with four ramps
            MiniKit.Block(root, "Dais", new Vector3(0f, 0.75f, 0f), new Vector3(9f, 1.5f, 9f), steel);
            MiniKit.Block(root, "DaisCarpet", new Vector3(0f, 1.505f, 0f), new Vector3(8.4f, 0.01f, 8.4f), carpet, false);
            MiniKit.Block(root, "DaisTrim", new Vector3(0f, 1.42f, 0f), new Vector3(9.1f, 0.12f, 9.1f), gold, false); // top sits just under the carpet (no flicker)
            for (int i = 0; i < 4; i++)
            {
                float yaw = i * 90f;
                var rot = Quaternion.Euler(0f, yaw, 0f);
                float rise = 1.5f, run = 4f, angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg, len = Mathf.Sqrt(rise * rise + run * run);
                float a = angle * Mathf.Deg2Rad;
                Vector3 mid = rot * new Vector3(0f, 0.75f - 0.5f * Mathf.Cos(a), 6.5f - 0.5f * Mathf.Sin(a));
                MiniKit.Block(root, "Ramp", mid, new Vector3(4f, 1f, len), carpet, true, new Vector3(angle, yaw, 0f));
            }

            // Corner pads where the throne can go
            for (int i = 1; i < Spots.Length; i++)
            {
                MiniKit.Disc(root, "Pad", Spots[i] + new Vector3(0f, -0.45f, 0f), 4f, 1f, gold, 24);
            }

            // Cover: velvet ropes posts and big gold vases
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, i * 90f + 45f, 0f) * new Vector3(0f, 0f, 9f);
                LowPoly.MakeSolid(LowPoly.Ico(1), "Vase", root, p + Vector3.up * 1.1f, new Vector3(1.6f, 2.2f, 1.6f), gold);
                LowPoly.Make(LowPoly.Cylinder(10), "VaseNeck", root, p + Vector3.up * 2.4f, new Vector3(0.8f, 0.6f, 0.8f), gold);
            }
            MiniKit.Pickup(root, new Vector3(15f, 0f, 0f));
            MiniKit.Pickup(root, new Vector3(-15f, 0f, 0f));
            MiniKit.Pickup(root, new Vector3(0f, 0f, 15f));
            MiniKit.Pickup(root, new Vector3(0f, 0f, -15f));

            // The throne itself and its zone (moved at runtime)
            var t = new GameObject("Throne").transform;
            t.SetParent(root, false);
            t.localPosition = Spots[0];
            var bowl = MiniKit.M("Throne_Gold", new Color(1f, 0.82f, 0.25f));
            LowPoly.Make(LowPoly.Cylinder(14), "Pedestal", t, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.5f, 0.9f), bowl);
            LowPoly.Make(LowPoly.Ico(1), "Bowl", t, new Vector3(0f, 0.65f, 0.1f), new Vector3(1.1f, 0.6f, 1.3f), bowl);
            LowPoly.Make(LowPoly.Torus(16, 4, 0.25f), "Seat", t, new Vector3(0f, 0.92f, 0.1f), new Vector3(1.15f, 0.25f, 1.35f), MiniKit.M("Throne_Seat", new Color(0.55f, 0.12f, 0.6f)));
            Shapes.Make(PrimitiveType.Cube, "Tank", t, new Vector3(0f, 1.35f, -0.55f), new Vector3(1.1f, 1.1f, 0.45f), bowl, false);
            Shapes.Make(PrimitiveType.Cube, "Lid", t, new Vector3(0f, 1.95f, -0.55f), new Vector3(1.2f, 0.1f, 0.55f), bowl, false);
            LowPoly.Make(LowPoly.Cylinder(10), "CrownBand", t, new Vector3(0f, 2.15f, -0.55f), new Vector3(0.7f, 0.3f, 0.7f), gold);
            for (int i = 0; i < 5; i++)
                LowPoly.Make(LowPoly.Cone(4), "CrownSpike", t, new Vector3(Mathf.Sin(i * 1.2566f) * 0.3f, 2.45f, -0.55f + Mathf.Cos(i * 1.2566f) * 0.3f),
                    new Vector3(0.18f, 0.35f, 0.18f), gold);
            LowPoly.Make(LowPoly.Cylinder(8), "Beam", t, new Vector3(0f, 10f, 0f), new Vector3(1.2f, 20f, 1.2f), Mats.Unlit("Throne_Beam", new Color(1f, 0.9f, 0.4f, 0.25f)));

            MiniKit.FlatRing(root, "ThroneZone", Spots[0] + Vector3.up * 0.1f, ZoneRadius, Mats.Unlit("Throne_Zone", new Color(1f, 0.85f, 0.25f)), 0.1f);

            info.spawnPoints = MiniKit.TwoRings(Origin, 11f, 14f);
            info.center = Origin;
            info.radius = 16f;
            MiniKit.Dress(root, 18f, 404, "THRONE ROOM", "the golden toilet waits for no one", "ROYAL FLUSH", new Color(0.25f, 0.18f, 0.3f));
        }

        // ------------------------------------------------------------------ flow

        int SpotAt(int segment)
        {
            int s = 0;
            for (int k = 1; k <= segment; k++) s = (s + 1 + Mathf.FloorToInt(Rand(k, 7) * 4f)) % Spots.Length;
            return s;
        }

        Vector3 ZoneWorld => Origin + Spots[SpotAt(Mathf.Max(0, Mathf.FloorToInt(Clock / Segment)))];

        public override void Begin()
        {
            throne = Course.transform.Find("Throne");
            zone = Course.transform.Find("ThroneZone");
            zoneRend = zone != null ? zone.GetComponent<Renderer>() : null;
            zoneOn = Mats.Unlit("Throne_Zone", new Color(1f, 0.85f, 0.25f));
            zoneWarn = Mats.Unlit("Throne_ZoneWarn", new Color(1f, 0.3f, 0.2f));
            shownSegment = -1;
            moveT = 1f;
            Place(0, true);
        }

        void Place(int segment, bool instant)
        {
            shownSegment = segment;
            var target = Origin + Spots[SpotAt(segment)];
            if (instant || throne == null)
            {
                shownSpot = target;
                moveT = 1f;
            }
            else
            {
                moveFrom = throne.position;
                shownSpot = target;
                moveT = 0f;
                Sfx.Ui("streak_ready", 0.6f);
                Puffs.Confetti(target + Vector3.up * 2f, 30);
            }
            if (zone != null) zone.position = shownSpot + Vector3.up * 0.1f;
            if (throne != null && instant) throne.position = shownSpot;
        }

        public override void Tick()
        {
            if (throne == null) return;
            int seg = Mathf.Max(0, Mathf.FloorToInt(Clock / Segment));
            if (Clock > 0f && seg != shownSegment) Place(seg, false);
            if (moveT < 1f)
            {
                moveT = Mathf.Min(1f, moveT + Time.deltaTime * 1.4f);
                float e = UiSkin.EaseOutBack(moveT);
                throne.position = Vector3.LerpUnclamped(moveFrom, shownSpot, e) + Vector3.up * Mathf.Sin(moveT * Mathf.PI) * 6f;
            }
            throne.Rotate(0f, 25f * Time.deltaTime, 0f, Space.World);

            // The zone flashes red for the last 3 seconds before it moves.
            if (zoneRend != null)
            {
                float left = Segment - Mathf.Repeat(Mathf.Max(0f, Clock), Segment);
                bool warn = Clock > 0f && left < 3f && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
                var want = warn ? zoneWarn : zoneOn;
                if (zoneRend.sharedMaterial != want) zoneRend.sharedMaterial = want;
                float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 5f);
                zone.localScale = new Vector3(ZoneRadius * 2f * pulse, 0.15f, ZoneRadius * 2f * pulse);
            }
        }

        List<Contestant> Occupants()
        {
            var list = new List<Contestant>();
            Vector3 z = ZoneWorld;
            foreach (var c in Alive())
            {
                Vector3 d = c.transform.position - z;
                if (Shapes.Flat(d).magnitude <= ZoneRadius && d.y > -0.8f && d.y < 2.5f) list.Add(c);
            }
            return list;
        }

        public override void HostSecond(RaceManager race)
        {
            if (Clock < 0.5f) return;
            var on = Occupants();
            if (on.Count == 1) Award(on[0], SoloCash);
            else foreach (var c in on) Award(c, SharedCash);
        }

        // ------------------------------------------------------------------ HUD

        public override string Status(RaceManager race)
        {
            var on = Occupants();
            float left = Segment - Mathf.Repeat(Mathf.Max(0f, Clock), Segment);
            string moves = "   (MOVES IN " + Mathf.CeilToInt(left) + ")";
            if (on.Count == 0) return "THE THRONE IS EMPTY!" + moves;
            if (on.Count == 1) return "ON THE THRONE: " + on[0].DisplayName.ToUpperInvariant() + moves;
            return "CONTESTED! " + on.Count + " ON THE THRONE" + moves;
        }

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Green;
            if (player == null || player.Eliminated) return null;
            var on = Occupants();
            if (!on.Contains(player)) return null;
            if (on.Count == 1) return "ON THE THRONE!  +$" + SoloCash + "/s";
            color = UiSkin.Gold;
            return "SHOVE THEM OFF YOUR THRONE!";
        }

        public override string StatusFor(Contestant c) => Occupants().Contains(c) ? "ON THE THRONE" : null;

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Everyone piles onto the throne, each to their own spot inside the zone.
            float a = me.NetId * 2.4f;
            return ZoneWorld + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * ZoneRadius * 0.45f;
        }
    }
}
