using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// GREASED SUMO: a round, greasy ring that keeps shrinking. Shoves only. Fall off and you're out.
    /// The ring's size comes from the shared clock, and the grease is just low grip on your own dummy.
    /// </summary>
    public class GreasedSumoGame : MiniGame
    {
        const float StartRadius = 13f, EndRadius = 4.5f, GreaseAccel = 14f;

        public override EventKind Kind => EventKind.GreasedSumo;
        public override string Title => "GREASED SUMO";
        public override string Goal => "Shove everyone off the shrinking, greasy ring.";
        public override string Objective => "The ring is covered in grease and keeps shrinking. Shove people off the edge. Fall off and you're out!";
        public override string Blurb => "A shrinking, greasy ring. Shoves only. Last one standing wins.";
        public override string[,] Cards => new[,]
        {
            { "SHOVE", "is all you get" },
            { "FALL = OUT", "one life only" },
            { "$" + ShowManager.SurvivorPayout[0], "last one standing" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override int StandingPerSecond => 5;

        Transform ring, rim;
        readonly Dictionary<DummyMotor, float> grip = new Dictionary<DummyMotor, float>();

        public override void Build(Transform root, CourseInfo info)
        {
            var grease = MiniKit.M("Sumo_Grease", new Color(0.86f, 0.74f, 0.38f));
            var puddle = MiniKit.M("Sumo_Puddle", new Color(0.95f, 0.85f, 0.45f));
            var mat = MiniKit.M("Sumo_Mat", new Color(0.75f, 0.25f, 0.2f));
            var disc = MiniKit.Disc(root, "Ring", new Vector3(0f, -0.5f, 0f), StartRadius, 1f, grease, 40);
            // Puddles and the sumo circle ride on the ring, so they shrink with it.
            var rng = new System.Random(66);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = (float)rng.NextDouble() * 0.42f;
                LowPoly.Make(LowPoly.Cylinder(10), "Puddle", disc.transform, new Vector3(Mathf.Sin(a) * d, 0.505f, Mathf.Cos(a) * d),
                    new Vector3(0.05f + (float)rng.NextDouble() * 0.06f, 0.01f, 0.04f + (float)rng.NextDouble() * 0.05f), puddle,
                    new Vector3(0f, (float)rng.NextDouble() * 180f, 0f));
            }
            LowPoly.Make(LowPoly.Torus(40, 3, 0.06f), "SumoCircle", disc.transform, new Vector3(0f, 0.51f, 0f), new Vector3(0.55f, 0.2f, 0.55f), mat);
            Shapes.Make(PrimitiveType.Cube, "StartLineA", disc.transform, new Vector3(-0.06f, 0.51f, 0f), new Vector3(0.008f, 0.01f, 0.08f), mat, false);
            Shapes.Make(PrimitiveType.Cube, "StartLineB", disc.transform, new Vector3(0.06f, 0.51f, 0f), new Vector3(0.008f, 0.01f, 0.08f), mat, false);
            MiniKit.FlatRing(root, "Rim", new Vector3(0f, 0.02f, 0f), StartRadius - 0.2f, MiniKit.M("Stripe", Palette.Stripe), 0.05f);
            LowPoly.Make(LowPoly.Cone(30), "Underside", root, new Vector3(0f, -5f, 0f), new Vector3(20f, 8f, 20f), MiniKit.M("Sumo_Under", new Color(0.35f, 0.2f, 0.15f)),
                new Vector3(180f, 0f, 0f));

            // Four giant lard buckets around the outside (scenery)
            var bucket = MiniKit.M("Sumo_Bucket", new Color(0.9f, 0.9f, 0.85f));
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, -6f, 22f);
                LowPoly.Make(LowPoly.Cylinder(14), "LardBucket", root, p, new Vector3(6f, 9f, 6f), bucket);
                MiniKit.Sign(root, "LARD", p + Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 1f, -3.05f), 0.25f, new Color(0.8f, 0.2f, 0.15f),
                    45f + i * 90f);
            }

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 8.5f);
            info.center = Origin;
            info.radius = StartRadius;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, 24f));
            MiniKit.Dress(root, 17f, 606, "GREASED SUMO", "brought to you by LARD (tm)", "BIG LARD", new Color(0.35f, 0.25f, 0.12f));
        }

        float RadiusAt(float t)
        {
            float u = Mathf.InverseLerp(Duration * 0.1f, Duration * 0.85f, t);
            return Mathf.Lerp(StartRadius, EndRadius, Mathf.SmoothStep(0f, 1f, u));
        }

        public float Radius => RadiusAt(Mathf.Max(0f, Clock));

        public override void Begin()
        {
            ring = Course.transform.Find("Ring");
            rim = Course.transform.Find("Rim");
            grip.Clear();
            foreach (var c in Everyone())
            {
                if (!c.IsLocal) continue;
                grip[c.Motor] = c.Motor.groundAccel;
                c.Motor.groundAccel = GreaseAccel;
            }
            Apply(StartRadius);
        }

        public override void End()
        {
            foreach (var kv in grip) if (kv.Key != null) kv.Key.groundAccel = kv.Value;
            grip.Clear();
            Apply(StartRadius);
        }

        void Apply(float r)
        {
            if (ring != null) ring.localScale = new Vector3(r * 2f, 1f, r * 2f);
            if (rim != null) rim.localScale = new Vector3((r - 0.2f) * 2f, 0.15f, (r - 0.2f) * 2f);
        }

        public override void Tick() => Apply(Radius);

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            float r = Radius;
            // Someone close? Go shove them (but stay off the edge).
            Contestant best = null;
            float bestD = 4.5f;
            foreach (var c in Alive())
            {
                if (c == me) continue;
                float d = Vector3.Distance(pos, c.transform.position);
                if (d < bestD) { bestD = d; best = c; }
            }
            Vector3 goal;
            if (best != null) goal = best.transform.position;
            else
            {
                int bucket = Mathf.FloorToInt(Time.time / 2.5f) + me.NetId * 13;
                float a = (bucket * 2.399f) % (Mathf.PI * 2f);
                float d = 0.2f + 0.2f * Mathf.Repeat(bucket * 0.618f, 1f);
                goal = Origin + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r * d;
            }
            Vector3 off = Shapes.Flat(goal - Origin);
            if (off.magnitude > r * 0.6f) goal = Origin + off.normalized * r * 0.6f;
            return goal;
        }
    }
}
