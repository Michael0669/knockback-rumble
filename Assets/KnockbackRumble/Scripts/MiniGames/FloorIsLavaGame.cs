using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// THE FLOOR IS LAVA: lava rises from below. Climb the two spiral staircases of floating
    /// platforms around the tower. Touch the lava and you're out. The lava height is a formula of
    /// the shared clock, so it's the same everywhere.
    /// </summary>
    public class FloorIsLavaGame : MiniGame
    {
        const int Steps = 22;
        const float StepAngle = 32f, StepRadius = 7.5f, Rise = 1f, TopY = 23f, Grace = 12f;

        public override EventKind Kind => EventKind.FloorIsLava;
        public override string Title => "THE FLOOR IS LAVA";
        public override string Goal => "Climb! The lava keeps rising.";
        public override string Objective => "Lava is rising from below. Climb the spiral platforms around the tower to stay above it. Touch it and you're out!";
        public override string Blurb => "Climb the tower before the rising lava gets you.";
        public override string[,] Cards => new[,]
        {
            { "CLIMB", "higher and higher" },
            { "LAVA = OUT", "one touch and you're done" },
            { "$6 / SEC", "while you're still in" },
        };
        public override float Duration => 110f;
        public override bool FallsEliminate => true;
        public override int StandingPerSecond => 6;

        Transform lava;
        float nextBubble;

        static Vector3 StepLocal(int spiral, int k)
        {
            float a = (spiral * 180f + k * StepAngle) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * StepRadius, (k + 1) * Rise, Mathf.Cos(a) * StepRadius);
        }

        public override void Build(Transform root, CourseInfo info)
        {
            var stone = MiniKit.M("Lava_Stone", new Color(0.4f, 0.36f, 0.34f));
            var stoneDark = MiniKit.M("Lava_StoneDark", new Color(0.26f, 0.23f, 0.22f));
            var rest = MiniKit.M("Lava_Rest", new Color(0.5f, 0.45f, 0.38f));
            var gold = MiniKit.M("Gold", Palette.Gold);

            MiniKit.Disc(root, "Ground", new Vector3(0f, -0.5f, 0f), 14f, 1f, stone, 36);
            MiniKit.StripeRim(root, Vector3.zero, 14f, 0.015f, 40);
            // The tower
            MiniKit.Disc(root, "Tower", new Vector3(0f, TopY * 0.5f - 0.3f, 0f), 2.2f, TopY - 0.5f, stoneDark, 14);
            for (int i = 0; i < 8; i++)
                Shapes.Make(PrimitiveType.Cube, "TowerBand", root, new Vector3(0f, 2f + i * 2.8f, 0f), new Vector3(4.5f, 0.3f, 4.5f), stone, false,
                    new Vector3(0f, i * 11f, 0f));
            // The steps
            for (int s = 0; s < 2; s++)
                for (int k = 0; k < Steps; k++)
                {
                    bool big = k % 7 == 6;
                    var p = StepLocal(s, k);
                    float size = big ? 4.2f : 3.2f;
                    MiniKit.Block(root, "Step", p + Vector3.down * 0.25f, new Vector3(size, 0.5f, size), big ? rest : stone, true,
                        new Vector3(0f, s * 180f + k * StepAngle, 0f));
                    LowPoly.Make(LowPoly.Rock(k % 3), "Under", root, p + Vector3.down * 0.9f, new Vector3(size * 0.8f, 1.2f, size * 0.8f), stoneDark);
                }
            // The top: a golden crown platform
            MiniKit.Disc(root, "Top", new Vector3(0f, TopY - 0.25f, 0f), 4.5f, 0.5f, gold, 24);
            MiniKit.StripeRim(root, new Vector3(0f, 0f, 0f), 4.5f, TopY + 0.015f, 20);
            MiniKit.Sign(root, "SAFE (for now)", new Vector3(0f, TopY + 3.2f, 0f), 0.12f, Color.white);

            // The lava (moved at runtime) with a glow
            var l = MiniKit.Block(root, "Lava", new Vector3(0f, -1.5f, 0f), new Vector3(200f, 1f, 200f), Mats.Unlit("Lava_Glow", new Color(1f, 0.4f, 0.08f)), false);
            var glow = new GameObject("LavaLight").AddComponent<Light>();
            glow.transform.SetParent(l.transform, false);
            glow.transform.localPosition = new Vector3(0f, 3f, 0f);
            glow.type = LightType.Point;
            glow.range = 40f;
            glow.intensity = 3f;
            glow.color = new Color(1f, 0.45f, 0.15f);

            var spawns = new List<Vector3>(MiniKit.TwoRings(Origin, 9.5f, 12f));
            info.spawnPoints = spawns.ToArray();
            info.center = Origin;
            info.radius = 12f;
            MiniKit.Lounge(root, info, new Vector3(0f, 27f, 22f));
            MiniKit.Dress(root, 20f, 808, "THE FLOOR IS LAVA", "the floor has always been lava", "MAGMA MART", new Color(0.5f, 0.15f, 0.05f));
        }

        float LavaAt(float t)
        {
            if (t < Grace) return -1f;
            float u = Mathf.Clamp01((t - Grace) / (Duration - Grace));
            return -1f + (TopY - 0.7f + 1f) * Mathf.Pow(u, 1.1f);
        }

        public float LavaY => Origin.y + LavaAt(Mathf.Max(0f, Clock));
        public override float KillY => LavaY - 0.3f;

        public override void Begin()
        {
            lava = Course.transform.Find("Lava");
        }

        public override void End()
        {
            if (lava != null) lava.position = new Vector3(lava.position.x, Origin.y - 1.5f, lava.position.z);
        }

        public override void Tick()
        {
            if (lava == null) return;
            float y = LavaY;
            lava.position = new Vector3(lava.position.x, y - 0.5f, lava.position.z);
            if (Time.time >= nextBubble)
            {
                nextBubble = Time.time + 0.08f;
                var cam = Camera.main;
                Vector3 c = cam != null ? cam.transform.position : Origin;
                Vector3 p = new Vector3(c.x + Random.Range(-14f, 14f), y + 0.1f, c.z + Random.Range(-14f, 14f));
                Puffs.Fire(p, Vector3.up * Random.Range(1f, 3f), Random.Range(0.3f, 0.7f), Random.Range(0.3f, 0.6f));
            }
        }

        public override string Status(RaceManager race)
        {
            return Clock < Grace ? "THE LAVA RISES IN " + Mathf.CeilToInt(Grace - Clock) + "..." : "STILL IN: " + race.AliveCount + "     LAVA RISING!";
        }

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Red;
            if (player == null || player.Eliminated || Clock < Grace) return null;
            float gap = player.transform.position.y - LavaY;
            return gap < 1.6f ? "CLIMB! THE LAVA'S RIGHT THERE!" : null;
        }

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill) => null;

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            var motor = me.Motor;
            if (motor.Stunned) return;
            Vector3 pos = me.transform.position;
            int spiral = me.NetId % 2;
            float lavaY = LavaY;

            // Which step are we on? (-1 = the ground, Steps = the top)
            int on = -1;
            float best = 2.6f;
            for (int k = 0; k < Steps; k++)
            {
                Vector3 p = Origin + StepLocal(spiral, k);
                float dh = Shapes.Flat(pos - p).magnitude;
                if (dh < best && Mathf.Abs(pos.y - p.y) < 0.7f) { best = dh; on = k; }
            }
            if (Shapes.Flat(pos - Origin).magnitude < 4.6f && pos.y > Origin.y + TopY - 0.7f) on = Steps;

            // How high do we want to be? A few meters above the lava (smarter bots keep more margin).
            float margin = Mathf.Lerp(2.5f, 4.5f, skill);
            int want = Steps;
            for (int k = 0; k < Steps; k++)
                if (Origin.y + (k + 1) * Rise > lavaY + margin) { want = k; break; }

            Vector3 target;
            if (on >= want || on == Steps)
            {
                // Hang out in the middle of this platform.
                target = on == Steps || on < 0 ? Origin + Vector3.up * TopY : Origin + StepLocal(spiral, on);
                Vector3 to = Shapes.Flat(target - pos);
                motor.MoveInput = to.magnitude > 0.8f ? to.normalized * 0.6f : Vector3.zero;
                return;
            }

            int next = on + 1;
            target = next >= Steps ? Origin + Vector3.up * TopY : Origin + StepLocal(spiral, next);
            Vector3 dir = Shapes.Flat(target - pos);
            float dist = dir.magnitude;
            motor.MoveInput = dist > 0.01f ? dir / dist : Vector3.zero;
            // Jump across the gap when we're near our edge.
            float jumpFrom = next >= Steps ? 4.2f : 3.1f;
            if (motor.Grounded && dist < jumpFrom && target.y > pos.y + 0.4f) motor.QueueJump();
        }
    }
}
