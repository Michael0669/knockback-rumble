using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// SPIN CYCLE: you're inside a giant washing machine. Two sweeper bars spin around the drum,
    /// in opposite directions, faster and faster. Jump them or get swept out. One life.
    /// The bars' angles are a formula of the shared clock, so they're identical everywhere.
    /// </summary>
    public class SpinCycleGame : MiniGame
    {
        const float Radius = 13f, HubRadius = 1.7f, BarHeight = 0.7f;

        struct Bar
        {
            public float a0, w0, acc;
            public Transform pivot;
        }

        public override EventKind Kind => EventKind.SpinCycle;
        public override string Title => "SPIN CYCLE";
        public override string Goal => "Jump the spinning bars. Don't get swept off.";
        public override string Objective => "Two giant bars sweep the drum, faster and faster. Jump over them! Fall off and you're out.";
        public override string Blurb => "Jump the sweeper bars inside a giant washing machine. They speed up.";
        public override string[,] Cards => new[,]
        {
            { "JUMP", "over the bars" },
            { "FALL = OUT", "one life only" },
            { "$5 / SEC", "while you're still in" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override int StandingPerSecond => 5;

        readonly Bar[] bars =
        {
            new Bar { a0 = 0f, w0 = 55f, acc = 1.0f },
            new Bar { a0 = 180f, w0 = -38f, acc = -0.8f },
        };
        readonly HitCooldown cooldown = new HitCooldown();

        public override void Build(Transform root, CourseInfo info)
        {
            var drum = MiniKit.M("Spin_Drum", new Color(0.7f, 0.74f, 0.8f));
            var drumDark = MiniKit.M("Spin_DrumDark", new Color(0.45f, 0.48f, 0.55f));
            var hub = MiniKit.M("Spin_Hub", new Color(0.25f, 0.55f, 0.85f));
            MiniKit.Disc(root, "Drum", new Vector3(0f, -0.5f, 0f), Radius, 1f, drum, 40);
            // Drum holes in rings
            for (int ringI = 0; ringI < 4; ringI++)
            {
                float r = 3.5f + ringI * 2.6f;
                int n = 10 + ringI * 6;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + ringI * 0.5f) / n * Mathf.PI * 2f;
                    LowPoly.Make(LowPoly.Cylinder(8), "Hole", root, new Vector3(Mathf.Sin(a) * r, 0.006f, Mathf.Cos(a) * r), new Vector3(0.45f, 0.01f, 0.45f), drumDark);
                }
            }
            MiniKit.StripeRim(root, Vector3.zero, Radius, 0.015f, 40);
            LowPoly.Make(LowPoly.Cone(30), "Underside", root, new Vector3(0f, -5f, 0f), new Vector3(Radius * 2f, 8f, Radius * 2f), drumDark, new Vector3(180f, 0f, 0f));

            // The hub (agitator) in the middle: solid, so nobody hides inside it
            var hubGo = MiniKit.Disc(root, "Hub", new Vector3(0f, 1.2f, 0f), HubRadius, 2.4f, hub, 16);
            for (int i = 0; i < 6; i++)
                Shapes.Make(PrimitiveType.Cube, "Fin", hubGo.transform, Quaternion.Euler(0f, i * 60f, 0f) * new Vector3(0f, 0f, 0.5f),
                    new Vector3(0.12f, 0.9f, 0.25f), MiniKit.M("Spin_Fin", new Color(0.85f, 0.9f, 0.95f)), false, new Vector3(0f, i * 60f, 0f));

            // The bars (turned at runtime)
            var stripeA = MiniKit.M("Stripe", Palette.Stripe);
            var stripeB = MiniKit.M("Dummy_Black", Palette.Black);
            for (int b = 0; b < 2; b++)
            {
                var pivot = new GameObject("BarPivot" + b).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(0f, BarHeight, 0f);
                pivot.localEulerAngles = new Vector3(0f, b * 180f, 0f);
                float len = Radius - HubRadius + 0.3f;
                int segs = 9;
                for (int s = 0; s < segs; s++)
                {
                    float z = HubRadius - 0.1f + (s + 0.5f) * len / segs;
                    Shapes.Make(PrimitiveType.Cube, "Seg", pivot, new Vector3(0f, 0f, z), new Vector3(0.45f, 0.45f, len / segs), (s + b) % 2 == 0 ? stripeA : stripeB, false);
                }
                LowPoly.Make(LowPoly.Ico(1), "Cap", pivot, new Vector3(0f, 0f, Radius + 0.2f), Vector3.one * 0.8f, b == 0 ? MiniKit.M("Hazard_Red", Palette.Hazard) : hub);
                // A giant sock flapping off each bar, because laundry
                LowPoly.Make(LowPoly.Cylinder(8), "Sock", pivot, new Vector3(0f, 0.55f, Radius * 0.6f), new Vector3(0.7f, 1.2f, 0.7f),
                    MiniKit.M(b == 0 ? "Spin_SockA" : "Spin_SockB", b == 0 ? new Color(0.95f, 0.4f, 0.6f) : new Color(0.4f, 0.9f, 0.45f)), new Vector3(0f, 0f, 20f));
            }

            // A giant washing-machine door hanging behind it all (scenery)
            LowPoly.Make(LowPoly.Torus(40, 6, 0.12f), "Door", root, new Vector3(0f, 12f, 30f), new Vector3(34f, 3f, 34f), MiniKit.M("Spin_DoorRim", new Color(0.9f, 0.9f, 0.92f)),
                new Vector3(90f, 0f, 0f));
            MiniKit.Sign(root, "DELICATES", new Vector3(-12f, 3f, 25f), 0.2f, Color.white);
            MiniKit.Sign(root, "HEAVY DUTY", new Vector3(12f, 3f, 25f), 0.2f, Color.white);

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 9f);
            info.center = Origin;
            info.radius = Radius;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, -24f), 180f);
            MiniKit.Dress(root, 17f, 707, "SPIN CYCLE", "40 degrees, no fabric softener", "SUDZ", new Color(0.2f, 0.3f, 0.4f));
        }

        float AngleOf(int b, float t)
        {
            t = Mathf.Max(0f, t);
            return bars[b].a0 + bars[b].w0 * t + 0.5f * bars[b].acc * t * t;
        }

        float SpeedOf(int b, float t) => bars[b].w0 + bars[b].acc * Mathf.Max(0f, t);

        public override void Begin()
        {
            for (int b = 0; b < bars.Length; b++) bars[b].pivot = Course.transform.Find("BarPivot" + b);
        }

        public override void Tick()
        {
            float t = Clock;
            Vector3 center = Origin + Vector3.up * BarHeight;
            for (int b = 0; b < bars.Length; b++)
            {
                float ang = AngleOf(b, t);
                if (bars[b].pivot != null) bars[b].pivot.rotation = Quaternion.Euler(0f, ang, 0f);
                if (t <= 0f) continue;

                Vector3 axis = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                float w = SpeedOf(b, t);
                foreach (var body in DentBody.All)
                {
                    if (body == null || !body.IsLocal) continue;
                    Vector3 bp = body.transform.position;
                    if (bp.y > center.y + 0.45f || bp.y < center.y - 2.2f) continue;
                    Vector3 local = Shapes.Flat(bp - center);
                    float along = Vector3.Dot(local, axis);
                    if (along < HubRadius - 0.4f || along > Radius + 0.6f) continue;
                    if ((local - axis * along).magnitude > 0.75f) continue;
                    if (!cooldown.TryHit(body, 0.5f)) continue;
                    Vector3 tangent = Vector3.Cross(Vector3.up, axis) * Mathf.Sign(w);
                    float speedBoost = Mathf.Clamp01(Mathf.Abs(w) / 150f);
                    body.Hit(tangent + axis * 0.3f, 13f + 8f * speedBoost, 12f, null, "Spin Bar", 0.4f);
                    Sfx.At("hit_heavy", bp + Vector3.up, 0.8f, 0.1f, 40f);
                }
            }
        }

        public override string Status(RaceManager race)
        {
            float rpm = Mathf.Abs(SpeedOf(0, Clock)) / 6f;
            return "STILL IN: " + race.AliveCount + "     SPIN: " + Mathf.RoundToInt(rpm) + " RPM";
        }

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            int bucket = Mathf.FloorToInt(Time.time / 3f) + me.NetId * 7;
            float a = (bucket * 2.399f) % (Mathf.PI * 2f);
            float d = 4f + 5f * Mathf.Repeat(bucket * 0.618f, 1f);
            return Origin + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * d;
        }

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            var motor = me.Motor;
            if (!motor.Grounded || motor.Stunned) return;
            float t = Clock;
            Vector3 local = Shapes.Flat(me.transform.position - Origin);
            float phi = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            for (int b = 0; b < bars.Length; b++)
            {
                float ang = AngleOf(b, t), w = SpeedOf(b, t);
                if (Mathf.Abs(w) < 1f) continue;
                float gap = w > 0f ? Mathf.Repeat(phi - ang, 360f) : Mathf.Repeat(ang - phi, 360f);
                float timeToHit = gap / Mathf.Abs(w);
                // Each bot has its own timing; worse bots sometimes jump way too early.
                float rev = Mathf.Floor(ang / 360f);
                float jumpAt = 0.2f + (1f - skill) * 0.55f * Mathf.Repeat(me.NetId * 0.37f + rev * 0.61f + b * 0.29f, 1f);
                if (timeToHit < jumpAt && timeToHit > jumpAt - 0.1f)
                {
                    motor.QueueJump();
                    return;
                }
            }
        }
    }
}
