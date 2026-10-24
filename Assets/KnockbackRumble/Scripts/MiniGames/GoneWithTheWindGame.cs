using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// GONE WITH THE WIND: everyone gets a Leaf Blower (it never runs dry) on a square rooftop
    /// with no railings. Every so often a huge gust blows across the roof from one side. Blow
    /// people off. Fall off and you're out. Gust timing and direction come from the seed and the
    /// shared clock; each machine pushes its own dummies.
    /// </summary>
    public class GoneWithTheWindGame : FrenzyGame
    {
        const float Half = 13f, GustEvery = 11f, GustLength = 3.5f, GustWarn = 2f, GustSpeed = 7.5f;

        public override EventKind Kind => EventKind.GoneWithTheWind;
        public override string Title => "GONE WITH THE WIND";
        public override string Goal => "Leaf blowers for everyone. Blow them off the roof.";
        public override string Objective => "Everyone gets a Leaf Blower that never runs out. Watch out for the big gusts! Fall off and you're out.";
        public override string Blurb => "Leaf blowers on a windy rooftop. Big gusts. One life.";
        public override string[,] Cards => new[,]
        {
            { "LEAF BLOWER", "hold to blow" },
            { "GUSTS", "watch the wind sock" },
            { "FALL = OUT", "one life only" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override int StandingPerSecond => 5;
        protected override WeaponDef Gun => WeaponDef.LeafBlower;

        Transform sock;
        float lastWarn = -99f;

        public override void Build(Transform root, CourseInfo info)
        {
            var roof = MiniKit.M("Wind_Roof", new Color(0.55f, 0.5f, 0.45f));
            var tar = MiniKit.M("Wind_Tar", new Color(0.25f, 0.24f, 0.24f));
            MiniKit.Block(root, "Roof", new Vector3(0f, -0.5f, 0f), new Vector3(Half * 2f, 1f, Half * 2f), roof);
            for (int i = -2; i <= 2; i++)
                MiniKit.Block(root, "TarStrip", new Vector3(i * 5f, 0.006f, 0f), new Vector3(0.3f, 0.01f, Half * 2f), tar, false);
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, i * 90f, 0f);
                MiniKit.Block(root, "Edge", rot * new Vector3(0f, 0.01f, Half - 0.25f), new Vector3(Half * 2f, 0.02f, 0.5f), MiniKit.M("Stripe", Palette.Stripe), false,
                    new Vector3(0f, i * 90f, 0f));
            }
            // Leaves everywhere, a couple of chimneys and a wind sock in the middle
            var leaf = MiniKit.M("Wind_Leaf", new Color(0.85f, 0.45f, 0.15f));
            var leafB = MiniKit.M("Wind_LeafB", new Color(0.7f, 0.6f, 0.15f));
            var rng = new System.Random(2020);
            for (int i = 0; i < 40; i++)
                MiniKit.Block(root, "Leaf", new Vector3((float)rng.NextDouble() * 24f - 12f, 0.01f, (float)rng.NextDouble() * 24f - 12f),
                    new Vector3(0.35f, 0.02f, 0.22f), i % 2 == 0 ? leaf : leafB, false, new Vector3(0f, (float)rng.NextDouble() * 180f, 0f));
            var brick = MiniKit.M("Wind_Brick", new Color(0.6f, 0.3f, 0.22f));
            MiniKit.Block(root, "Chimney", new Vector3(-6f, 1.2f, 6f), new Vector3(1.6f, 2.4f, 1.6f), brick);
            MiniKit.Block(root, "Chimney", new Vector3(6f, 1.2f, -6f), new Vector3(1.6f, 2.4f, 1.6f), brick);
            LowPoly.Make(LowPoly.Cylinder(8), "SockPole", root, new Vector3(0f, 2f, 0f), new Vector3(0.15f, 4f, 0.15f), Kit.Steel);
            var pivot = new GameObject("WindSock").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0f, 3.8f, 0f);
            LowPoly.Make(LowPoly.Cone(10), "Sock", pivot, new Vector3(0f, 0f, 0.8f), new Vector3(0.6f, 1.6f, 0.6f), MiniKit.M("Hazard_Red", Palette.Hazard),
                new Vector3(-90f, 0f, 0f));

            info.spawnPoints = MiniKit.TwoRings(Origin, 4.5f, 9f);
            info.center = Origin;
            info.radius = Half;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, 24f));
            MiniKit.Dress(root, 17f, 2020, "GONE WITH THE WIND", null, null, new Color(0.3f, 0.35f, 0.3f));
        }

        public override void Begin()
        {
            sock = Course.transform.Find("WindSock");
            lastWarn = -99f;
        }

        /// <summary>Gust number n: which way it blows.</summary>
        Vector3 GustDir(int n)
        {
            float a = Rand(n, 7) * Mathf.PI * 2f;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        /// <summary>0 = calm, 1 = full gust; also the gust index and whether it's about to start.</summary>
        float GustAt(float t, out int n, out bool warning)
        {
            n = Mathf.FloorToInt((t - 6f) / GustEvery);
            warning = false;
            if (t < 6f) { n = -1; return 0f; }
            float into = (t - 6f) - n * GustEvery;
            if (into < GustWarn) { warning = true; return 0f; }
            into -= GustWarn;
            if (into > GustLength) return 0f;
            return Mathf.Sin(Mathf.Clamp01(into / GustLength) * Mathf.PI);
        }

        public override void Tick()
        {
            base.Tick();
            float t = Clock;
            float g = GustAt(t, out int n, out bool warn);
            Vector3 dir = n >= 0 ? GustDir(n) : Vector3.forward;
            if (sock != null)
            {
                // The sock points where the wind will blow (wobbles harder during a gust).
                float wob = Mathf.Sin(Time.time * (6f + g * 20f)) * (5f + g * 12f);
                sock.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-g * 70f + 60f, wob, 0f);
            }
            if (warn && n != Mathf.RoundToInt(lastWarn))
            {
                lastWarn = n;
                Announcer.ShowToast("GUST INCOMING!");
                Sfx.Ui("siren", 0.6f);
            }
            if (g <= 0f || t <= 0f) return;
            foreach (var c in LocalAlive())
                c.Motor.AddConveyor(dir * GustSpeed * g);
            if (Random.value < g * 0.6f)
            {
                Vector3 p = Origin + new Vector3(Random.Range(-Half, Half), Random.Range(0.3f, 3f), Random.Range(-Half, Half));
                Puffs.Smoke(p, dir * 12f, Random.Range(0.15f, 0.3f), 0.8f);
            }
        }

        public override string Status(RaceManager race)
        {
            GustAt(Clock, out _, out bool warn);
            return "STILL IN: " + race.AliveCount + (warn ? "     GUST INCOMING!" : "");
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Lean into the wind during gusts.
            float g = GustAt(Clock, out int n, out bool warn);
            if ((g > 0f || warn) && n >= 0) return Origin - GustDir(n) * 4f;
            return ChaseClosest(me, pos, Half - 3f);
        }
    }
}
