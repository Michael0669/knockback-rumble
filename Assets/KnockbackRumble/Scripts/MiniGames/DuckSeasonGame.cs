using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// DUCK SEASON: nine tall pillars, everyone gets a Long Shot with unlimited ammo. Snipe people
    /// off their pillars. Knock-offs pay big. Hop between pillars to dodge.
    /// </summary>
    public class DuckSeasonGame : FrenzyGame
    {
        const float Spacing = 7.6f, PillarR = 2.6f, Height = 0f;

        public override EventKind Kind => EventKind.DuckSeason;
        public override string Title => "DUCK SEASON";
        public override string Goal => "Snipers on pillars. Knock everyone off.";
        public override string Objective => "Everyone gets a Long Shot with unlimited ammo. Hold right mouse to scope in. Knock-offs pay big. Hop pillars to dodge!";
        public override string Blurb => "Unlimited sniper rifles on nine tall pillars.";
        public override string[,] Cards => new[,]
        {
            { "LONG SHOT", "unlimited ammo" },
            { "+$" + (ShowManager.KnockoffCash + KoBonus), "per knock-off" },
            { "HOP", "between pillars" },
        };
        public override float Duration => 90f;
        protected override WeaponDef Gun => WeaponDef.LongShot;
        protected override int KoBonus => 150;

        static Vector3 PillarAt(int i) => new Vector3((i % 3 - 1) * Spacing, Height + (i % 2 == 0 ? 0f : 0.8f), (i / 3 - 1) * Spacing);

        public override void Build(Transform root, CourseInfo info)
        {
            var stone = MiniKit.M("Duck_Stone", new Color(0.62f, 0.6f, 0.55f));
            var moss = MiniKit.M("Duck_Moss", new Color(0.35f, 0.55f, 0.3f));
            var reed = MiniKit.M("Duck_Reed", new Color(0.55f, 0.5f, 0.25f));
            var water = MiniKit.M("Duck_Water", new Color(0.2f, 0.45f, 0.55f));
            for (int i = 0; i < 9; i++)
            {
                var p = PillarAt(i);
                MiniKit.Disc(root, "Pillar", p + Vector3.down * 7f, PillarR, 14f, stone, 14);
                MiniKit.Disc(root, "Moss", p + Vector3.down * 0.05f, PillarR + 0.05f, 0.12f, moss, 14);
                // A duck decoy on every pillar, obviously
                var y = MiniKit.M("Duck_Yellow", new Color(1f, 0.85f, 0.2f));
                LowPoly.Make(LowPoly.Ico(1), "Decoy", root, p + new Vector3(1.4f, 0.3f, 0.9f), new Vector3(0.7f, 0.45f, 0.5f), y);
                LowPoly.Make(LowPoly.Ico(1), "DecoyHead", root, p + new Vector3(1.7f, 0.65f, 0.9f), Vector3.one * 0.3f, y);
                LowPoly.Make(LowPoly.Cone(4), "Beak", root, p + new Vector3(1.9f, 0.62f, 0.9f), new Vector3(0.1f, 0.18f, 0.1f), MiniKit.M("Gun_Orange", new Color(1f, 0.5f, 0.1f)),
                    new Vector3(0f, 0f, -90f));
            }
            // A swamp far below, with reeds
            MiniKit.Block(root, "Swamp", new Vector3(0f, -13.5f, 0f), new Vector3(70f, 1f, 70f), water, false);
            var rng = new System.Random(2121);
            for (int i = 0; i < 40; i++)
                LowPoly.Make(LowPoly.Cylinder(4), "Reed", root, new Vector3((float)rng.NextDouble() * 50f - 25f, -12f, (float)rng.NextDouble() * 50f - 25f),
                    new Vector3(0.15f, 3f, 0.15f), reed);

            var spawns = new Vector3[16];
            for (int i = 0; i < 16; i++)
            {
                int pi = i % 9;
                Vector3 off = i < 9 ? new Vector3(-0.9f, 0f, 0f) : new Vector3(0.9f, 0f, -0.6f);
                spawns[i] = W(PillarAt(pi) + off + Vector3.up * 0.05f);
            }
            info.spawnPoints = spawns;
            info.center = Origin;
            info.radius = Spacing + PillarR;
            info.killY = Origin.y - 10f;
            MiniKit.Dress(root, 16f, 2121, "DUCK SEASON", null, null, new Color(0.2f, 0.35f, 0.3f));
        }

        int PillarOf(Vector3 pos)
        {
            int best = 4;
            float bd = float.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                float d = Shapes.Flat(W(PillarAt(i)) - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Stay on your pillar; now and then hop to a neighbor.
            int here = PillarOf(pos);
            int bucket = Mathf.FloorToInt(Time.time / 9f) + me.NetId * 5;
            if (Mathf.Repeat(bucket * 0.618f, 1f) < 0.35f)
            {
                int dx = here % 3, dz = here / 3;
                int pick = Mathf.Abs(bucket) % 4;
                if (pick == 0 && dx > 0) here -= 1;
                else if (pick == 1 && dx < 2) here += 1;
                else if (pick == 2 && dz > 0) here -= 3;
                else if (pick == 3 && dz < 2) here += 3;
            }
            return W(PillarAt(here));
        }

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            var motor = me.Motor;
            if (motor.Stunned || !motor.Grounded) return;
            Vector3 pos = me.transform.position;
            Vector3 goal = W(PillarAt(PillarOf(pos)));
            var g = BotGoal(me, pos, skill);
            if (g == null) return;
            Vector3 to = Shapes.Flat(g.Value - pos);
            if (to.magnitude < 1.2f) { motor.MoveInput = Vector3.zero; return; }
            // Heading for another pillar: jump at the edge.
            motor.MoveInput = to.normalized;
            if (Shapes.Flat(pos - goal).magnitude > PillarR - 0.6f && Shapes.Flat(g.Value - goal).magnitude > 1f) motor.QueueJump();
        }
    }
}
