using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// BOTTOMLESS CLIP: your own loadout gun, but it never runs out. A plus-shaped rooftop with
    /// four shipping-container forts. Knock-offs pay double.
    /// </summary>
    public class BottomlessClipGame : FrenzyGame
    {
        const float Arm = 17f, Half = 5.5f, Hub = 8f;

        public override EventKind Kind => EventKind.BottomlessClip;
        public override string Title => "BOTTOMLESS CLIP";
        public override string Goal => "Your gun never runs out. Knock everyone off.";
        public override string Objective => "Bring your own loadout gun. No reloading, no ammo, no mercy. Knock-offs pay double!";
        public override string Blurb => "Your loadout gun with unlimited ammo, on a plus-shaped rooftop.";
        public override string[,] Cards => new[,]
        {
            { "UNLIMITED", "ammo for your own gun" },
            { "+$" + (ShowManager.KnockoffCash + KoBonus), "per knock-off" },
            { "-$" + ShowManager.FallPenalty, "when you fall" },
        };
        public override float Duration => 90f;
        public override bool StreaksAllowed => true;
        protected override WeaponDef Gun => null;
        protected override int KoBonus => 100;

        public override void Build(Transform root, CourseInfo info)
        {
            var roof = MiniKit.M("Clip_Roof", new Color(0.48f, 0.5f, 0.55f));
            var roofB = MiniKit.M("Clip_RoofB", new Color(0.4f, 0.42f, 0.47f));
            MiniKit.Block(root, "Hub", new Vector3(0f, -0.5f, 0f), new Vector3(Hub * 2f, 1f, Hub * 2f), roof);
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, i * 90f, 0f);
                MiniKit.Block(root, "Arm", rot * new Vector3(0f, -0.5f, Hub + (Arm - Hub) * 0.5f), new Vector3(Half * 2f, 1f, Arm - Hub), i % 2 == 0 ? roofB : roof, true,
                    new Vector3(0f, i * 90f, 0f));
                // A container fort at the end of each arm, and an ammo... no, you don't need ammo. A vent for cover.
                Kit.Container(root, rot * new Vector3(0f, 0f, Arm - 1.4f), i * 90f + 90f, i % 2 == 0 ? new Color(0.8f, 0.25f, 0.2f) : new Color(0.2f, 0.5f, 0.8f), 6f);
                var col = MiniKit.Block(root, "ContainerCollider", rot * new Vector3(0f, 1.3f, Arm - 1.4f), new Vector3(6f, 2.6f, 2.5f), roof, true,
                    new Vector3(0f, i * 90f, 0f));
                col.GetComponent<Renderer>().enabled = false;
                MiniKit.Block(root, "Vent", rot * new Vector3(3f, 0.6f, Hub + 2f), new Vector3(1.6f, 1.2f, 1.6f), Kit.Steel);
            }
            // Stripes along every edge
            var yellow = MiniKit.M("Stripe", Palette.Stripe);
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, i * 90f, 0f);
                MiniKit.Block(root, "EdgeL", rot * new Vector3(-Half + 0.2f, 0.01f, (Hub + Arm) * 0.5f), new Vector3(0.4f, 0.02f, Arm - Hub), yellow, false, new Vector3(0f, i * 90f, 0f));
                MiniKit.Block(root, "EdgeR", rot * new Vector3(Half - 0.2f, 0.01f, (Hub + Arm) * 0.5f), new Vector3(0.4f, 0.02f, Arm - Hub), yellow, false, new Vector3(0f, i * 90f, 0f));
            }
            // An AC unit and a satellite dish in the middle for a bit of cover
            MiniKit.Block(root, "ACUnit", new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 1.8f, 2.4f), MiniKit.M("Clip_AC", new Color(0.82f, 0.82f, 0.78f)));
            LowPoly.Make(LowPoly.Cone(12), "Dish", root, new Vector3(0f, 2.3f, 0f), new Vector3(1.6f, 0.5f, 1.6f), MiniKit.M("White", Palette.White), new Vector3(180f, 0f, 0f));

            // Spawns: 4 down each arm (16 total)
            var spawns = new Vector3[16];
            for (int i = 0; i < 16; i++)
            {
                var rot = Quaternion.Euler(0f, (i % 4) * 90f, 0f);
                int k = i / 4;
                spawns[i] = W(rot * new Vector3(k % 2 == 0 ? -2f : 2f, 0.05f, k < 2 ? 9.5f : 12.5f));
            }
            info.spawnPoints = spawns;
            info.center = Origin;
            info.radius = Arm;
            MiniKit.Lounge(root, info, new Vector3(16f, 9f, 16f), 225f);
            MiniKit.Dress(root, Arm, 1818, "BOTTOMLESS CLIP", null, null, new Color(0.25f, 0.27f, 0.3f));
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            var g = ChaseClosest(me, pos, Arm - 3f);
            if (g == null) return null;
            // Stay on the plus: snap toward the nearest arm's center line.
            Vector3 l = Shapes.Flat(g.Value - Origin);
            if (Mathf.Abs(l.x) > Hub - 1f && Mathf.Abs(l.z) > Half - 1.5f) l.z = Mathf.Clamp(l.z, -(Half - 1.5f), Half - 1.5f);
            if (Mathf.Abs(l.z) > Hub - 1f && Mathf.Abs(l.x) > Half - 1.5f) l.x = Mathf.Clamp(l.x, -(Half - 1.5f), Half - 1.5f);
            return Origin + l;
        }
    }
}
