using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// MOON BASE: low gravity the whole game, Spray-n-Prays with unlimited ammo, and bounce pads
    /// that throw you sky high. Floaty chaos on a little crater-covered moon. Knock-offs pay extra.
    /// </summary>
    public class MoonBaseGame : FrenzyGame
    {
        const float Radius = 15f;

        public override EventKind Kind => EventKind.MoonBase;
        public override string Title => "MOON BASE";
        public override string Goal => "Low gravity. Unlimited bullets. Float and spray.";
        public override string Objective => "Gravity is barely a thing here. Everyone gets a Spray-n-Pray that never runs out. Hit the pink pads to fly!";
        public override string Blurb => "Low gravity, bounce pads and unlimited Spray-n-Prays on the moon.";
        public override string[,] Cards => new[,]
        {
            { "LOW GRAVITY", "float like a butterfly" },
            { "SPRAY-N-PRAY", "unlimited ammo" },
            { "+$" + (ShowManager.KnockoffCash + KoBonus), "per knock-off" },
        };
        public override float Duration => 90f;
        protected override WeaponDef Gun => WeaponDef.SprayNPray;
        protected override int KoBonus => 75;

        static readonly Vector3[] Pads =
        {
            new Vector3(8f, 0f, 0f), new Vector3(-8f, 0f, 0f), new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, -8f),
        };
        const float PadRadius = 1f, PadLaunch = 15f;
        readonly System.Collections.Generic.Dictionary<DentBody, float> lastPad = new System.Collections.Generic.Dictionary<DentBody, float>();

        public override void Build(Transform root, CourseInfo info)
        {
            var moon = MiniKit.M("Moon_Rock", new Color(0.72f, 0.72f, 0.75f));
            var crater = MiniKit.M("Moon_Crater", new Color(0.55f, 0.55f, 0.6f));
            MiniKit.Disc(root, "Moon", new Vector3(0f, -0.5f, 0f), Radius, 1f, moon, 36);
            LowPoly.Make(LowPoly.Rock(2), "MoonUnder", root, new Vector3(0f, -6f, 0f), new Vector3(Radius * 2.1f, 11f, Radius * 2.1f), moon);
            var rng = new System.Random(2222);
            for (int i = 0; i < 12; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = 3f + (float)rng.NextDouble() * (Radius - 5f);
                float r = 0.8f + (float)rng.NextDouble() * 1.4f;
                LowPoly.Make(LowPoly.Torus(16, 4, 0.25f), "Crater", root, new Vector3(Mathf.Sin(a) * d, 0.02f, Mathf.Cos(a) * d), new Vector3(r * 2f, 0.6f, r * 2f), crater);
            }
            foreach (var p in Pads)
            {
                var spot = new GameObject("PadSpot").transform;
                spot.SetParent(root, false);
                spot.localPosition = p;
                WeaponModels.BouncePad(spot);
            }
            // A tiny lunar lander and a flag in the middle (cover)
            var gold = MiniKit.M("Moon_Foil", new Color(0.9f, 0.7f, 0.25f));
            MiniKit.Block(root, "Lander", new Vector3(0f, 1.5f, 0f), new Vector3(2.4f, 1.6f, 2.4f), gold);
            for (int i = 0; i < 4; i++)
                MiniKit.Block(root, "Leg", Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0.6f, 1.7f), new Vector3(0.15f, 1.3f, 0.15f), Kit.Steel, false,
                    new Vector3(25f, 45f + i * 90f, 0f));
            LowPoly.Make(LowPoly.Cylinder(6), "FlagPole", root, new Vector3(3f, 1.5f, 3f), new Vector3(0.08f, 3f, 0.08f), Kit.Steel);
            MiniKit.Block(root, "Flag", new Vector3(3.6f, 2.7f, 3f), new Vector3(1.2f, 0.7f, 0.03f), MiniKit.M("Moon_Flag", new Color(0.9f, 0.3f, 0.6f)), false);
            // The Earth hanging in the sky
            LowPoly.Make(LowPoly.Ico(2), "Earth", root, new Vector3(-40f, 30f, 70f), Vector3.one * 18f, MiniKit.Glow("Moon_Earth", new Color(0.3f, 0.55f, 0.95f)));

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 11f);
            info.center = Origin;
            info.radius = Radius;
            MiniKit.Dress(root, 17f, 2222, "MOON BASE", null, null, new Color(0.08f, 0.08f, 0.12f));
        }

        public override void Begin() => lastPad.Clear();

        public override void Tick()
        {
            base.Tick();
            foreach (var c in LocalAlive())
            {
                // Low gravity all game (the same as the Moon Boots kill streak).
                c.Motor.MoonBootsUntil = Time.time + 0.5f;
                if (Clock <= 0f) continue;
                Vector3 feet = c.transform.position;
                foreach (var pad in Pads)
                {
                    Vector3 pp = W(pad);
                    if (Shapes.Flat(feet - pp).magnitude > PadRadius || feet.y > pp.y + 0.8f || feet.y < pp.y - 0.3f) continue;
                    if (lastPad.TryGetValue(c.Dent, out float t) && Time.time - t < 0.6f) continue;
                    lastPad[c.Dent] = Time.time;
                    Vector3 v = c.Motor.Velocity;
                    c.Motor.Launch(Vector3.up * (PadLaunch - Mathf.Min(0f, v.y)), 0f);
                    Sfx.At("spring", pp, 1f, 0.1f, 40f);
                    Fx.Ring(pp + Vector3.up * 0.25f, 1.6f, new Color(1f, 0.35f, 0.7f));
                }
            }
        }

        public override void End()
        {
            foreach (var c in Everyone()) if (c.IsLocal) c.Motor.MoonBootsUntil = -1f;
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill) => ChaseClosest(me, pos, Radius - 3f);
    }
}
