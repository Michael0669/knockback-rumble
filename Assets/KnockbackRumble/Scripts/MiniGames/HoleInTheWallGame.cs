using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// HOLE IN THE WALL: walls slide across the stage toward the edge. Get through a hole or get
    /// shoved off. Fewer holes and faster walls as it goes. The walls are built from the round's
    /// seed and move on the shared clock; each machine pushes its own dummies.
    /// </summary>
    public class HoleInTheWallGame : MiniGame
    {
        const float HalfX = 11f, Front = -8f, Back = 8f, StartZ = 13f, EndZ = -16f, WallH = 3.4f, Thick = 0.7f, HoleW = 2.7f;
        const int WallCash = 40;

        class Wall
        {
            public float t0, speed;
            public float[] holes;
            public Transform view;
            public bool paid;
        }

        public override EventKind Kind => EventKind.HoleInTheWall;
        public override string Title => "HOLE IN THE WALL";
        public override string Goal => "Fit through the holes or get pushed off.";
        public override string Objective => "Walls slide across the stage. Get through a hole before one shoves you off the edge. Fall off and you're out!";
        public override string Blurb => "Find the hole in the wall or get shoved off the edge.";
        public override string[,] Cards => new[,]
        {
            { "FIND", "the hole" },
            { "FALL = OUT", "one life only" },
            { "+$" + WallCash, "every wall you survive" },
        };
        public override float Duration => 110f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override string PrizeLabel => "SURVIVED A WALL";

        readonly List<Wall> walls = new List<Wall>();
        static readonly Color[] WallColors =
        {
            new Color(0.95f, 0.35f, 0.4f), new Color(0.3f, 0.7f, 0.95f), new Color(0.45f, 0.85f, 0.35f), new Color(1f, 0.75f, 0.2f), new Color(0.75f, 0.45f, 0.95f),
        };

        public override void Build(Transform root, CourseInfo info)
        {
            var stage = MiniKit.M("Wall_Stage", new Color(0.3f, 0.3f, 0.38f));
            var stageB = MiniKit.M("Wall_StageB", new Color(0.38f, 0.38f, 0.48f));
            MiniKit.Block(root, "Stage", new Vector3(0f, -0.5f, 0f), new Vector3(HalfX * 2f, 1f, Back - Front), stage);
            for (int i = 0; i < 8; i++)
                MiniKit.Block(root, "Lane", new Vector3(0f, 0.006f, Front + (i + 0.5f) * (Back - Front) / 8f), new Vector3(HalfX * 2f, 0.01f, (Back - Front) / 16f), stageB, false);
            MiniKit.Block(root, "EdgeFront", new Vector3(0f, 0.012f, Front + 0.4f), new Vector3(HalfX * 2f, 0.01f, 0.8f), MiniKit.M("Stripe", Palette.Stripe), false);
            // The track the walls run on, out past the back of the stage
            MiniKit.Block(root, "Rails", new Vector3(-HalfX - 0.4f, -0.2f, 2f), new Vector3(0.5f, 0.4f, 30f), Kit.Steel, false);
            MiniKit.Block(root, "Rails", new Vector3(HalfX + 0.4f, -0.2f, 2f), new Vector3(0.5f, 0.4f, 30f), Kit.Steel, false);
            MiniKit.Block(root, "BackDeck", new Vector3(0f, -0.5f, Back + 4f), new Vector3(HalfX * 2f, 1f, 8f), MiniKit.M("Wall_Back", new Color(0.2f, 0.2f, 0.25f)), false);
            Kit.IBeam(root, new Vector3(-HalfX, -1.5f, Front), new Vector3(-HalfX, -1.5f, Back + 8f), 1f, Kit.Girder);
            Kit.IBeam(root, new Vector3(HalfX, -1.5f, Front), new Vector3(HalfX, -1.5f, Back + 8f), 1f, Kit.Girder);
            // Big "wall factory" doorway behind
            MiniKit.Block(root, "Factory", new Vector3(0f, 4f, Back + 9f), new Vector3(HalfX * 2f + 6f, 10f, 2f), MiniKit.M("Wall_Factory", new Color(0.35f, 0.3f, 0.3f)), false);
            MiniKit.Block(root, "FactoryDoor", new Vector3(0f, 2f, Back + 7.9f), new Vector3(HalfX * 2f, 4f, 0.2f), MiniKit.M("Dummy_Black", Palette.Black), false);
            MiniKit.Sign(root, "HOLE IN THE WALL", new Vector3(0f, 7.5f, Back + 7.8f), 0.4f, Palette.Gold);

            info.spawnPoints = MiniKit.StartGrid(Origin, 2f, HalfX * 1.6f);
            info.center = W(0f, 0f, 7f); // spawns face the incoming walls
            info.radius = 9f;
            MiniKit.Lounge(root, info, new Vector3(0f, 6f, -22f), 180f);
            MiniKit.Dress(root, 17f, 1212, "", null, "BRICKS & GIGGLES", new Color(0.3f, 0.25f, 0.3f));
        }

        public override void Begin()
        {
            walls.Clear();
            float t = 3f;
            for (int k = 0; t < Duration; k++)
            {
                var w = new Wall { t0 = t, speed = Mathf.Min(7f, 3.2f + k * 0.28f) };
                int holes = k < 3 ? 3 : k < 7 ? 2 : 1;
                var list = new List<float>();
                for (int tries = 0; list.Count < holes && tries < 40; tries++)
                {
                    float x = (Rand(k * 50 + tries, 3) * 2f - 1f) * (HalfX - 2f);
                    bool ok = true;
                    foreach (var h in list) if (Mathf.Abs(h - x) < HoleW + 1f) ok = false;
                    if (ok) list.Add(x);
                }
                list.Sort();
                w.holes = list.ToArray();
                w.view = BuildWall(w, WallColors[k % WallColors.Length]);
                walls.Add(w);
                t += Mathf.Lerp(9f, 5f, Mathf.Clamp01(k / 12f)) + (StartZ - Back) / w.speed * 0.3f;
            }
        }

        Transform BuildWall(Wall w, Color color)
        {
            var root = new GameObject("Wall").transform;
            root.SetParent(Course.transform, false);
            var mat = Mats.Get("Wall_" + ColorUtility.ToHtmlStringRGB(color), color);
            var frame = Mats.Get("Wall_Frame", new Color(0.95f, 0.95f, 0.9f));
            float x = -HalfX;
            foreach (var h in w.holes)
            {
                float a = h - HoleW * 0.5f;
                if (a > x + 0.05f) Shapes.Make(PrimitiveType.Cube, "Seg", root, new Vector3((x + a) * 0.5f, WallH * 0.5f, 0f), new Vector3(a - x, WallH, Thick), mat, false);
                // A dummy-shaped outline above each hole, for the joke
                LowPoly.Make(LowPoly.Ico(0), "HeadHole", root, new Vector3(h, WallH + 0.5f, 0f), new Vector3(0.9f, 0.9f, Thick), mat);
                Shapes.Make(PrimitiveType.Cube, "Lintel", root, new Vector3(h, WallH - 0.05f, -Thick * 0.5f - 0.02f), new Vector3(HoleW, 0.1f, 0.04f), frame, false);
                x = h + HoleW * 0.5f;
            }
            if (x < HalfX - 0.05f) Shapes.Make(PrimitiveType.Cube, "Seg", root, new Vector3((x + HalfX) * 0.5f, WallH * 0.5f, 0f), new Vector3(HalfX - x, WallH, Thick), mat, false);
            root.gameObject.SetActive(false);
            return root;
        }

        public override void End()
        {
            foreach (var w in walls) if (w.view != null) Object.Destroy(w.view.gameObject);
            walls.Clear();
        }

        float WallZ(Wall w, float t) => StartZ - w.speed * (t - w.t0);

        bool InHole(Wall w, float x)
        {
            foreach (var h in w.holes) if (Mathf.Abs(x - h) < HoleW * 0.5f - 0.38f) return true;
            return false;
        }

        public override void Tick()
        {
            float t = Clock;
            foreach (var w in walls)
            {
                float z = WallZ(w, t);
                bool live = t >= w.t0 && z > EndZ;
                if (w.view != null)
                {
                    if (w.view.gameObject.activeSelf != live) w.view.gameObject.SetActive(live);
                    if (live) w.view.position = W(0f, 0f, z);
                }
                if (!live) continue;

                // Push our own dummies that are in the way.
                float wz = Origin.z + z;
                foreach (var c in LocalAlive())
                {
                    Vector3 p = c.transform.position;
                    if (p.y - Origin.y > WallH || p.y < Origin.y - 3f) continue;
                    if (Mathf.Abs(p.z - wz) > Thick * 0.5f + 0.45f) continue;
                    if (InHole(w, p.x - Origin.x)) continue;
                    var rb = c.Motor.Body;
                    if (rb == null) continue;
                    bool inFront = p.z <= wz;
                    float targetZ = inFront ? wz - Thick * 0.5f - 0.46f : wz + Thick * 0.5f + 0.46f;
                    rb.position = new Vector3(rb.position.x, rb.position.y, targetZ);
                    var v = rb.linearVelocity;
                    if (inFront && v.z > -w.speed) v.z = -w.speed - 0.5f;
                    rb.linearVelocity = v;
                }
            }
        }

        public override void HostSecond(RaceManager race)
        {
            float t = Clock;
            foreach (var w in walls)
            {
                if (w.paid || WallZ(w, t) > Front - 1f) continue;
                w.paid = true;
                foreach (var c in Alive()) Award(c, WallCash);
            }
        }

        Wall NextWall(float z)
        {
            float t = Clock;
            Wall best = null;
            float bestZ = float.MaxValue;
            foreach (var w in walls)
            {
                if (t < w.t0 - 2f) continue;
                float wz = Origin.z + WallZ(w, t);
                if (wz < z - 0.2f || wz > bestZ) continue;
                best = w;
                bestZ = wz;
            }
            return best;
        }

        public override string Status(RaceManager race)
        {
            int passed = 0;
            foreach (var w in walls) if (w.paid || WallZ(w, Clock) < Front - 1f) passed++;
            return "STILL IN: " + race.AliveCount + "     WALLS SURVIVED: " + passed;
        }

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Red;
            if (player == null || player.Eliminated) return null;
            var w = NextWall(player.transform.position.z);
            if (w == null) return null;
            float dz = Origin.z + WallZ(w, Clock) - player.transform.position.z;
            return dz < 4f && dz > 0f && !InHole(w, player.transform.position.x - Origin.x) ? "FIND THE HOLE!" : null;
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            var w = NextWall(pos.z);
            if (w == null || w.holes.Length == 0) return W(0f, 0f, 1f);
            float wz = Origin.z + WallZ(w, Clock);
            float bestX = 0f, bestD = float.MaxValue;
            foreach (var h in w.holes)
            {
                float d = Mathf.Abs(Origin.x + h - pos.x);
                if (d < bestD) { bestD = d; bestX = Origin.x + h; }
            }
            float dz = wz - pos.z;
            // Line up with the hole early (good bots) or late (bad ones), then step through it.
            float lineUp = Mathf.Lerp(3f, 9f, skill);
            if (dz > lineUp) return new Vector3(pos.x, pos.y, Origin.z + 1f);
            return new Vector3(bestX, pos.y, Mathf.Abs(pos.x - bestX) < 0.5f ? wz + 2f : Mathf.Min(pos.z, wz - 1.5f));
        }
    }
}
