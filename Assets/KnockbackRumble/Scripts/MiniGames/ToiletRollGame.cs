using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// TOILET ROLL HILL: race up a long hill while giant toilet paper rolls tumble down at you.
    /// Every roll's lane, size, speed and start time come from the round's seed, so they roll
    /// identically on every machine; each machine knocks back its own dummies.
    /// </summary>
    public class ToiletRollGame : MiniGame
    {
        const float RampStart = 4f, RampEnd = 74f, TopY = 14f, Width = 20f, FinishZ = 80f, RollStartZ = 78f, RollEndZ = -12f;

        struct Roll
        {
            public float t0, radius, length, x, speed;
        }

        public override EventKind Kind => EventKind.ToiletRoll;
        public override string Title => "TOILET ROLL HILL";
        public override string Goal => "Race to the top. Dodge the giant toilet rolls.";
        public override string Objective => "Giant toilet paper rolls come tumbling down the hill. Dodge them and get to the top first for the biggest payout!";
        public override string Blurb => "Race up a hill while giant toilet rolls tumble down at you.";
        public override string[,] Cards => new[,]
        {
            { "$" + ShowManager.FinishPayout[0], "for 1st place" },
            { "DODGE", "the giant rolls" },
            { "SHOVE", "people into their path" },
        };
        public override float Duration => 120f;
        public override bool IsRace => true;
        public override bool WeaponsAllowed => false;

        Roll[] rolls;
        Transform[] views;
        readonly HitCooldown cooldown = new HitCooldown();

        static float Surface(float z) => z < RampStart ? 0f : z > RampEnd ? TopY : (z - RampStart) / (RampEnd - RampStart) * TopY;

        public override void Build(Transform root, CourseInfo info)
        {
            var tile = MiniKit.M("Roll_Tile", new Color(0.62f, 0.82f, 0.9f));
            var tileB = MiniKit.M("Roll_TileB", new Color(0.85f, 0.93f, 0.96f));
            var grout = MiniKit.M("Roll_Grout", new Color(0.4f, 0.5f, 0.55f));
            float run = RampEnd - RampStart;
            float angle = Mathf.Atan2(TopY, run) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(run * run + TopY * TopY);
            float a = angle * Mathf.Deg2Rad;

            // Start pad, the hill, and the top
            MiniKit.Block(root, "StartPad", new Vector3(0f, -0.5f, (RampStart - 10f) * 0.5f), new Vector3(Width, 1f, RampStart + 10f), tile);
            var ramp = MiniKit.Block(root, "Hill", new Vector3(0f, TopY * 0.5f - 0.5f * Mathf.Cos(a), (RampStart + RampEnd) * 0.5f + 0.5f * Mathf.Sin(a)),
                new Vector3(Width, 1f, len), tile, true, new Vector3(-angle, 0f, 0f));
            // Bathroom tiles on the hill (stripes across, on the ramp so they tilt with it)
            for (int i = 0; i < 18; i++)
                Shapes.Make(PrimitiveType.Cube, "TileRow", ramp.transform, new Vector3(0f, 0.505f, -0.5f + (i + 0.5f) / 18f), new Vector3(1f, 0.01f, 0.5f / 18f),
                    i % 2 == 0 ? tileB : grout, false);
            MiniKit.Block(root, "Top", new Vector3(0f, TopY - 0.5f, (RampEnd + 98f) * 0.5f), new Vector3(Width, 1f, 98f - RampEnd), tile);
            MiniKit.Block(root, "TopRug", new Vector3(0f, TopY + 0.006f, 88f), new Vector3(10f, 0.01f, 10f), MiniKit.M("Roll_Rug", new Color(0.55f, 0.35f, 0.75f)), false);

            MiniKit.CheckpointGate(root, new Vector3(0f, Surface(40f) + 0.1f, 40f), Width - 1f, 0);
            MiniKit.FinishGate(root, new Vector3(0f, TopY, FinishZ), Width - 1f);

            // The giant toilet at the top that "dispenses" the rolls
            var porcelain = MiniKit.M("Roll_Porcelain", new Color(0.95f, 0.95f, 0.97f));
            LowPoly.Make(LowPoly.Ico(1), "Bowl", root, new Vector3(0f, TopY + 3f, 102f), new Vector3(12f, 6f, 9f), porcelain);
            MiniKit.Block(root, "Tank", new Vector3(0f, TopY + 8f, 107f), new Vector3(12f, 9f, 4f), porcelain, false);
            MiniKit.Block(root, "Handle", new Vector3(-5f, TopY + 11f, 104.9f), new Vector3(2.5f, 0.6f, 0.6f), Kit.Steel, false);
            MiniKit.Sign(root, "TOILET ROLL HILL", new Vector3(0f, TopY + 15f, 106f), 0.5f, Palette.Gold);

            // Spawns, the bot path
            info.spawnPoints = MiniKit.StartGrid(Origin, 0f, 16f);
            var wps = new List<Vector3>();
            for (float z = 2f; z <= 86f; z += 6f) wps.Add(W(0f, Surface(z), z));
            info.waypoints = wps.ToArray();
            info.center = W(0f, 7f, 40f);
            info.radius = 40f;
            info.killY = Origin.y - 14f;

            // Scenery: grandstands along both sides of the hill, the pit, mountains
            var rng = new System.Random(1111);
            MiniKit.Block(root, "Pit", new Vector3(0f, -16f, 45f), new Vector3(140f, 1f, 200f), MiniKit.M("MiniPit_Roll", new Color(0.25f, 0.3f, 0.35f)), false);
            Props.Grandstand(root, new Vector3(-24f, 4f, 40f), 90f, 76f, 5, rng, 18f);
            Props.Grandstand(root, new Vector3(24f, 4f, 40f), -90f, 76f, 5, rng, 18f);
            Props.LightTower(root, new Vector3(-20f, -16f, 0f), 34f, new Vector3(0f, 0f, 30f));
            Props.LightTower(root, new Vector3(20f, -16f, 0f), 34f, new Vector3(0f, 0f, 30f));
            Props.LightTower(root, new Vector3(-20f, -16f, 80f), 40f, new Vector3(0f, 10f, 50f));
            Props.LightTower(root, new Vector3(20f, -16f, 80f), 40f, new Vector3(0f, 10f, 50f));
            Props.Backdrop(root, new Vector3(0f, 0f, 45f), 70f, 1111, "2-PLY OR DIE");
        }

        public override void Begin()
        {
            var list = new List<Roll>();
            float t = 3f;
            for (int i = 0; t < Duration; i++)
            {
                float progress = Mathf.Clamp01(t / Duration);
                list.Add(new Roll
                {
                    t0 = t,
                    radius = 1.2f + Rand(i, 1) * 0.9f,
                    length = 2.4f + Rand(i, 2) * 1f,
                    x = (Rand(i, 3) * 2f - 1f) * (Width * 0.5f - 2f),
                    speed = 8f + Rand(i, 4) * 4f + progress * 3f,
                });
                t += Mathf.Lerp(1.4f, 0.55f, progress) + Rand(i, 5) * 0.4f;
            }
            rolls = list.ToArray();
            views = new Transform[rolls.Length];
        }

        public override void End()
        {
            if (views != null)
                foreach (var v in views) if (v != null) Object.Destroy(v.gameObject);
            views = null;
        }

        bool RollPos(int i, float t, out Vector3 p, out float dist)
        {
            var r = rolls[i];
            dist = (t - r.t0) * r.speed;
            float z = RollStartZ - dist;
            p = W(r.x, Surface(z) + r.radius, z);
            return t >= r.t0 && z > RollEndZ;
        }

        Transform MakeView(int i)
        {
            var r = rolls[i];
            var root = new GameObject("ToiletRoll").transform;
            root.SetParent(Course.transform, true);
            var spin = new GameObject("Spin").transform;
            spin.SetParent(root, false);
            var paper = Mats.Get("Roll_Paper", new Color(0.97f, 0.97f, 0.93f));
            var tube = Mats.Get("Roll_Tube", new Color(0.62f, 0.45f, 0.28f));
            LowPoly.Make(LowPoly.Cylinder(16), "Paper", spin, Vector3.zero, new Vector3(r.radius * 2f, r.length, r.radius * 2f), paper, new Vector3(0f, 0f, 90f));
            LowPoly.Make(LowPoly.Cylinder(12), "Tube", spin, Vector3.zero, new Vector3(r.radius * 0.8f, r.length + 0.04f, r.radius * 0.8f), tube, new Vector3(0f, 0f, 90f));
            // A loose sheet flapping behind it
            Shapes.Make(PrimitiveType.Cube, "Sheet", spin, new Vector3(0f, r.radius, 0f), new Vector3(r.length * 0.8f, 0.02f, 1.6f), paper, false);
            return root;
        }

        public override void Tick()
        {
            if (rolls == null) return;
            float t = Clock;
            for (int i = 0; i < rolls.Length; i++)
            {
                if (rolls[i].t0 > t) break;
                bool live = RollPos(i, t, out Vector3 p, out float dist);
                if (!live)
                {
                    if (views[i] != null) { Object.Destroy(views[i].gameObject); views[i] = null; }
                    continue;
                }
                if (views[i] == null)
                {
                    views[i] = MakeView(i);
                    if (NearCamera(p, 60f)) Sfx.At("tile_fall", p, 0.6f, 0.2f, 60f);
                }
                views[i].position = p;
                var spin = views[i].GetChild(0);
                spin.localRotation = Quaternion.Euler(-dist / rolls[i].radius * Mathf.Rad2Deg, 0f, 0f);

                // Knock back anyone it runs over.
                var r = rolls[i];
                foreach (var body in DentBody.All)
                {
                    if (body == null || !body.IsLocal) continue;
                    Vector3 c = body.Center;
                    if (Mathf.Abs(c.x - p.x) > r.length * 0.5f + 0.4f) continue;
                    float dyz = new Vector2(c.y - p.y, c.z - p.z).magnitude;
                    if (dyz > r.radius + 0.55f) continue;
                    if (!cooldown.TryHit(body, 0.8f)) continue;
                    body.Hit(new Vector3(Mathf.Sign(c.x - p.x) * 0.3f, 0.6f, -1f), 13f + r.speed * 0.4f, 10f, null, "Toilet Roll", 0.4f);
                    Sfx.At("boing", c, 0.8f, 0.1f, 40f);
                }
            }
        }

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            if (rolls == null || me.Finished || me.Motor.Stunned) return;
            Vector3 pos = me.transform.position;
            float t = Clock;
            float look = 5f + 9f * skill;
            for (int i = 0; i < rolls.Length; i++)
            {
                if (rolls[i].t0 > t) break;
                if (!RollPos(i, t, out Vector3 p, out _)) continue;
                float ahead = p.z - pos.z;
                if (ahead < -1f || ahead > look) continue;
                float dx = pos.x - p.x;
                if (Mathf.Abs(dx) > rolls[i].length * 0.5f + 1.3f) continue;
                float side = Mathf.Abs(dx) < 0.1f ? (pos.x - Origin.x > 0f ? -1f : 1f) : Mathf.Sign(dx);
                // Don't dodge off the side of the hill.
                if (Mathf.Abs(pos.x - Origin.x + side * 2f) > Width * 0.5f - 1f) side = -side;
                me.Motor.MoveInput = new Vector3(side, 0f, 0.25f).normalized;
                return;
            }
        }
    }
}
