using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// BOUNCY CASTLE: the whole floor is a bouncy castle - you bounce every time you land.
    /// Everyone gets a Pop Pistol with unlimited ammo. The castle slowly deflates (shrinks), and
    /// falling off means you're out. Each machine bounces its own dummies.
    /// </summary>
    public class BouncyCastleGame : FrenzyGame
    {
        const float StartRadius = 14f, EndRadius = 6f, Bounce = 9.5f;

        public override EventKind Kind => EventKind.BouncyCastle;
        public override string Title => "BOUNCY CASTLE";
        public override string Goal => "Bounce, shoot, survive. The castle is deflating.";
        public override string Objective => "The floor is a bouncy castle and you never stop bouncing. Unlimited Pop Pistols. It's slowly deflating - fall off and you're out!";
        public override string Blurb => "A shrinking bouncy castle and unlimited Pop Pistols. One life.";
        public override string[,] Cards => new[,]
        {
            { "BOING", "you bounce on landing" },
            { "POP PISTOL", "unlimited ammo" },
            { "FALL = OUT", "one life only" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override int StandingPerSecond => 5;
        protected override WeaponDef Gun => WeaponDef.PopPistol;
        protected override int KoBonus => 50;

        Transform floor, rim;
        readonly System.Collections.Generic.Dictionary<DummyMotor, bool> wasGrounded = new System.Collections.Generic.Dictionary<DummyMotor, bool>();

        public override void Build(Transform root, CourseInfo info)
        {
            var red = MiniKit.M("Castle_Red", new Color(0.95f, 0.3f, 0.3f));
            var blue = MiniKit.M("Castle_Blue", new Color(0.3f, 0.55f, 0.95f));
            var yellow = MiniKit.M("Castle_Yellow", new Color(1f, 0.85f, 0.25f));
            var f = MiniKit.Disc(root, "Bouncy", new Vector3(0f, -0.5f, 0f), StartRadius, 1f, blue, 36);
            // Stripes and puffy rim ride on the floor so they shrink with it
            for (int i = 0; i < 8; i++)
                Shapes.Make(PrimitiveType.Cube, "Stripe", f.transform, new Vector3(0f, 0.505f, 0f), new Vector3(0.06f, 0.01f, 1f), i % 2 == 0 ? red : yellow, false,
                    new Vector3(0f, i * 22.5f, 0f));
            var rimGo = new GameObject("Puffs").transform;
            rimGo.SetParent(root, false);
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2f / 28f;
                LowPoly.Make(LowPoly.Ico(1), "Puff", rimGo, new Vector3(Mathf.Sin(a), 0.2f, Mathf.Cos(a)), new Vector3(0.12f, 0.6f, 0.12f), i % 2 == 0 ? red : yellow);
            }
            rimGo.localScale = new Vector3(StartRadius, 1f, StartRadius);
            // Inflatable turrets far out (scenery)
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, -4f, StartRadius + 4f);
                LowPoly.Make(LowPoly.Cylinder(12), "Turret", root, p, new Vector3(4f, 12f, 4f), i % 2 == 0 ? red : yellow);
                LowPoly.Make(LowPoly.Cone(12), "Roof", root, p + Vector3.up * 7.5f, new Vector3(4.6f, 3f, 4.6f), blue);
            }
            LowPoly.Make(LowPoly.Cone(30), "Underside", root, new Vector3(0f, -5f, 0f), new Vector3(StartRadius * 2f, 8f, StartRadius * 2f),
                MiniKit.M("Castle_Under", new Color(0.2f, 0.3f, 0.5f)), new Vector3(180f, 0f, 0f));

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 10f);
            info.center = Origin;
            info.radius = StartRadius;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, 24f));
            MiniKit.Dress(root, 17f, 2828, "BOUNCY CASTLE", null, null, new Color(0.3f, 0.35f, 0.5f));
        }

        float RadiusAt(float t) => Mathf.Lerp(StartRadius, EndRadius, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Duration * 0.15f, Duration * 0.9f, t)));
        float Radius => RadiusAt(Mathf.Max(0f, Clock));

        public override void Begin()
        {
            floor = Course.transform.Find("Bouncy");
            rim = Course.transform.Find("Puffs");
            wasGrounded.Clear();
            Apply(StartRadius);
        }

        public override void End() => Apply(StartRadius);

        void Apply(float r)
        {
            if (floor != null) floor.localScale = new Vector3(r * 2f, 1f, r * 2f);
            if (rim != null) rim.localScale = new Vector3(r, 1f, r);
        }

        public override void Tick()
        {
            base.Tick();
            Apply(Radius);
            if (Clock <= 0f) return;
            foreach (var c in LocalAlive())
            {
                var m = c.Motor;
                bool g = m.Grounded;
                wasGrounded.TryGetValue(m, out bool before);
                wasGrounded[m] = g;
                if (!g || before) continue;
                // Just landed: boing! (Jumping right as you land goes higher.)
                Vector3 p = c.transform.position;
                if (Shapes.Flat(p - Origin).magnitude > Radius + 0.5f) continue;
                m.Launch(Vector3.up * Bounce, 0f);
                if (c.IsPlayer || NearCamera(p, 20f)) Sfx.At("boing", p, 0.5f, 0.15f, 25f);
            }
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill) => ChaseClosest(me, pos, Radius * 0.6f);
    }
}
