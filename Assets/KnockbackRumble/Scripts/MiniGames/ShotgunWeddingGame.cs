using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// SHOTGUN WEDDING: everyone gets a Boom Stick with unlimited shells, on top of a giant
    /// three-tier wedding cake. Fall off and you're out. Last one standing catches the bouquet.
    /// </summary>
    public class ShotgunWeddingGame : FrenzyGame
    {
        static readonly float[] TierRadius = { 14f, 9f, 4.5f };
        const float TierStep = 1.5f;

        public override EventKind Kind => EventKind.ShotgunWedding;
        public override string Title => "SHOTGUN WEDDING";
        public override string Goal => "Boom Sticks for everyone. Last one on the cake wins.";
        public override string Objective => "A giant wedding cake, and everybody brought a shotgun. Unlimited shells. Fall off and you're out!";
        public override string Blurb => "Unlimited Boom Sticks on top of a giant wedding cake. One life.";
        public override string[,] Cards => new[,]
        {
            { "BOOM STICK", "unlimited shells" },
            { "FALL = OUT", "one life only" },
            { "$" + ShowManager.SurvivorPayout[0], "last one standing" },
        };
        public override float Duration => 90f;
        public override bool FallsEliminate => true;
        public override int StandingPerSecond => 5;
        protected override WeaponDef Gun => WeaponDef.BoomStick;
        protected override int KoBonus => 50;

        public override void Build(Transform root, CourseInfo info)
        {
            var icing = MiniKit.M("Cake_Icing", new Color(0.98f, 0.95f, 0.92f));
            var sponge = MiniKit.M("Cake_Sponge", new Color(0.95f, 0.8f, 0.55f));
            var pink = MiniKit.M("Cake_Pink", new Color(1f, 0.6f, 0.75f));
            var plate = MiniKit.M("Cake_Plate", new Color(0.85f, 0.87f, 0.9f));
            LowPoly.Make(LowPoly.Cylinder(36), "Plate", root, new Vector3(0f, -1.2f, 0f), new Vector3(TierRadius[0] * 2f + 3f, 0.4f, TierRadius[0] * 2f + 3f), plate);
            for (int i = 0; i < TierRadius.Length; i++)
            {
                float r = TierRadius[i];
                float top = i * TierStep;
                float h = i == 0 ? 2f : TierStep;
                MiniKit.Disc(root, "Tier" + i, new Vector3(0f, top - h * 0.5f, 0f), r, h, i == 0 ? sponge : icing, 36);
                MiniKit.Disc(root, "Icing" + i, new Vector3(0f, top - 0.06f, 0f), r + 0.05f, 0.14f, icing, 36);
                // Pink frosting blobs around each tier
                int n = Mathf.RoundToInt(r * 2.2f);
                for (int k = 0; k < n; k++)
                {
                    float a = k * Mathf.PI * 2f / n;
                    LowPoly.Make(LowPoly.Ico(1), "Frosting", root, new Vector3(Mathf.Sin(a) * r, top - 0.1f, Mathf.Cos(a) * r), new Vector3(0.6f, 0.35f, 0.6f), pink);
                }
            }
            // Bride and groom toppers (huge, they're the cover in the middle)
            var suit = MiniKit.M("Cake_Suit", new Color(0.12f, 0.12f, 0.15f));
            float top2 = (TierRadius.Length - 1) * TierStep;
            MiniKit.Block(root, "Groom", new Vector3(-1f, top2 + 1.3f, 0f), new Vector3(0.9f, 2.6f, 0.7f), suit);
            LowPoly.Make(LowPoly.Ico(1), "GroomHead", root, new Vector3(-1f, top2 + 2.95f, 0f), Vector3.one * 0.7f, MiniKit.M("Cake_Skin", new Color(0.95f, 0.8f, 0.65f)));
            LowPoly.Make(LowPoly.Cone(16), "Bride", root, new Vector3(1f, top2 + 1.3f, 0f), new Vector3(1.6f, 2.6f, 1.6f), icing);
            LowPoly.Make(LowPoly.Ico(1), "BrideHead", root, new Vector3(1f, top2 + 2.95f, 0f), Vector3.one * 0.7f, MiniKit.M("Cake_Skin", new Color(0.95f, 0.8f, 0.65f)));
            var bride = root.Find("Bride");
            if (bride != null && bride.GetComponent<Collider>() == null) bride.gameObject.AddComponent<CapsuleCollider>();

            info.spawnPoints = MiniKit.TwoRings(Origin, 10.5f, 12.5f);
            info.center = Origin;
            info.radius = TierRadius[0];
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, 24f));
            MiniKit.Dress(root, 17f, 1919, "SHOTGUN WEDDING", null, null, new Color(0.4f, 0.3f, 0.35f));
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill) => ChaseClosest(me, pos, TierRadius[0] - 3f);
    }
}
