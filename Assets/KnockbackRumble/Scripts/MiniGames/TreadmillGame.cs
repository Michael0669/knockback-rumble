using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// TREADMILL OF DOOM: a giant treadmill drags everyone toward the grinder at the back, faster
    /// and faster, while junk rides the belt at you. Run, jump the low stuff, dodge the tall stuff.
    /// Belt speed and junk come from the shared clock and seed; each machine pushes its own dummies.
    /// </summary>
    public class TreadmillGame : MiniGame
    {
        const float HalfX = 7f, HalfZ = 17f, StartSpeed = 3f, EndSpeed = 7.6f, SpawnZ = 16f, GoneZ = -21f;

        struct Junk
        {
            public float t0, x;
            public byte type; // 0 crate (jump it), 1 barrel (jump it), 2 tall stack (dodge it)
        }

        static readonly Vector3[] Sizes = { new Vector3(1.3f, 1.2f, 1.3f), new Vector3(1.8f, 1.1f, 1.1f), new Vector3(1.8f, 2.8f, 1.4f) };

        public override EventKind Kind => EventKind.Treadmill;
        public override string Title => "TREADMILL OF DOOM";
        public override string Goal => "Keep running. The treadmill won't stop.";
        public override string Objective => "A giant treadmill drags everyone toward the grinder. Run against it, jump the low junk, dodge the tall junk. Fall off the back and you're out!";
        public override string Blurb => "A giant treadmill drags you toward the grinder. Keep running.";
        public override string[,] Cards => new[,]
        {
            { "RUN", "against the belt" },
            { "FALL = OUT", "one life only" },
            { "$5 / SEC", "while you're still in" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override int StandingPerSecond => 5;

        Conveyor belt;
        Junk[] junk;
        Transform[] views;
        Transform grinderA, grinderB;
        readonly HitCooldown bump = new HitCooldown();

        public override void Build(Transform root, CourseInfo info)
        {
            var beltMat = MiniKit.M("Belt", Palette.Belt);
            var slat = MiniKit.M("Tread_Slat", new Color(0.3f, 0.3f, 0.33f));
            var frame = MiniKit.M("Tread_Frame", new Color(0.85f, 0.2f, 0.2f));
            var go = MiniKit.Block(root, "Belt", new Vector3(0f, -0.5f, 0f), new Vector3(HalfX * 2f, 1f, HalfZ * 2f), beltMat);
            var conveyor = go.AddComponent<Conveyor>();
            conveyor.velocity = Vector3.zero;
            var stripes = new GameObject("Stripes").transform;
            stripes.SetParent(root, false);
            stripes.localPosition = new Vector3(0f, 0.01f, 0f);
            for (int i = 0; i < 17; i++)
                Shapes.Make(PrimitiveType.Cube, "Slat", stripes, new Vector3(0f, 0f, -HalfZ + i * 2f + 1f), new Vector3(HalfX * 2f - 0.2f, 0.02f, 0.5f), slat, false);
            conveyor.stripes = stripes;
            conveyor.stripeLength = HalfZ * 2f;

            // Side rails (you can't fall off the sides) and the front console
            MiniKit.Block(root, "RailL", new Vector3(-HalfX - 0.4f, 0.5f, 0f), new Vector3(0.8f, 1f, HalfZ * 2f), frame);
            MiniKit.Block(root, "RailR", new Vector3(HalfX + 0.4f, 0.5f, 0f), new Vector3(0.8f, 1f, HalfZ * 2f), frame);
            MiniKit.Block(root, "Console", new Vector3(0f, 2f, HalfZ + 1f), new Vector3(HalfX * 2f + 1.6f, 4f, 2f), frame);
            MiniKit.Block(root, "Screen", new Vector3(0f, 3f, HalfZ - 0.02f), new Vector3(8f, 1.6f, 0.05f), Mats.Unlit("Tread_Screen", new Color(0.1f, 0.25f, 0.15f)), false);
            MiniKit.Sign(root, "INCLINE: YES", new Vector3(0f, 3f, HalfZ - 0.06f), 0.13f, new Color(0.4f, 1f, 0.5f));
            for (int s = -1; s <= 1; s += 2)
                LowPoly.Make(LowPoly.Cylinder(10), "Handle", root, new Vector3(s * (HalfX + 0.4f), 2f, HalfZ - 4f), new Vector3(0.3f, 4f, 0.3f), Kit.Steel);
            MiniKit.Block(root, "HandleBar", new Vector3(0f, 4f, HalfZ - 4f), new Vector3(HalfX * 2f + 0.8f, 0.3f, 0.3f), Kit.Steel, false);

            // The grinder at the back
            var spike = MiniKit.M("Tread_Spike", new Color(0.6f, 0.62f, 0.66f));
            for (int g = 0; g < 2; g++)
            {
                var pivot = new GameObject("Grinder" + g).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(0f, -2.2f - g * 0.3f, -HalfZ - 1.6f - g * 1.8f);
                LowPoly.Make(LowPoly.Cylinder(12), "Drum", pivot, Vector3.zero, new Vector3(1.4f, HalfX * 2f, 1.4f), spike, new Vector3(0f, 0f, 90f));
                for (int k = 0; k < 24; k++)
                {
                    float a = k * 47f;
                    LowPoly.Make(LowPoly.Cone(4), "Tooth", pivot, Quaternion.Euler(a, 0f, 0f) * new Vector3(-HalfX + 0.5f + (k % 12) * 1.2f, 0.85f, 0f),
                        new Vector3(0.3f, 0.6f, 0.3f), spike, new Vector3(a, 0f, 0f));
                }
            }
            MiniKit.Sign(root, "DO NOT FALL IN THE GRINDER", new Vector3(0f, 1.6f, -HalfZ - 4f), 0.12f, Palette.Hazard, 180f);

            info.spawnPoints = MiniKit.StartGrid(Origin, 8f, HalfX * 1.5f);
            info.center = W(0f, 0f, HalfZ); // spawns face up the belt
            info.radius = HalfX;
            MiniKit.Lounge(root, info, new Vector3(0f, 6f, -27f), 180f);
            MiniKit.Dress(root, 20f, 1717, "TREADMILL OF DOOM", "you said you'd get fit this year", "GAINZ GYM", new Color(0.2f, 0.2f, 0.25f));
        }

        float SpeedAt(float t) => t <= 0f ? 0f : StartSpeed + (EndSpeed - StartSpeed) * Mathf.Clamp01(t / Duration);
        float DistAt(float t) => t <= 0f ? 0f : StartSpeed * t + 0.5f * (EndSpeed - StartSpeed) * t * t / Duration;

        public override void Begin()
        {
            belt = Course.transform.Find("Belt").GetComponent<Conveyor>();
            grinderA = Course.transform.Find("Grinder0");
            grinderB = Course.transform.Find("Grinder1");
            var list = new List<Junk>();
            float t = 4f;
            for (int k = 0; t < Duration; k++)
            {
                float r = Rand(k, 1);
                list.Add(new Junk { t0 = t, x = (Rand(k, 2) * 2f - 1f) * (HalfX - 1.3f), type = (byte)(r < 0.4f ? 0 : r < 0.75f ? 1 : 2) });
                t += Mathf.Lerp(3f, 1.1f, Mathf.Clamp01(t / Duration)) + Rand(k, 3) * 0.6f;
            }
            junk = list.ToArray();
            views = new Transform[junk.Length];
        }

        public override void End()
        {
            if (belt != null) belt.velocity = Vector3.zero;
            if (views != null)
                foreach (var v in views) if (v != null) Object.Destroy(v.gameObject);
            views = null;
        }

        Transform MakeJunk(int i)
        {
            var size = Sizes[junk[i].type];
            var root = new GameObject("Junk").transform;
            root.SetParent(Course.transform, true);
            var rb = root.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.size = size;
            col.center = new Vector3(0f, size.y * 0.5f, 0f);
            switch (junk[i].type)
            {
                case 0:
                    Shapes.Make(PrimitiveType.Cube, "Crate", root, new Vector3(0f, size.y * 0.5f, 0f), size, Mats.Get("Potato_Crate", new Color(0.55f, 0.38f, 0.2f)), false);
                    break;
                case 1:
                    LowPoly.Make(LowPoly.Cylinder(12), "Barrel", root, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.y, size.x, size.z), Mats.Get("Tread_Barrel", new Color(0.2f, 0.45f, 0.8f)),
                        new Vector3(0f, 0f, 90f));
                    break;
                default:
                    // A wobbly stack of dumbbells and gym bags
                    Shapes.Make(PrimitiveType.Cube, "Bag", root, new Vector3(0f, 0.6f, 0f), new Vector3(size.x, 1.2f, size.z), Mats.Get("Tread_Bag", new Color(0.15f, 0.15f, 0.18f)), false);
                    Shapes.Make(PrimitiveType.Cube, "Bag2", root, new Vector3(0.1f, 1.7f, 0f), new Vector3(size.x * 0.9f, 1f, size.z * 0.9f), Mats.Get("Tread_Bag2", new Color(0.85f, 0.3f, 0.3f)), false);
                    LowPoly.Make(LowPoly.Cylinder(10), "Plate", root, new Vector3(0f, 2.5f, 0f), new Vector3(1.4f, 0.6f, 1.4f), Kit.Steel);
                    break;
            }
            return root;
        }

        public override void Tick()
        {
            float t = Clock;
            float v = SpeedAt(t);
            if (belt != null) belt.velocity = new Vector3(0f, 0f, -v);
            if (grinderA != null) grinderA.Rotate(400f * Time.deltaTime, 0f, 0f, Space.Self);
            if (grinderB != null) grinderB.Rotate(-400f * Time.deltaTime, 0f, 0f, Space.Self);
            if (junk == null) return;

            float dist = DistAt(t);
            for (int i = 0; i < junk.Length; i++)
            {
                if (junk[i].t0 > t) break;
                float z = SpawnZ - (dist - DistAt(junk[i].t0));
                if (z < GoneZ)
                {
                    if (views[i] != null) { Object.Destroy(views[i].gameObject); views[i] = null; }
                    continue;
                }
                if (views[i] == null) views[i] = MakeJunk(i);
                float drop = z < -HalfZ ? (-HalfZ - z) * (-HalfZ - z) * 0.5f : 0f; // tumbles off the end
                views[i].position = W(junk[i].x, -drop, z);
                if (drop > 0f) continue;

                // Shove our own dummies backward if they run into it.
                var size = Sizes[junk[i].type];
                Vector3 c = views[i].position;
                foreach (var m in LocalAlive())
                {
                    Vector3 p = m.transform.position;
                    if (Mathf.Abs(p.x - c.x) > size.x * 0.5f + 0.42f || Mathf.Abs(p.z - c.z) > size.z * 0.5f + 0.42f) continue;
                    if (p.y > c.y + size.y - 0.25f) continue; // standing on top
                    var rb = m.Motor.Body;
                    if (rb == null) continue;
                    float back = c.z - size.z * 0.5f - 0.45f;
                    if (p.z > back + 0.2f) continue; // already past it
                    rb.position = new Vector3(rb.position.x, rb.position.y, Mathf.Min(rb.position.z, back));
                    var vel = rb.linearVelocity;
                    if (vel.z > -v - 1f) vel.z = -v - 1f;
                    rb.linearVelocity = vel;
                    if (bump.TryHit(m.Dent, 0.7f)) Sfx.At("hit_light", p + Vector3.up, 0.6f, 0.1f, 30f);
                }
            }
        }

        public override string Status(RaceManager race) =>
            "STILL IN: " + race.AliveCount + "     SPEED: " + (SpeedAt(Clock) * 3.6f).ToString("0") + " KM/H";

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Red;
            if (player == null || player.Eliminated) return null;
            return player.transform.position.z < Origin.z - HalfZ + 4f ? "RUN! THE GRINDER!" : null;
        }

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            float laneX = Origin.x + ((me.NetId % 5) - 2f) * 2.4f;
            if (junk != null && views != null)
            {
                for (int i = 0; i < junk.Length; i++)
                {
                    if (views[i] == null || junk[i].type != 2) continue;
                    Vector3 c = views[i].position;
                    float ahead = c.z - pos.z;
                    if (ahead < -1f || ahead > 4f + 6f * skill) continue;
                    if (Mathf.Abs(pos.x - c.x) > Sizes[2].x * 0.5f + 1f) continue;
                    // Step around the tall stuff, toward the side with more room.
                    float side = c.x - Origin.x > 0f ? -1f : 1f;
                    laneX = c.x + side * (Sizes[2].x * 0.5f + 1.3f);
                    break;
                }
            }
            return new Vector3(Mathf.Clamp(laneX, Origin.x - HalfX + 1f, Origin.x + HalfX - 1f), pos.y, Origin.z + HalfZ - 3f);
        }

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            if (junk == null || views == null || !me.Motor.Grounded) return;
            Vector3 pos = me.transform.position;
            for (int i = 0; i < junk.Length; i++)
            {
                if (views[i] == null || junk[i].type == 2) continue;
                Vector3 c = views[i].position;
                float ahead = c.z - pos.z;
                if (ahead < 0f || ahead > Mathf.Lerp(1.2f, 2.2f, skill)) continue;
                if (Mathf.Abs(pos.x - c.x) > Sizes[junk[i].type].x * 0.5f + 0.5f) continue;
                me.Motor.QueueJump();
                return;
            }
        }
    }
}
