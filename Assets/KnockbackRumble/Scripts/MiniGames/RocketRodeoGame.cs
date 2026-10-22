using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// ROCKET RODEO: everyone gets a rocket launcher with unlimited ammo, on small floating islands.
    /// Knock-offs pay extra. Pure chaos.
    /// </summary>
    public class RocketRodeoGame : MiniGame
    {
        const int KoBonus = 100;

        public override EventKind Kind => EventKind.RocketRodeo;
        public override string Title => "ROCKET RODEO";
        public override string Goal => "Everyone gets a rocket launcher. Go nuts.";
        public override string Objective => "Unlimited rockets, tiny floating islands, long way down. Knock people off for big money. Rocket jump between islands!";
        public override string Blurb => "Unlimited rocket launchers on tiny floating islands.";
        public override string[,] Cards => new[,]
        {
            { "+$" + (ShowManager.KnockoffCash + KoBonus), "per knock-off" },
            { "UNLIMITED", "rockets. no reloading" },
            { "-$" + ShowManager.FallPenalty, "when you fall" },
        };
        public override float Duration => 90f;
        public override bool StreaksAllowed => false;
        public override string PrizeLabel => "RODEO BONUS";

        static readonly Vector3[] Islands =
        {
            new Vector3(0f, 0f, 0f), new Vector3(13f, 1.2f, 0f), new Vector3(6.5f, 1.6f, 11.3f), new Vector3(-6.5f, 0.6f, 11.3f),
            new Vector3(-13f, 1.4f, 0f), new Vector3(-6.5f, 0.4f, -11.3f), new Vector3(6.5f, 1.0f, -11.3f),
        };

        float nextRefill;

        public override void Build(Transform root, CourseInfo info)
        {
            var grass = MiniKit.M("Rodeo_Grass", new Color(0.45f, 0.62f, 0.28f));
            var dirt = MiniKit.M("Rodeo_Dirt", new Color(0.45f, 0.32f, 0.22f));
            var rock = MiniKit.M("Rodeo_Rock", new Color(0.5f, 0.45f, 0.42f));
            var wood = MiniKit.M("Rodeo_Wood", new Color(0.55f, 0.36f, 0.2f));
            for (int i = 0; i < Islands.Length; i++)
            {
                float r = i == 0 ? 6.5f : 4.2f;
                var p = Islands[i];
                MiniKit.Disc(root, "Island", p + Vector3.down * 0.5f, r, 1f, grass, 20);
                LowPoly.Make(LowPoly.Cylinder(20), "Dirt", root, p + Vector3.down * 1.2f, new Vector3(r * 2f - 0.2f, 0.6f, r * 2f - 0.2f), dirt);
                LowPoly.Make(LowPoly.Rock(i % 3), "Rock", root, p + Vector3.down * 3.2f, new Vector3(r * 1.7f, 4.5f, r * 1.7f), rock);
                // A little fence piece and a hay bale for cover
                if (i > 0)
                {
                    Vector3 outward = Shapes.Flat(p).normalized;
                    MiniKit.Block(root, "HayBale", p + outward * 1.6f + Vector3.up * 0.5f, new Vector3(1.6f, 1f, 1f), MiniKit.M("Rodeo_Hay", new Color(0.9f, 0.78f, 0.35f)), true,
                        new Vector3(0f, Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + 90f, 0f));
                }
            }
            // Center: a bull statue on a plinth (cover)
            MiniKit.Block(root, "Plinth", new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 1.4f), wood);
            var bull = MiniKit.M("Rodeo_Bull", new Color(0.35f, 0.2f, 0.15f));
            LowPoly.Make(LowPoly.Ico(1), "BullBody", root, new Vector3(0f, 1.9f, 0f), new Vector3(2.2f, 1.2f, 1.2f), bull);
            LowPoly.Make(LowPoly.Ico(1), "BullHead", root, new Vector3(1.3f, 2.2f, 0f), new Vector3(0.8f, 0.7f, 0.7f), bull);
            LowPoly.Make(LowPoly.Cone(6), "HornL", root, new Vector3(1.4f, 2.7f, 0.35f), new Vector3(0.15f, 0.5f, 0.15f), MiniKit.M("White", Palette.White), new Vector3(30f, 0f, 0f));
            LowPoly.Make(LowPoly.Cone(6), "HornR", root, new Vector3(1.4f, 2.7f, -0.35f), new Vector3(0.15f, 0.5f, 0.15f), MiniKit.M("White", Palette.White), new Vector3(-30f, 0f, 0f));

            var spawns = new Vector3[Islands.Length * 2];
            for (int i = 0; i < Islands.Length; i++)
            {
                float r = i == 0 ? 3.5f : 1.8f;
                Vector3 side = i == 0 ? new Vector3(r, 0f, 0f) : Quaternion.Euler(0f, 90f, 0f) * Shapes.Flat(Islands[i]).normalized * r;
                spawns[i * 2] = W(Islands[i] + side + Vector3.up * 0.05f);
                spawns[i * 2 + 1] = W(Islands[i] - side + Vector3.up * 0.05f);
            }
            info.spawnPoints = spawns;
            info.center = Origin;
            info.radius = 15f;
            MiniKit.Dress(root, 20f, 1010, "ROCKET RODEO", "yee-haw (legally required to say that)", "BOOMTOWN", new Color(0.4f, 0.3f, 0.2f));
        }

        public override void SetupLocal(Contestant c)
        {
            c.Weapons.Give(WeaponDef.RocketLauncher);
        }

        public override void Tick()
        {
            if (Time.time < nextRefill) return;
            nextRefill = Time.time + 0.25f;
            foreach (var c in LocalAlive())
            {
                if (c.Weapons.Held != WeaponDef.RocketLauncher) c.Weapons.Give(WeaponDef.RocketLauncher);
                else if (c.Weapons.Ammo < WeaponDef.RocketLauncher.ammo) c.Weapons.Refill(false);
            }
        }

        public override void HostOnFell(Contestant victim, Contestant attacker)
        {
            if (attacker != null) Award(attacker, KoBonus);
        }

        Vector3 BotIsland(Contestant me)
        {
            // Hop between islands every few seconds.
            int bucket = Mathf.FloorToInt(Time.time / 6f) + me.NetId * 3;
            return W(Islands[Mathf.Abs(bucket * 5 + me.NetId) % Islands.Length]);
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill) => BotIsland(me);

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            // The usual bot edge check won't jump UP to a higher island, so steer and jump here.
            var motor = me.Motor;
            if (motor.Stunned) return;
            Vector3 pos = me.transform.position;
            Vector3 to = Shapes.Flat(BotIsland(me) - pos);
            if (to.magnitude < 2f) return;
            Vector3 dir = to.normalized;
            motor.MoveInput = dir;
            bool edge = !Physics.Raycast(pos + Vector3.up * 0.5f + dir * 0.9f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
            if (motor.Grounded && edge) motor.QueueJump();
        }
    }
}
