using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// WHEEL OF MISFORTUNE: a giant prize wheel spins every 15 seconds and changes the rules:
    /// rockets for everyone, leaf blowers, moon gravity, greased floor, shoves only, double
    /// knock-off cash... The order comes from the round's seed, so it's the same everywhere.
    /// </summary>
    public class WheelOfMisfortuneGame : MiniGame
    {
        const float Radius = 15f, RuleLength = 15f, SpinTime = 2.5f, KoBonus = 50f;

        enum Rule { Rockets, LeafBlowers, MoonGravity, Grease, ShovesOnly, DoubleCash, BoomSticks, HandCannons }

        static readonly string[] RuleNames =
        {
            "ROCKETS FOR EVERYONE", "LEAF BLOWERS", "MOON GRAVITY", "GREASED FLOOR", "SHOVES ONLY", "DOUBLE KNOCK-OFF CASH", "BOOM STICKS", "HAND CANNONS",
        };
        static readonly Color[] RuleColors =
        {
            new Color(0.95f, 0.3f, 0.25f), new Color(0.95f, 0.55f, 0.15f), new Color(0.6f, 0.65f, 1f), new Color(0.9f, 0.8f, 0.4f),
            new Color(0.95f, 0.95f, 0.95f), new Color(0.3f, 0.85f, 0.4f), new Color(0.95f, 0.65f, 0.2f), new Color(0.8f, 0.7f, 0.5f),
        };

        public override EventKind Kind => EventKind.WheelOfMisfortune;
        public override string Title => "WHEEL OF MISFORTUNE";
        public override string Goal => "The wheel changes the rules every 15 seconds.";
        public override string Objective => "Every 15 seconds the wheel spins: rockets, leaf blowers, moon gravity, grease, shoves only... Adapt or get launched. Knock-offs pay extra.";
        public override string Blurb => "A giant wheel changes the rules every 15 seconds.";
        public override string[,] Cards => new[,]
        {
            { "NEW RULE", "every 15 seconds" },
            { "UNLIMITED", "ammo for wheel guns" },
            { "+$" + (ShowManager.KnockoffCash + (int)KoBonus), "per knock-off" },
        };
        public override float Duration => 105f;
        public override bool StreaksAllowed => false;
        public override bool InfiniteAmmo => true;
        public override string PrizeLabel => "WHEEL BONUS";

        Transform wheel;
        int shown = -2;
        float nextCheck;
        readonly Dictionary<DummyMotor, float> grip = new Dictionary<DummyMotor, float>();

        public override void Build(Transform root, CourseInfo info)
        {
            var stage = MiniKit.M("Wheel_Stage", new Color(0.3f, 0.22f, 0.45f));
            var gold = MiniKit.M("Gold", Palette.Gold);
            MiniKit.Disc(root, "Stage", new Vector3(0f, -0.5f, 0f), Radius, 1f, stage, 40);
            MiniKit.StripeRim(root, Vector3.zero, Radius, 0.015f, 40);
            LowPoly.Make(LowPoly.Cone(30), "Underside", root, new Vector3(0f, -5f, 0f), new Vector3(Radius * 2f, 8f, Radius * 2f), stage, new Vector3(180f, 0f, 0f));
            // A few bumpers for cover
            for (int i = 0; i < 5; i++)
            {
                var p = Quaternion.Euler(0f, i * 72f + 36f, 0f) * new Vector3(0f, 0.75f, 8f);
                MiniKit.Disc(root, "Podium", p, 1.2f, 1.5f, gold, 12);
            }

            // The wheel itself, standing up behind the stage
            var hub = new GameObject("Wheel").transform;
            hub.SetParent(root, false);
            hub.localPosition = new Vector3(0f, 12f, Radius + 8f);
            int n = RuleNames.Length;
            for (int i = 0; i < n; i++)
            {
                var wedge = new GameObject("Wedge").transform;
                wedge.SetParent(hub, false);
                wedge.localRotation = Quaternion.Euler(0f, 0f, i * 360f / n);
                Shapes.Make(PrimitiveType.Cube, "Slice", wedge, new Vector3(0f, 4.5f, 0f), new Vector3(3.4f, 9f, 0.4f), MiniKit.M("Wheel_" + i, RuleColors[i]), false);
            }
            LowPoly.Make(LowPoly.Cylinder(16), "Hub", root, new Vector3(0f, 12f, Radius + 7.6f), new Vector3(3f, 0.6f, 3f), gold, new Vector3(90f, 0f, 0f));
            LowPoly.Make(LowPoly.Cone(4), "Pointer", root, new Vector3(0f, 22.3f, Radius + 7.4f), new Vector3(1.6f, 1.8f, 0.6f), MiniKit.M("Hazard_Red", Palette.Hazard),
                new Vector3(180f, 0f, 0f));
            MiniKit.Block(root, "WheelStand", new Vector3(0f, 0f, Radius + 8.6f), new Vector3(2f, 24f, 1f), Kit.Girder, false);

            info.spawnPoints = MiniKit.TwoRings(Origin, 4.5f, 11f);
            info.center = Origin;
            info.radius = Radius;
            MiniKit.Dress(root, 17f, 2525, "WHEEL OF MISFORTUNE", null, null, new Color(0.3f, 0.2f, 0.35f));
        }

        int RuleIndexAt(float t) => t < 0f ? -1 : Mathf.FloorToInt(t / RuleLength);

        Rule RuleOf(int k)
        {
            if (k < 0) return Rule.ShovesOnly;
            // Never the same rule twice in a row.
            int n = RuleNames.Length;
            int prev = k == 0 ? -1 : (int)RuleOf(k - 1);
            int pick = Mathf.FloorToInt(Rand(k, 5) * n) % n;
            if (pick == prev) pick = (pick + 1 + Mathf.FloorToInt(Rand(k, 6) * (n - 1))) % n;
            if (pick == prev) pick = (pick + 1) % n;
            return (Rule)pick;
        }

        Rule Current => RuleOf(RuleIndexAt(Clock));
        bool Spinning => Clock >= 0f && Clock - RuleIndexAt(Clock) * RuleLength < SpinTime;

        public override void Begin()
        {
            wheel = Course.transform.Find("Wheel");
            shown = -2;
            grip.Clear();
            foreach (var c in Everyone())
                if (c.IsLocal) grip[c.Motor] = c.Motor.groundAccel;
        }

        public override void End()
        {
            foreach (var kv in grip)
                if (kv.Key != null) { kv.Key.groundAccel = kv.Value; kv.Key.MoonBootsUntil = -1f; }
            grip.Clear();
        }

        WeaponDef GunFor(Rule r)
        {
            switch (r)
            {
                case Rule.Rockets: return WeaponDef.RocketLauncher;
                case Rule.LeafBlowers: return WeaponDef.LeafBlower;
                case Rule.BoomSticks: return WeaponDef.BoomStick;
                case Rule.HandCannons: return WeaponDef.HandCannon;
                default: return null;
            }
        }

        public override void Tick()
        {
            float t = Clock;
            int k = RuleIndexAt(t);
            var rule = RuleOf(k);

            // Spin the wheel: fast at the start of each rule, landing on the new one.
            if (wheel != null)
            {
                float into = t - Mathf.Max(0, k) * RuleLength;
                float target = -(int)rule * 360f / RuleNames.Length;
                float spin = k >= 0 && into < SpinTime ? (1f - Mathf.SmoothStep(0f, 1f, into / SpinTime)) * 1080f : 0f;
                wheel.localRotation = Quaternion.Euler(0f, 0f, target + spin);
            }
            if (k != shown)
            {
                shown = k;
                if (k >= 0)
                {
                    Announcer.ShowToast(RuleNames[(int)rule] + "!");
                    Sfx.Ui("fanfare", 0.6f);
                }
            }

            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.2f;
            foreach (var c in LocalAlive())
            {
                var gun = k >= 0 ? GunFor(rule) : null;
                if (k >= 0 && rule == Rule.ShovesOnly) { if (c.Weapons.Held != null) c.Weapons.Holster(); }
                else if (gun != null) { if (c.Weapons.Held != gun) c.Weapons.Give(gun); }
                else if (c.Weapons.Held == null || IsWheelGun(c.Weapons.Held)) c.Weapons.Give(c.Loadout.PrimaryDef);
                c.Motor.MoonBootsUntil = k >= 0 && rule == Rule.MoonGravity ? Time.time + 0.4f : -1f;
                if (grip.TryGetValue(c.Motor, out float normal))
                    c.Motor.groundAccel = k >= 0 && rule == Rule.Grease ? 12f : normal;
            }
        }

        static bool IsWheelGun(WeaponDef w) =>
            w == WeaponDef.RocketLauncher || w == WeaponDef.LeafBlower || w == WeaponDef.BoomStick || w == WeaponDef.HandCannon;

        public override void HostOnFell(Contestant victim, Contestant attacker)
        {
            if (attacker == null) return;
            Award(attacker, Current == Rule.DoubleCash ? (int)KoBonus + ShowManager.KnockoffCash : (int)KoBonus);
        }

        public override string Status(RaceManager race)
        {
            if (Clock < 0f) return null;
            float left = RuleLength - (Clock - RuleIndexAt(Clock) * RuleLength);
            return Spinning ? "SPINNING..." : RuleNames[(int)Current] + "     NEXT SPIN IN " + Mathf.CeilToInt(left);
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            Contestant best = null;
            float bd = float.MaxValue;
            foreach (var c in Alive())
            {
                if (c == me) continue;
                float d = (c.transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            Vector3 goal = best != null ? best.transform.position : Origin;
            Vector3 off = Shapes.Flat(goal - Origin);
            if (off.magnitude > Radius - 3f) goal = Origin + off.normalized * (Radius - 3f);
            return goal;
        }
    }
}
