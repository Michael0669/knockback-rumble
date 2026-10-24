using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// PINBALL WIZARD: you're the ball. A giant pinball table with pop bumpers that fling you,
    /// flippers that swat you back up the table, and glowing targets along the top that pay cash
    /// every time you touch them. Slip down the drain at the bottom and you lose money.
    /// Bumpers and flippers run on the shared clock and each machine knocks its own dummies;
    /// the host pays out the targets.
    /// </summary>
    public class PinballGame : MiniGame
    {
        const float HalfW = 12f, Top = 30f, Bottom = -2f, BumperR = 1.3f, TargetR = 1.4f, TargetCash = 25, TargetCooldown = 2.5f;
        const float FlipperLen = 6f, FlipperPeriod = 1.6f;
        const byte OpLit = 1;

        static readonly Vector3[] Bumpers =
        {
            new Vector3(-5f, 0f, 16f), new Vector3(5f, 0f, 16f), new Vector3(0f, 0f, 20f), new Vector3(-7f, 0f, 9f), new Vector3(7f, 0f, 9f), new Vector3(0f, 0f, 11f),
        };
        static readonly Vector3[] Targets =
        {
            new Vector3(-9f, 0f, 27f), new Vector3(-4.5f, 0f, 28f), new Vector3(0f, 0f, 28.5f), new Vector3(4.5f, 0f, 28f), new Vector3(9f, 0f, 27f),
        };

        public override EventKind Kind => EventKind.Pinball;
        public override string Title => "PINBALL WIZARD";
        public override string Goal => "You're the pinball. Hit the targets, avoid the drain.";
        public override string Objective => "Bounce off the bumpers and touch the glowing targets at the top for cash. The flippers swat you back up. Fall down the drain and you lose $50!";
        public override string Blurb => "You're the ball on a giant pinball table. Hit targets for cash.";
        public override string[,] Cards => new[,]
        {
            { "$" + (int)TargetCash, "per target you touch" },
            { "BUMPERS", "fling you around" },
            { "-$" + ShowManager.FallPenalty, "down the drain" },
        };
        public override float Duration => 90f;
        public override bool WeaponsAllowed => false;
        public override string PrizeLabel => "TARGET";

        readonly HitCooldown cooldown = new HitCooldown();
        readonly Dictionary<long, float> hostTouched = new Dictionary<long, float>();
        Transform[] bumperCaps;
        Renderer[] targetLamps;
        float[] litUntil;
        Transform flipL, flipR;
        Material lampOn, lampOff;

        public override void Build(Transform root, CourseInfo info)
        {
            var table = MiniKit.M("Pin_Table", new Color(0.16f, 0.12f, 0.35f));
            var tableB = MiniKit.M("Pin_TableB", new Color(0.25f, 0.18f, 0.5f));
            var chrome = MiniKit.M("Pin_Chrome", new Color(0.8f, 0.82f, 0.88f));
            var pink = MiniKit.M("Pin_Pink", new Color(1f, 0.35f, 0.7f));
            // The table: playfield with a drain gap at the bottom between the flippers
            float len = Top - Bottom;
            MiniKit.Block(root, "Playfield", new Vector3(0f, -0.5f, Bottom + 6f + (len - 6f) * 0.5f), new Vector3(HalfW * 2f, 1f, len - 6f), table);
            // Bottom slopes beside the drain (the flippers sit at their inner ends)
            MiniKit.Block(root, "InlaneL", new Vector3(-HalfW * 0.5f - 1.5f, -0.5f, Bottom + 3f), new Vector3(HalfW - 3f, 1f, 6f), table);
            MiniKit.Block(root, "InlaneR", new Vector3(HalfW * 0.5f + 1.5f, -0.5f, Bottom + 3f), new Vector3(HalfW - 3f, 1f, 6f), table);
            for (int i = 0; i < 6; i++)
                MiniKit.Block(root, "Lane", new Vector3(-HalfW + 2f + i * 4f, 0.006f, Top - 6f), new Vector3(0.2f, 0.01f, 8f), tableB, false);
            // Walls all round (open at the drain)
            MiniKit.Block(root, "WallL", new Vector3(-HalfW - 0.4f, 0.8f, (Top + Bottom) * 0.5f), new Vector3(0.8f, 2.6f, len), chrome);
            MiniKit.Block(root, "WallR", new Vector3(HalfW + 0.4f, 0.8f, (Top + Bottom) * 0.5f), new Vector3(0.8f, 2.6f, len), chrome);
            MiniKit.Block(root, "WallTop", new Vector3(0f, 0.8f, Top + 0.4f), new Vector3(HalfW * 2f + 1.6f, 2.6f, 0.8f), chrome);
            // Slingshot guides
            MiniKit.Block(root, "GuideL", new Vector3(-7.5f, 0.6f, 3.2f), new Vector3(0.5f, 1.2f, 5f), pink, true, new Vector3(0f, 35f, 0f));
            MiniKit.Block(root, "GuideR", new Vector3(7.5f, 0.6f, 3.2f), new Vector3(0.5f, 1.2f, 5f), pink, true, new Vector3(0f, -35f, 0f));

            // Pop bumpers
            for (int i = 0; i < Bumpers.Length; i++)
            {
                MiniKit.Disc(root, "Bumper" + i, Bumpers[i] + Vector3.up * 0.6f, BumperR, 1.2f, chrome, 16);
                LowPoly.Make(LowPoly.Cylinder(16), "BumperCap" + i, root, Bumpers[i] + Vector3.up * 1.3f, new Vector3(BumperR * 2.2f, 0.3f, BumperR * 2.2f),
                    MiniKit.Glow("Pin_BumperCap", new Color(1f, 0.5f, 0.85f)));
            }
            // Targets (glowing pads along the top)
            for (int i = 0; i < Targets.Length; i++)
            {
                LowPoly.Make(LowPoly.Cylinder(16), "Target" + i, root, Targets[i] + Vector3.up * 0.03f, new Vector3(TargetR * 2f, 0.05f, TargetR * 2f), MiniKit.M("Dummy_Black", Palette.Black));
                MiniKit.FlatRing(root, "TargetRing", Targets[i] + Vector3.up * 0.06f, TargetR, MiniKit.M("Gold", Palette.Gold), 0.08f);
            }
            // Flippers (pivots near the drain, swung at runtime)
            foreach (float side in new[] { -1f, 1f })
            {
                var pivot = new GameObject(side < 0f ? "FlipperL" : "FlipperR").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(side * 5.2f, 0.6f, Bottom + 3.6f);
                MiniKit.Block(pivot, "Flipper", new Vector3(-side * FlipperLen * 0.5f, 0f, 0f), new Vector3(FlipperLen, 1f, 0.9f), chrome, false);
                LowPoly.Make(LowPoly.Cylinder(10), "Hinge", pivot, Vector3.zero, new Vector3(1.2f, 1.1f, 1.2f), pink);
            }
            // The backglass
            MiniKit.Block(root, "Backglass", new Vector3(0f, 8f, Top + 4f), new Vector3(HalfW * 2f + 2f, 14f, 1f), tableB, false);
            for (int i = 0; i < 12; i++)
                LowPoly.Make(LowPoly.Ico(1), "Bulb", root, new Vector3(-HalfW + 1f + i * 2.1f, 14.6f, Top + 3.4f), Vector3.one * 0.6f,
                    MiniKit.Glow(i % 2 == 0 ? "Pin_BulbA" : "Pin_BulbB", i % 2 == 0 ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 0.9f, 1f)));

            info.spawnPoints = MiniKit.StartGrid(W(0f, 0f, 25f), 0f, 16f);
            info.center = W(0f, 0f, 14f);
            info.radius = 16f;
            info.killY = Origin.y - 10f;
            MiniKit.Dress(root, 20f, 2323, "PINBALL WIZARD", null, null, new Color(0.15f, 0.1f, 0.25f));
        }

        public override void Begin()
        {
            bumperCaps = new Transform[Bumpers.Length];
            for (int i = 0; i < Bumpers.Length; i++) bumperCaps[i] = Course.transform.Find("BumperCap" + i);
            targetLamps = new Renderer[Targets.Length];
            litUntil = new float[Targets.Length];
            for (int i = 0; i < Targets.Length; i++)
            {
                var t = Course.transform.Find("Target" + i);
                targetLamps[i] = t != null ? t.GetComponent<Renderer>() : null;
            }
            flipL = Course.transform.Find("FlipperL");
            flipR = Course.transform.Find("FlipperR");
            lampOn = MiniKit.Glow("Pin_TargetOn", new Color(1f, 0.85f, 0.25f));
            lampOff = MiniKit.M("Pin_TargetOff", new Color(0.45f, 0.38f, 0.15f));
            hostTouched.Clear();
        }

        /// <summary>Flipper angle (degrees up from resting) at time t. Each flips on its own beat.</summary>
        float FlipAngle(float t, int side)
        {
            float phase = Mathf.Repeat(t / FlipperPeriod + side * 0.5f, 1f);
            return phase < 0.12f ? Mathf.Sin(phase / 0.12f * Mathf.PI * 0.5f) * 55f : Mathf.Lerp(55f, 0f, Mathf.Clamp01((phase - 0.12f) / 0.25f));
        }

        bool Flipping(float t, int side) => Mathf.Repeat(t / FlipperPeriod + side * 0.5f, 1f) < 0.14f;

        public override void Tick()
        {
            float t = Mathf.Max(0f, Clock);
            if (flipL != null) flipL.localRotation = Quaternion.Euler(0f, -(-25f + FlipAngle(t, 0)), 0f);
            if (flipR != null) flipR.localRotation = Quaternion.Euler(0f, -25f + FlipAngle(t, 1), 0f);
            if (bumperCaps != null)
                for (int i = 0; i < bumperCaps.Length; i++)
                    if (bumperCaps[i] != null) bumperCaps[i].localScale = Vector3.Lerp(bumperCaps[i].localScale, new Vector3(BumperR * 2.2f, 0.3f, BumperR * 2.2f), Time.deltaTime * 8f);
            if (targetLamps != null)
                for (int i = 0; i < targetLamps.Length; i++)
                    if (targetLamps[i] != null) targetLamps[i].sharedMaterial = Time.time < litUntil[i] || Mathf.Repeat(Time.time * 0.8f + i * 0.2f, 1f) < 0.15f ? lampOn : lampOff;
            if (Clock <= 0f) return;

            foreach (var body in DentBody.All)
            {
                if (body == null) continue;
                Vector3 p = body.transform.position;
                // Bumpers: fling you straight away from the middle (everyone sees the flash).
                for (int i = 0; i < Bumpers.Length; i++)
                {
                    Vector3 bp = W(Bumpers[i]);
                    Vector3 off = Shapes.Flat(p - bp);
                    if (off.magnitude > BumperR + 0.65f || p.y > bp.y + 2f) continue;
                    if (!cooldown.TryHit(body, 0.3f)) continue;
                    if (bumperCaps[i] != null) bumperCaps[i].localScale = new Vector3(BumperR * 3f, 0.5f, BumperR * 3f);
                    Sfx.At("bumper", bp + Vector3.up, 0.8f, 0.1f, 40f);
                    if (body.IsLocal) body.Hit((off.sqrMagnitude > 0.01f ? off.normalized : Vector3.back) + Vector3.up * 0.2f, 16f, 4f, null, "Bumper", 0.15f);
                }
                if (!body.IsLocal) continue;
                // Flippers: if you're on one while it flips, you get swatted up the table.
                for (int s = 0; s < 2; s++)
                {
                    if (!Flipping(Clock, s)) continue;
                    float side = s == 0 ? -1f : 1f;
                    Vector3 hinge = W(side * 5.2f, 0f, Bottom + 3.6f);
                    Vector3 local = p - hinge;
                    if (local.y > 2f || local.y < -1f) continue;
                    if (local.x * -side < -0.5f || local.x * -side > FlipperLen + 0.5f || Mathf.Abs(local.z) > 2.2f) continue;
                    if (!cooldown.TryHit(body, 0.5f)) continue;
                    body.Hit(new Vector3(-side * 0.25f, 0.5f, 1f), 22f, 3f, null, "Flipper", 0.4f);
                    Sfx.At("slam", hinge, 0.9f, 0.1f, 40f);
                }
            }
        }

        // ---- Host: targets pay out ----

        public override void HostFrame(RaceManager race)
        {
            foreach (var c in Alive())
            {
                Vector3 p = c.transform.position;
                for (int i = 0; i < Targets.Length; i++)
                {
                    Vector3 tp = W(Targets[i]);
                    if (Shapes.Flat(p - tp).magnitude > TargetR || Mathf.Abs(p.y - tp.y) > 1.5f) continue;
                    long key = c.NetId * 16L + i;
                    if (hostTouched.TryGetValue(key, out float t) && Time.time - t < TargetCooldown) continue;
                    hostTouched[key] = Time.time;
                    Award(c, (int)TargetCash);
                    Lit(i);
                    Send(Msg(OpLit).Byte((byte)i));
                }
            }
        }

        void Lit(int i)
        {
            if (litUntil == null || i < 0 || i >= litUntil.Length) return;
            litUntil[i] = Time.time + 0.8f;
            Sfx.At("coin", W(Targets[i]) + Vector3.up, 1f, 0.1f, 40f);
        }

        public override void OnMessage(byte op, NetReader r)
        {
            if (op == OpLit) Lit(r.Byte());
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Wander between the targets at the top (better bots go straight for them).
            int bucket = Mathf.FloorToInt(Time.time / (4f - skill * 2f)) + me.NetId * 3;
            var t = W(Targets[Mathf.Abs(bucket) % Targets.Length]);
            // Stay out of the drain area.
            if (pos.z - Origin.z < 4f) return W(0f, 0f, 12f);
            return t;
        }
    }
}
